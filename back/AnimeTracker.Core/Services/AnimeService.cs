using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services;

public class AnimeService(
	IAnimeRepository animeRepository,
	IRefreshRunRepository refreshRunRepository,
	IDubMatchRepository dubMatchRepository,
	IAnimeSourceAdapter sourceAdapter,
	IDubService dubService,
	IHangfireJobAdapter hangfireJobAdapter,
	TimeProvider timeProvider,
	ILogger<AnimeService> logger
) : TracingService(logger), IAnimeService
{
	public async Task<IReadOnlyCollection<Anime>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(date)}");

		var entities = await animeRepository.GetBySeason(date, cancellationToken);
		var dubs = (await dubMatchRepository.GetBySeason(date, cancellationToken)).ToLookup(match => match.SourceId);

		var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

		return entities
			.Select(entity => AnimeAssembler.Convert(entity, dubs[entity.SourceId], today))
			// Soonest bingeable first; anything without an end date sinks to the bottom. The grid
			// re-sorts client-side, but an ordered payload is the honest default for the API.
			.OrderBy(anime => anime.Binge.BingeableAt ?? DateOnly.MaxValue)
			.ThenByDescending(anime => anime.Popularity)
			.ToArray();
	}

	public async Task<RefreshQueueResult> QueueRefresh(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(date)}");

		// A dub sync of the season may still be going. It does not block a refresh, which is what feeds it.
		var active = await refreshRunRepository.GetActive(date, RefreshKind.Season, cancellationToken);

		if (active is not null) return new RefreshQueueResult(true, RefreshRunAssembler.Convert(active));

		// Ours, not Hangfire's: reading the scheduler's job id inside the job would mean a Hangfire
		// type in this project, which knows nothing about how jobs are stored and should not start.
		var runId = Guid.NewGuid();

		var run = await refreshRunRepository.Queue(runId, date, RefreshKind.Season, timeProvider.GetUtcNow(), cancellationToken);

		hangfireJobAdapter.Enqueue<AnimeRefreshJob>(job => job.RefreshSeason(date.Year, date.Season, runId));

		return new RefreshQueueResult(false, RefreshRunAssembler.Convert(run));
	}

	public async Task<IReadOnlyCollection<RefreshRun>> GetRecentRuns(int limit, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(limit)}");

		var runs = await refreshRunRepository.GetRecent(limit, cancellationToken);

		return runs.Select(RefreshRunAssembler.Convert).ToArray();
	}

	public async Task RefreshAll(AnimeDate date, Guid runId, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(date)} {Log.F(runId)}");

		await refreshRunRepository.Begin(runId, date, RefreshKind.Season, timeProvider.GetUtcNow(), cancellationToken);

		try
		{
			// One call, episodes included. There is no per-anime follow-up to pace, throttle or
			// resume — which is why this now runs in seconds rather than tens of minutes.
			var animes = await sourceAdapter.GetSeason(date, cancellationToken);

			await animeRepository.Refresh(date, animes, cancellationToken);

			await refreshRunRepository.Finish(runId, RefreshStatus.Succeeded, animes.Length, null,
				timeProvider.GetUtcNow(), cancellationToken);
		}
		catch (Exception exception)
		{
			// The token that killed the run cannot be the one used to record why it died.
			await refreshRunRepository.Finish(runId, RefreshStatus.Failed, 0, exception.Message,
				timeProvider.GetUtcNow(), CancellationToken.None);

			throw;
		}

		await QueueDubSync(date);
	}

	/// <summary>
	///     The dub is measured against the season just stored, so it follows every successful refresh —
	///     the button and the nightly job alike, with no trigger of its own. Whatever goes wrong here is
	///     the dub sync's problem: the season is stored, and its run already says so.
	/// </summary>
	private async Task QueueDubSync(AnimeDate date)
	{
		try
		{
			await dubService.QueueSync(date, CancellationToken.None);
		}
		catch (Exception exception)
		{
			_logger.LogWarning(exception, "The dub sync of {Date} could not be queued", date);
		}
	}
}
