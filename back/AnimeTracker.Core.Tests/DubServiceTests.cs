using System.Linq.Expressions;
using System.Net;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Services.Dub;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>
///     The dub sync's choreography: which series is asked for, in what order, what is saved, and what
///     the run ends up saying. Every port is substituted — no platform, database or scheduler.
/// </summary>
public class DubServiceTests
{
	private static readonly AnimeDate Summer2026 = new(2026, AnimeSeason.Summer);

	private static readonly DateTimeOffset Now = new(2026, 7, 26, 3, 0, 0, TimeSpan.Zero);

	private static readonly DateOnly Premiere = new(2026, 7, 4);

	private readonly IAnimeRepository _animes = Substitute.For<IAnimeRepository>();

	private readonly IDubMatchRepository _matches = Substitute.For<IDubMatchRepository>();

	private readonly IDubOverrideRepository _overrides = Substitute.For<IDubOverrideRepository>();

	private readonly IRefreshRunRepository _runs = Substitute.For<IRefreshRunRepository>();

	private readonly IHangfireJobAdapter _jobs = Substitute.For<IHangfireJobAdapter>();

	private readonly IDubPlatformAdapter _crunchyroll = Platform(DubPlatform.Crunchyroll, "crunchyroll.test");

	private readonly IDubPlatformAdapter _adn = Platform(DubPlatform.Adn, "adn.test");

