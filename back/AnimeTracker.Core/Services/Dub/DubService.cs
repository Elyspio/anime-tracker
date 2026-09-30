using System.Net;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services.Dub;

public class DubService(
	IAnimeRepository animeRepository,
	IDubMatchRepository matchRepository,
	IDubOverrideRepository overrideRepository,
	IRefreshRunRepository refreshRunRepository,
	IEnumerable<IDubPlatformAdapter> platforms,
	IHangfireJobAdapter hangfireJobAdapter,
	TimeProvider timeProvider,
	ILogger<DubService> logger
) : TracingService(logger), IDubService
{
	/// <summary>Title-search hits opened per query. Each costs a season listing, and the right one is near the top.</summary>
	private const int MaxCandidatesPerQuery = 3;

	/// <summary>
	///     Seasons read per series, newest first. The anime is almost always the newest or the one before;
	///     a franchise with twenty seasons is not worth twenty requests to find out otherwise.
	/// </summary>
	private const int MaxSeasonsPerSeries = 6;

	private readonly IDubPlatformAdapter[] _platforms = platforms.ToArray();

	public async Task<RefreshRun?> QueueSync(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogService($"{Log.F(date)}");

		if (_platforms.Length == 0)
		{
			_logger.LogInformation("No dub platform is configured, {Date} is not synced", date);
			return null;
		}

		// A sync still queued reads the season when it starts, so it will see what was just stored. One
		// already running will not, and the next night's catches up: a second sync alongside it would
		// only ask the platforms everything twice.
		if (await refreshRunRepository.GetActive(date, RefreshKind.Dub, cancellationToken) is not null) return null;

		var runId = Guid.NewGuid();
		var run = await refreshRunRepository.Queue(runId, date, RefreshKind.Dub, timeProvider.GetUtcNow(), cancellationToken);

		try
		{
			hangfireJobAdapter.Enqueue<DubSyncJob>(job => job.SyncSeason(date.Year, date.Season, runId));
		}
		catch (Exception exception)
		{
			// The run already exists: left queued, it would claim a sync nobody will ever run.
			var error = $"The dub sync could not be queued: {exception.Message}";
			await refreshRunRepository.Finish(runId, RefreshStatus.Failed, 0, error, timeProvider.GetUtcNow(), CancellationToken.None);

			run.Status = RefreshStatus.Failed;
			run.Error = error;
		}

		return RefreshRunAssembler.Convert(run);
	}

	public async Task Sync(AnimeDate date, Guid runId, CancellationToken cancellationToken = default)
	{
		using var trace = LogService($"{Log.F(date)} {Log.F(runId)}");

		await refreshRunRepository.Begin(runId, date, RefreshKind.Dub, timeProvider.GetUtcNow(), cancellationToken);

		try
		{
			var today = Today();

			// Most popular first: when a platform starts refusing halfway, the shows people look for
			// are the ones already measured.
			var animes = (await animeRepository.GetBySeason(date, cancellationToken))
				.Where(anime => IsWorthMatching(anime, today))
				.OrderByDescending(anime => anime.Popularity)
				.ToArray();

			var previous = (await matchRepository.GetBySeason(date, cancellationToken)).ToDictionary(match => (match.SourceId, match.Platform));
			var overrides = (await overrideRepository.GetBySourceIds(animes.Select(anime => anime.SourceId).ToArray(), cancellationToken))
				.ToDictionary(@override => (@override.SourceId, @override.Platform));

			var matched = 0;
			var problems = new List<string>();

			foreach (var platform in _platforms)
			{
				var unreadable = 0;

				try
				{
					foreach (var anime in animes)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var key = (anime.SourceId, platform.Platform);

						try
						{
							var match = await Resolve(platform, anime, previous.GetValueOrDefault(key), overrides.GetValueOrDefault(key), cancellationToken);

							await Apply(anime, platform.Platform, match, cancellationToken);

							if (match?.Status == DubMatchStatus.Matched) matched++;
						}
						catch (Exception exception) when (exception is not (DubPlatformUnavailableException or OperationCanceledException))
						{
							// A reply this adapter could not make sense of says nothing about the dub: the
							// previous measurement stays, and the run says how many were skipped.
							unreadable++;
							_logger.LogWarning(exception, "Could not match {SourceId} on {Platform}", anime.SourceId, platform.Platform);
						}

						await refreshRunRepository.Progress(runId, matched, timeProvider.GetUtcNow(), cancellationToken);
					}
				}
				catch (DubPlatformUnavailableException exception)
				{
					// Everything after this anime keeps its previous measurement. The other platforms
					// are still asked: one refusing says nothing about the next.
					problems.Add($"{platform.Platform}: {exception.Message}");
					_logger.LogWarning(exception, "{Platform} stopped answering, its remaining matches are left as they were", platform.Platform);
				}

				if (unreadable > 0) problems.Add($"{platform.Platform}: {unreadable} anime(s) could not be read");
			}

			// Read again rather than trust the list the sync started from: a season refresh may have
			// landed since, and the matches of the animes it added are not strays.
			var current = (await animeRepository.GetBySeason(date, cancellationToken)).Select(anime => anime.SourceId).ToArray();
			await matchRepository.DeleteAllBut(date, current, cancellationToken);

			await refreshRunRepository.Finish(runId,
				problems.Count == 0 ? RefreshStatus.Succeeded : RefreshStatus.Failed,
				matched,
				problems.Count == 0 ? null : string.Join("; ", problems),
				timeProvider.GetUtcNow(),
				cancellationToken);
		}
		catch (Exception exception)
		{
			// The token that killed the run cannot be the one used to record why it died.
			await refreshRunRepository.Finish(runId, RefreshStatus.Failed, 0, exception.Message, timeProvider.GetUtcNow(), CancellationToken.None);

			throw;
		}
	}

	public async Task<IReadOnlyCollection<DubCase>> GetCases(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogService($"{Log.F(date)}");

		var animes = await animeRepository.GetBySeason(date, cancellationToken);
		var matches = (await matchRepository.GetBySeason(date, cancellationToken)).ToDictionary(match => (match.SourceId, match.Platform));
		var overrides = (await overrideRepository.GetBySourceIds(animes.Select(anime => anime.SourceId).ToArray(), cancellationToken))
			.ToDictionary(@override => (@override.SourceId, @override.Platform));

		var cases = new List<DubCase>();

		foreach (var anime in animes.OrderByDescending(anime => anime.Popularity))
		foreach (var platform in _platforms)
		{
			var key = (anime.SourceId, platform.Platform);
			var match = matches.GetValueOrDefault(key);
			var @override = overrides.GetValueOrDefault(key);

			if (IsWorthALook(anime, platform, match, @override)) cases.Add(ToCase(anime, platform.Platform, match, @override));
		}

		return cases;
	}

	public async Task<DubOverrideResult> SetOverride(int sourceId, DubPlatform platform, DubOverrideRequest request,
		CancellationToken cancellationToken = default)
	{
		using var trace = LogService($"{Log.F(sourceId)} {Log.F(platform)} {Log.F(request.Mode)}");

		var adapter = _platforms.FirstOrDefault(candidate => candidate.Platform == platform)
			?? throw new HttpException(HttpStatusCode.NotFound, $"{platform} is not a configured platform.");

		var anime = await animeRepository.GetBySourceId(sourceId, cancellationToken)
			?? throw new HttpException(HttpStatusCode.NotFound, $"No anime has the AniList id {sourceId}.");

		var @override = request.Mode switch
		{
			DubOverrideMode.Auto => null,
			// Checked against the platform's own domain and series path: a pin is never an arbitrary URL.
			DubOverrideMode.Pinned => Override(DubOverrideMode.Pinned, (request.Url is { } url ? adapter.ParseSeriesId(url) : null)
				?? throw new HttpException(HttpStatusCode.BadRequest, $"Not a {platform} series page: {request.Url}")),
			DubOverrideMode.Blocked => Override(DubOverrideMode.Blocked, null),
			_ => throw new HttpException(HttpStatusCode.BadRequest, $"Unknown override mode {request.Mode}.")
		};

		if (@override is null) await overrideRepository.Delete(sourceId, platform, cancellationToken);
		else await overrideRepository.Save(@override, cancellationToken);

		var previous = (await matchRepository.GetBySeason(anime.Date, cancellationToken))
			.FirstOrDefault(match => match.SourceId == sourceId && match.Platform == platform);

		string? error = null;
		DubMatchBase? current = previous;

		try
		{
			if (@override?.Mode == DubOverrideMode.Blocked || IsWorthMatching(anime, Today()))
			{
				current = await Resolve(adapter, anime, previous, @override, cancellationToken);
				await Apply(anime, platform, current, cancellationToken);
			}
		}
		catch (DubPlatformUnavailableException exception)
		{
			// Saved all the same: the next sync applies it. Saying so beats refusing an admin's
			// correction because a platform is having a bad minute.
			error = $"Saved, but {platform} could not be reached ({exception.Message}). The next sync applies it.";
		}

		return new DubOverrideResult(ToCase(anime, platform, current, @override), error);

		DubOverrideBase Override(DubOverrideMode mode, string? seriesId)
		{
			return new DubOverrideBase
			{
				SourceId = sourceId,
				Platform = platform,
				Mode = mode,
				SeriesId = seriesId,
				UpdatedAt = timeProvider.GetUtcNow()
			};
		}
	}

	/// <summary>
	///     The anime's match on one platform, or null when an admin blocked the platform for it. Known
	///     series come first — the one pinned, the one matched last time, the ones AniList links to —
	///     because an id is both cheaper and surer than a title search.
	/// </summary>
	private async Task<DubMatchBase?> Resolve(IDubPlatformAdapter platform, AnimeEntity anime, DubMatchBase? previous,
		DubOverrideBase? @override, CancellationToken cancellationToken)
	{
		if (@override?.Mode == DubOverrideMode.Blocked) return null;

		var anchor = DubAligner.Anchor(anime.Episodes)!;

		if (@override is { Mode: DubOverrideMode.Pinned, SeriesId: { } pinned })
		{
			// A pinned series that no longer lines up is reported, never swapped for another one:
			// the admin said which series it is.
			var attempt = await TrySeries(platform, pinned, anchor, cancellationToken);

			return attempt.Alignment is not null
				? Matched(anime, platform.Platform, attempt, DubMatchMethod.Pinned)
				: Unmatched(anime, platform.Platform, attempt.Series, DubMatchMethod.Pinned);
		}

		var known = new List<(string SeriesId, DubMatchMethod Method)>();

		// Last run's match — unless it came from a pin since removed, which is exactly what the admin
		// took back.
		if (previous is { Status: DubMatchStatus.Matched, SeriesId: { } matchedId, Method: { } matchedBy and not DubMatchMethod.Pinned })
			known.Add((matchedId, matchedBy));

		known.AddRange(anime.StreamingLinks
			.Select(link => platform.ParseSeriesId(link.Url))
			.OfType<string>()
			.Select(seriesId => (seriesId, DubMatchMethod.Link)));

		var tried = new HashSet<string>();
		SeriesAttempt? unaligned = null;

		foreach (var (seriesId, method) in known)
		{
			if (!tried.Add(seriesId)) continue;

			var attempt = await TrySeries(platform, seriesId, anchor, cancellationToken);
			if (attempt.Alignment is not null) return Matched(anime, platform.Platform, attempt, method);

			if (attempt.Series is not null) unaligned ??= attempt;
		}

		string[] titles = [anime.Title, ..anime.AlternativeTitles];
		var aligned = new List<SeriesAttempt>();

		foreach (var query in DubTitles.Queries(anime.Title, anime.AlternativeTitles))
		{
			var hits = await platform.Search(query, cancellationToken);

			foreach (var hit in hits.Where(hit => DubTitles.Matches(titles, [hit.Title, ..hit.AlternativeTitles])).Take(MaxCandidatesPerQuery))
			{
				if (!tried.Add(hit.Id)) continue;

				var attempt = await TrySeries(platform, hit.Id, anchor, cancellationToken);

				if (attempt.Alignment is not null) aligned.Add(attempt);
				else if (attempt.Series is not null) unaligned ??= attempt;
			}

			if (aligned.Count > 0) break;
		}

		return aligned.Count switch
		{
			1 => Matched(anime, platform.Platform, aligned[0], DubMatchMethod.Search),
			// Two series of the same name, each with a season that aired that day: a guess either way.
			> 1 => Unmatched(anime, platform.Platform, aligned[0].Series, DubMatchMethod.Search),
			_ => Unmatched(anime, platform.Platform, unaligned?.Series, null)
		};
	}

	private async Task<SeriesAttempt> TrySeries(IDubPlatformAdapter platform, string seriesId, Episode anchor, CancellationToken cancellationToken)
	{
		var series = await platform.GetSeries(seriesId, cancellationToken);
		if (series is null) return new SeriesAttempt(null, null);

		var loaded = new List<LoadedSeason>();

		foreach (var season in series.Seasons.OrderByDescending(season => season.Order).Take(MaxSeasonsPerSeries))
		{
			var episodes = season.Episodes ?? await platform.GetEpisodes(series.Id, season.Id, cancellationToken);
			loaded.Add(new LoadedSeason(season, episodes));

			if (DubAligner.StartsBy(episodes, anchor.ReleaseDate)) break;
		}

		return new SeriesAttempt(series, DubAligner.Align(anchor, loaded));
	}

	private DubMatchBase Matched(AnimeEntity anime, DubPlatform platform, SeriesAttempt attempt, DubMatchMethod method)
	{
		return new DubMatchBase
		{
			SourceId = anime.SourceId,
			Date = anime.Date,
			Platform = platform,
			Status = DubMatchStatus.Matched,
			Method = method,
			SeriesId = attempt.Series!.Id,
			SeriesTitle = attempt.Series.Title,
			Url = attempt.Series.Url,
			AvailableEpisodes = attempt.Alignment!.Available,
			FrenchEpisodes = attempt.Alignment.French,
			CheckedAt = timeProvider.GetUtcNow()
		};
	}

	/// <summary>A series that does not line up is <see cref="DubMatchStatus.Unaligned" />; no series at all is not found.</summary>
	private DubMatchBase Unmatched(AnimeEntity anime, DubPlatform platform, PlatformSeries? series, DubMatchMethod? method)
	{
		return new DubMatchBase
		{
			SourceId = anime.SourceId,
			Date = anime.Date,
			Platform = platform,
			Status = series is null ? DubMatchStatus.NotFound : DubMatchStatus.Unaligned,
			Method = series is null ? null : method,
			SeriesId = series?.Id,
			SeriesTitle = series?.Title,
			Url = series?.Url,
			AvailableEpisodes = [],
			FrenchEpisodes = [],
			CheckedAt = timeProvider.GetUtcNow()
		};
	}

	private async Task Apply(AnimeEntity anime, DubPlatform platform, DubMatchBase? match, CancellationToken cancellationToken)
	{
		if (match is null) await matchRepository.Delete(anime.SourceId, platform, cancellationToken);
		else await matchRepository.Save(match, cancellationToken);
	}

	/// <summary>
	///     Adult entries are not on these platforms, and an anime that has not aired has nothing to line
	///     up on: asking about either would only spend requests.
	/// </summary>
	private static bool IsWorthMatching(AnimeEntity anime, DateOnly today)
	{
		return !anime.IsAdult && anime.Format != AnimeFormat.Music && DubAligner.Anchor(anime.Episodes) is { } anchor && anchor.ReleaseDate <= today;
	}

	/// <summary>
	///     What an admin should look at: a series that does not line up, a platform AniList lists that
	///     could not be matched, a match made by title alone — the one kind that can be wrong without a
	///     trace — and every override, so it can be taken back.
	/// </summary>
	private static bool IsWorthALook(AnimeEntity anime, IDubPlatformAdapter platform, DubMatchBase? match, DubOverrideBase? @override)
	{
		if (@override is not null) return true;

		return match switch
		{
			{ Status: DubMatchStatus.Unaligned } => true,
			{ Status: DubMatchStatus.Matched, Method: DubMatchMethod.Search } => true,
			{ Status: DubMatchStatus.NotFound } => anime.StreamingLinks.Any(link => platform.Owns(link.Url)),
			_ => false
		};
	}

	private static DubCase ToCase(AnimeEntity anime, DubPlatform platform, DubMatchBase? match, DubOverrideBase? @override)
	{
		return new DubCase
		{
			SourceId = anime.SourceId,
			Title = anime.Title,
			Platform = platform,
			Status = match?.Status,
			Method = match?.Method,
			SeriesTitle = match?.SeriesTitle,
			SeriesUrl = match?.Url,
			FrenchEpisodes = match?.FrenchEpisodes.Count ?? 0,
			Override = @override?.Mode ?? DubOverrideMode.Auto,
			CheckedAt = match?.CheckedAt
		};
	}

	private DateOnly Today()
	{
		return DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
	}

	private sealed record SeriesAttempt(PlatformSeries? Series, DubAlignment? Alignment);
}