	public DubServiceTests()
	{
		_overrides.GetBySourceIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([]);
		_matches.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).Returns([]);
		_crunchyroll.Search(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
		_adn.Search(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
	}

	private DubService Service(params IDubPlatformAdapter[] platforms)
	{
		return new DubService(_animes, _matches, _overrides, _runs, platforms.Length == 0 ? [_crunchyroll] : platforms, _jobs,
			new FakeTimeProvider(Now), NullLogger<DubService>.Instance);
	}

	/// <summary>A platform whose series pages are <c>https://{host}/series/{id}</c>.</summary>
	private static IDubPlatformAdapter Platform(DubPlatform platform, string host)
	{
		var adapter = Substitute.For<IDubPlatformAdapter>();
		adapter.Platform.Returns(platform);
		adapter.Owns(Arg.Any<string>()).Returns(call => call.Arg<string>().Contains(host));
		adapter.ParseSeriesId(Arg.Any<string>()).Returns(call =>
			call.Arg<string>() is var url && url.Contains($"{host}/series/") ? url.Split("/series/")[1] : null);
		adapter.GetSeries(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((PlatformSeries?)null);

		return adapter;
	}

	/// <summary>Twelve weekly episodes from the premiere; three have aired by <see cref="Now" />.</summary>
	private static AnimeEntity Anime(int sourceId, string title = "Sousou no Frieren", int popularity = 0, params StreamingLink[] links)
	{
		return new AnimeEntity
		{
			Id = ObjectId.GenerateNewId(),
			SourceId = sourceId,
			Date = Summer2026,
			Title = title,
			AlternativeTitles = [],
			Description = "",
			Studio = "",
			ImageUrl = "",
			Url = $"https://anilist.co/anime/{sourceId}",
			Format = AnimeFormat.Tv,
			IsAdult = false,
			Score = null,
			Popularity = popularity,
			VotesCount = null,
			EpisodesCount = 12,
			Genres = [],
			Episodes = Enumerable.Range(1, 12).Select(number => new Episode(number, Premiere.AddDays(7 * (number - 1)))).ToArray(),
			StreamingLinks = links
		};
	}

	/// <summary>A series whose only season aired from <paramref name="start" />, French on <paramref name="french" />.</summary>
	private static PlatformSeries Series(string id, string title, DateOnly start, params int[] french)
	{
		var episodes = Enumerable.Range(1, 12)
			.Select(number => new PlatformEpisode(number, start.AddDays(7 * (number - 1)), french.Contains(number)))
			.ToArray();

		return new PlatformSeries(id, title, $"https://crunchyroll.test/series/{id}", [new PlatformSeason($"{id}-s1", 1, episodes)]);
	}

	private void Season(params AnimeEntity[] animes)
	{
		_animes.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).Returns(animes.ToList());
	}

	private void Returns(IDubPlatformAdapter platform, PlatformSeries series)
	{
		platform.GetSeries(series.Id, Arg.Any<CancellationToken>()).Returns(series);
	}

	private static DubMatchEntity Stored(int sourceId, DubMatchMethod method, string seriesId, DubMatchStatus status = DubMatchStatus.Matched)
	{
		return new DubMatchEntity
		{
			SourceId = sourceId,
			Date = Summer2026,
			Platform = DubPlatform.Crunchyroll,
			Status = status,
			Method = method,
			SeriesId = seriesId,
			SeriesTitle = "Frieren",
			Url = $"https://crunchyroll.test/series/{seriesId}",
			AvailableEpisodes = [1],
			FrenchEpisodes = [1],
			CheckedAt = Now.AddDays(-1)
		};
	}

	private static RefreshRunEntity Run(Guid runId, RefreshStatus status)
	{
		return new RefreshRunEntity
		{
			Id = ObjectId.GenerateNewId(),
			RunId = runId,
			Date = Summer2026,
			Kind = RefreshKind.Dub,
			Status = status,
			Total = 0,
			StartedAt = Now,
			UpdatedAt = Now,
			FinishedAt = null,
			Error = null
		};
	}

	/// <summary>The match the sync saved for an anime.</summary>
	private DubMatchBase Saved(int sourceId)
	{
		return _matches.ReceivedCalls()
			.Where(call => call.GetMethodInfo().Name == nameof(IDubMatchRepository.Save))
			.Select(call => (DubMatchBase)call.GetArguments()[0]!)
			.Single(match => match.SourceId == sourceId);
	}

	[Fact]
	public async Task Queues_nothing_when_no_platform_is_configured()
	{
		// No egress proxy, no platform adapter: the platforms are not asked, and no run pretends otherwise.
		var service = new DubService(_animes, _matches, _overrides, _runs, [], _jobs, new FakeTimeProvider(Now), NullLogger<DubService>.Instance);

		(await service.QueueSync(Summer2026, TestContext.Current.CancellationToken)).ShouldBeNull();

		await _runs.DidNotReceive().Queue(Arg.Any<Guid>(), Arg.Any<AnimeDate>(), Arg.Any<RefreshKind>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Queues_nothing_while_a_dub_sync_of_the_season_is_in_flight()
	{
		_runs.GetActive(Summer2026, RefreshKind.Dub, Arg.Any<CancellationToken>()).Returns(Run(Guid.NewGuid(), RefreshStatus.Running));

		(await Service().QueueSync(Summer2026, TestContext.Current.CancellationToken)).ShouldBeNull();

		_jobs.DidNotReceive().Enqueue(Arg.Any<Expression<Func<DubSyncJob, Task>>>());
	}

	[Fact]
	public async Task Records_a_dub_run_and_hands_it_to_the_scheduler()
	{
		_runs.Queue(Arg.Any<Guid>(), Summer2026, RefreshKind.Dub, Now, Arg.Any<CancellationToken>())
			.Returns(call => Run(call.Arg<Guid>(), RefreshStatus.Queued));

		var run = await Service().QueueSync(Summer2026, TestContext.Current.CancellationToken);

		run.ShouldNotBeNull();
		run.Kind.ShouldBe(RefreshKind.Dub);
		run.Status.ShouldBe(RefreshStatus.Queued);
		_jobs.Received(1).Enqueue(Arg.Any<Expression<Func<DubSyncJob, Task>>>());
	}

	[Fact]
	public async Task Closes_the_run_it_could_not_hand_to_the_scheduler()
	{
		// Left queued, the run would claim a sync nobody is ever going to run.
		_runs.Queue(Arg.Any<Guid>(), Summer2026, RefreshKind.Dub, Now, Arg.Any<CancellationToken>())
			.Returns(call => Run(call.Arg<Guid>(), RefreshStatus.Queued));
		_jobs.Enqueue(Arg.Any<Expression<Func<DubSyncJob, Task>>>()).Throws(new InvalidOperationException("storage down"));

		var run = await Service().QueueSync(Summer2026, TestContext.Current.CancellationToken);

		run!.Status.ShouldBe(RefreshStatus.Failed);
		await _runs.Received(1).Finish(run.RunId, RefreshStatus.Failed, 0, Arg.Is<string>(error => error.Contains("storage down")), Now, CancellationToken.None);
	}

	[Fact]
	public async Task Matches_through_the_series_anilist_links_to_without_searching()
	{
		Season(Anime(1, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G1")));
		Returns(_crunchyroll, Series("G1", "Frieren", Premiere, 1, 2, 3));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		var match = Saved(1);
		match.Status.ShouldBe(DubMatchStatus.Matched);
		match.Method.ShouldBe(DubMatchMethod.Link);
		match.SeriesId.ShouldBe("G1");
		match.Url.ShouldBe("https://crunchyroll.test/series/G1");
		match.FrenchEpisodes.ShouldBe([1, 2, 3]);
		match.AvailableEpisodes.Count.ShouldBe(12);
		match.CheckedAt.ShouldBe(Now);
		await _crunchyroll.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Falls_back_to_a_title_search_and_keeps_the_hit_that_lines_up()
	{
		Season(Anime(1, "Sousou no Frieren 2nd Season"));
		_crunchyroll.Search("sousou no frieren", Arg.Any<CancellationToken>()).Returns([
			new PlatformSeriesSummary("OTHER", "Something Else", []),
			new PlatformSeriesSummary("G1", "Sousou no Frieren", [])
		]);
		Returns(_crunchyroll, Series("G1", "Sousou no Frieren", Premiere, 1));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		var match = Saved(1);
		match.Status.ShouldBe(DubMatchStatus.Matched);
		match.Method.ShouldBe(DubMatchMethod.Search);
		// A hit whose title does not match is never even opened.
		await _crunchyroll.DidNotReceive().GetSeries("OTHER", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Leaves_a_series_that_does_not_line_up_unaligned()
	{
		// Same title, but the only season aired two years ago: this anime is not on it yet.
		Season(Anime(1, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G1")));
		Returns(_crunchyroll, Series("G1", "Frieren", new DateOnly(2024, 4, 6), 1, 2));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		var match = Saved(1);
		match.Status.ShouldBe(DubMatchStatus.Unaligned);
		match.SeriesId.ShouldBe("G1");
		match.FrenchEpisodes.ShouldBeEmpty();
	}

	[Fact]
	public async Task Marks_an_anime_no_series_carries_as_not_found()
	{
		Season(Anime(1));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		var match = Saved(1);
		match.Status.ShouldBe(DubMatchStatus.NotFound);
		match.SeriesId.ShouldBeNull();
		match.Url.ShouldBeNull();
	}

	[Fact]
	public async Task Tries_the_series_matched_last_time_first()
	{
		Season(Anime(1));
		_matches.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).Returns([Stored(1, DubMatchMethod.Search, "G1")]);
		Returns(_crunchyroll, Series("G1", "Frieren", Premiere, 1, 2, 3));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		(Saved(1)).FrenchEpisodes.ShouldBe([1, 2, 3]);
		// Found last night by search: still a search match, and no search tonight.
		(Saved(1)).Method.ShouldBe(DubMatchMethod.Search);
		await _crunchyroll.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Forgets_a_match_that_came_from_a_pin_since_taken_back()
	{
		// The admin reset the pin to automatic: reusing the pinned series would keep the pin alive.
		Season(Anime(1));
		_matches.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).Returns([Stored(1, DubMatchMethod.Pinned, "PINNED")]);

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		await _crunchyroll.DidNotReceive().GetSeries("PINNED", Arg.Any<CancellationToken>());
		(Saved(1)).Status.ShouldBe(DubMatchStatus.NotFound);
	}

	[Fact]
	public async Task Follows_a_pin_and_nothing_else()
	{
		Season(Anime(1, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/LINKED")));
		_overrides.GetBySourceIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([
			new DubOverrideEntity { SourceId = 1, Platform = DubPlatform.Crunchyroll, Mode = DubOverrideMode.Pinned, SeriesId = "PINNED", UpdatedAt = Now }
		]);
		Returns(_crunchyroll, Series("PINNED", "Frieren", Premiere, 1));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		(Saved(1)).Method.ShouldBe(DubMatchMethod.Pinned);
		await _crunchyroll.DidNotReceive().GetSeries("LINKED", Arg.Any<CancellationToken>());
		await _crunchyroll.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Reports_a_pin_that_no_longer_lines_up_instead_of_replacing_it()
	{
		Season(Anime(1));
		_overrides.GetBySourceIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([
			new DubOverrideEntity { SourceId = 1, Platform = DubPlatform.Crunchyroll, Mode = DubOverrideMode.Pinned, SeriesId = "PINNED", UpdatedAt = Now }
		]);
		Returns(_crunchyroll, Series("PINNED", "Frieren", new DateOnly(2024, 4, 6)));

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		var match = Saved(1);
		match.Status.ShouldBe(DubMatchStatus.Unaligned);
		match.Method.ShouldBe(DubMatchMethod.Pinned);
		await _crunchyroll.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Drops_the_match_of_a_blocked_platform_without_asking_it()
	{
		Season(Anime(1, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G1")));
		_overrides.GetBySourceIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([
			new DubOverrideEntity { SourceId = 1, Platform = DubPlatform.Crunchyroll, Mode = DubOverrideMode.Blocked, SeriesId = null, UpdatedAt = Now }
		]);

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		await _matches.Received(1).Delete(1, DubPlatform.Crunchyroll, Arg.Any<CancellationToken>());
		await _crunchyroll.DidNotReceive().GetSeries(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Does_not_ask_about_an_anime_that_has_not_aired()
	{
		var upcoming = Anime(1);
		upcoming.Episodes = [new Episode(1, new DateOnly(2026, 10, 3))];
		var adult = Anime(2);
		adult.IsAdult = true;
		Season(upcoming, adult);

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		await _crunchyroll.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<CancellationToken>());
		await _matches.DidNotReceive().Save(Arg.Any<DubMatchBase>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Reads_seasons_newest_first_and_stops_at_the_one_that_holds_the_premiere()
	{
		Season(Anime(1, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G1")));
		_crunchyroll.GetSeries("G1", Arg.Any<CancellationToken>()).Returns(new PlatformSeries("G1", "Frieren", "https://crunchyroll.test/series/G1", [
			new PlatformSeason("s1", 1, null),
			new PlatformSeason("s2", 2, null),
			new PlatformSeason("s3", 3, null)
		]));
		_crunchyroll.GetEpisodes("G1", "s3", Arg.Any<CancellationToken>()).Returns([new PlatformEpisode(1, Premiere, true)]);

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		(Saved(1)).Status.ShouldBe(DubMatchStatus.Matched);
		await _crunchyroll.DidNotReceive().GetEpisodes("G1", "s2", Arg.Any<CancellationToken>());
		await _crunchyroll.DidNotReceive().GetEpisodes("G1", "s1", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Stops_asking_a_platform_that_refuses_and_keeps_what_it_had()
	{
		var runId = Guid.NewGuid();
		Season(
			Anime(1, popularity: 10, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G1")),
			Anime(2, popularity: 5, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G2")));
		_crunchyroll.GetSeries("G1", Arg.Any<CancellationToken>())
			.ThrowsAsync(new DubPlatformUnavailableException(DubPlatform.Crunchyroll, "Crunchyroll answered with an anti-bot challenge."));
		_adn.GetSeries(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((PlatformSeries?)null);

		await Service(_crunchyroll, _adn).Sync(Summer2026, runId, TestContext.Current.CancellationToken);

		// A challenge says nothing about the catalogue: no Crunchyroll match is written or deleted.
		await _matches.DidNotReceive().Save(Arg.Is<DubMatchBase>(match => match.Platform == DubPlatform.Crunchyroll), Arg.Any<CancellationToken>());
		await _crunchyroll.DidNotReceive().GetSeries("G2", Arg.Any<CancellationToken>());
		// The next platform is still asked.
		await _adn.Received().Search(Arg.Any<string>(), Arg.Any<CancellationToken>());
		await _runs.Received(1).Finish(runId, RefreshStatus.Failed, 0,
			Arg.Is<string>(error => error.Contains("Crunchyroll") && error.Contains("challenge")), Now, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Skips_an_anime_whose_reply_it_cannot_read_and_carries_on()
	{
		var runId = Guid.NewGuid();
		Season(
			Anime(1, popularity: 10, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/BROKEN")),
			Anime(2, popularity: 5, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G2")));
		_crunchyroll.GetSeries("BROKEN", Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("unexpected shape"));
		Returns(_crunchyroll, Series("G2", "Frieren", Premiere, 1));

		await Service().Sync(Summer2026, runId, TestContext.Current.CancellationToken);

		(Saved(2)).Status.ShouldBe(DubMatchStatus.Matched);
		await _runs.Received(1).Finish(runId, RefreshStatus.Failed, 1,
			Arg.Is<string>(error => error.Contains("1 anime(s) could not be read")), Now, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Succeeds_and_counts_the_matched_animes()
	{
		var runId = Guid.NewGuid();
		Season(Anime(1, links: new StreamingLink("Crunchyroll", "https://crunchyroll.test/series/G1")), Anime(2));
		Returns(_crunchyroll, Series("G1", "Frieren", Premiere, 1));

		await Service().Sync(Summer2026, runId, TestContext.Current.CancellationToken);

		Received.InOrder(() =>
		{
			_runs.Begin(runId, Summer2026, RefreshKind.Dub, Now, Arg.Any<CancellationToken>());
			_runs.Finish(runId, RefreshStatus.Succeeded, 1, null, Now, Arg.Any<CancellationToken>());
		});
		await _runs.Received(2).Progress(runId, Arg.Any<int>(), Now, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Drops_the_matches_of_animes_no_longer_in_the_season_as_read_at_the_end()
	{
		// A season refresh landing mid-sync added anime 3: its match is not a stray.
		_animes.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).Returns([Anime(1)], [Anime(1), Anime(3)]);

		await Service().Sync(Summer2026, Guid.NewGuid(), TestContext.Current.CancellationToken);

		await _matches.Received(1).DeleteAllBut(Summer2026, Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(new[] { 1, 3 })), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Records_why_a_sync_died_and_rethrows()
	{
		var runId = Guid.NewGuid();
		_animes.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException("mongo"));

		await Should.ThrowAsync<TimeoutException>(() => Service().Sync(Summer2026, runId, TestContext.Current.CancellationToken));

		await _runs.Received(1).Finish(runId, RefreshStatus.Failed, 0, "mongo", Now, CancellationToken.None);
	}

	[Fact]
	public async Task Lists_the_matches_worth_an_admins_look()
	{
		var listed = new StreamingLink("Crunchyroll", "https://crunchyroll.test/some-slug");
		Season(
			Anime(1, "Unaligned"), Anime(2, "By search"), Anime(3, "By link"),
			Anime(4, "Listed but not found", links: listed), Anime(5, "Not found, not listed"), Anime(6, "Blocked"));
		_matches.GetBySeason(Summer2026, Arg.Any<CancellationToken>()).Returns([
			Stored(1, DubMatchMethod.Link, "U", DubMatchStatus.Unaligned),
			Stored(2, DubMatchMethod.Search, "S"),
			Stored(3, DubMatchMethod.Link, "L"),
			Stored(4, DubMatchMethod.Link, "N", DubMatchStatus.NotFound),
			Stored(5, DubMatchMethod.Link, "N", DubMatchStatus.NotFound)
		]);
		_overrides.GetBySourceIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([
			new DubOverrideEntity { SourceId = 6, Platform = DubPlatform.Crunchyroll, Mode = DubOverrideMode.Blocked, SeriesId = null, UpdatedAt = Now }
		]);

		var cases = await Service().GetCases(Summer2026, TestContext.Current.CancellationToken);

		cases.Select(@case => @case.SourceId).Order().ShouldBe([1, 2, 4, 6]);
		cases.Single(@case => @case.SourceId == 6).Override.ShouldBe(DubOverrideMode.Blocked);
		cases.Single(@case => @case.SourceId == 2).SeriesUrl.ShouldBe("https://crunchyroll.test/series/S");
	}

	[Fact]
	public async Task Refuses_to_pin_a_page_of_another_site()
	{
		_animes.GetBySourceId(1, Arg.Any<CancellationToken>()).Returns(Anime(1));

		var error = await Should.ThrowAsync<HttpException>(() => Service().SetOverride(1, DubPlatform.Crunchyroll,
			new DubOverrideRequest(DubOverrideMode.Pinned, "https://evil.test/series/G1"), TestContext.Current.CancellationToken));

		error.Code.ShouldBe(HttpStatusCode.BadRequest);
		await _overrides.DidNotReceive().Save(Arg.Any<DubOverrideBase>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Refuses_an_override_for_an_unknown_anime_or_platform()
	{
		(await Should.ThrowAsync<HttpException>(() => Service().SetOverride(404, DubPlatform.Crunchyroll,
			new DubOverrideRequest(DubOverrideMode.Blocked, null), TestContext.Current.CancellationToken))).Code.ShouldBe(HttpStatusCode.NotFound);

		(await Should.ThrowAsync<HttpException>(() => Service().SetOverride(1, DubPlatform.Adn,
			new DubOverrideRequest(DubOverrideMode.Blocked, null), TestContext.Current.CancellationToken))).Code.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Pins_a_series_and_applies_it_straight_away()
	{
		_animes.GetBySourceId(1, Arg.Any<CancellationToken>()).Returns(Anime(1));
		Returns(_crunchyroll, Series("G1", "Frieren", Premiere, 1, 2, 3));

		var result = await Service().SetOverride(1, DubPlatform.Crunchyroll,
			new DubOverrideRequest(DubOverrideMode.Pinned, "https://crunchyroll.test/series/G1"), TestContext.Current.CancellationToken);

		await _overrides.Received(1).Save(Arg.Is<DubOverrideBase>(saved => saved.Mode == DubOverrideMode.Pinned && saved.SeriesId == "G1"),
			Arg.Any<CancellationToken>());
		result.Error.ShouldBeNull();
		result.Case.Status.ShouldBe(DubMatchStatus.Matched);
		result.Case.Method.ShouldBe(DubMatchMethod.Pinned);
		result.Case.FrenchEpisodes.ShouldBe(3);
		result.Case.Override.ShouldBe(DubOverrideMode.Pinned);
	}

	[Fact]
	public async Task Keeps_an_override_the_platform_could_not_confirm_and_says_so()
	{
		_animes.GetBySourceId(1, Arg.Any<CancellationToken>()).Returns(Anime(1));
		_crunchyroll.GetSeries("G1", Arg.Any<CancellationToken>())
			.ThrowsAsync(new DubPlatformUnavailableException(DubPlatform.Crunchyroll, "Crunchyroll is rate limiting the requests (429)."));

		var result = await Service().SetOverride(1, DubPlatform.Crunchyroll,
			new DubOverrideRequest(DubOverrideMode.Pinned, "https://crunchyroll.test/series/G1"), TestContext.Current.CancellationToken);

		await _overrides.Received(1).Save(Arg.Any<DubOverrideBase>(), Arg.Any<CancellationToken>());
		result.Error.ShouldNotBeNull();
		result.Error.ShouldContain("next sync");
	}

	[Fact]
	public async Task Hands_an_anime_back_to_the_automatic_match()
	{
		_animes.GetBySourceId(1, Arg.Any<CancellationToken>()).Returns(Anime(1));

		var result = await Service().SetOverride(1, DubPlatform.Crunchyroll, new DubOverrideRequest(DubOverrideMode.Auto, null),
			TestContext.Current.CancellationToken);

		await _overrides.Received(1).Delete(1, DubPlatform.Crunchyroll, Arg.Any<CancellationToken>());
		result.Case.Override.ShouldBe(DubOverrideMode.Auto);
	}

	private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow()
		{
			return now;
		}
	}
}
