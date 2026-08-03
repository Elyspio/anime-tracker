using System.Linq.Expressions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>
///     The refresh choreography: who is asked, in what order, and what the run ends up saying. Every
///     port is substituted, so nothing here touches a source, a database or a scheduler.
/// </summary>
public class AnimeServiceTests
{
	private static readonly AnimeDate Summer2026 = new(2026, AnimeSeason.Summer);

	private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

	private readonly IAnimeRepository _animes = Substitute.For<IAnimeRepository>();

	private readonly IRefreshRunRepository _runs = Substitute.For<IRefreshRunRepository>();

	private readonly IAnimeSourceAdapter _source = Substitute.For<IAnimeSourceAdapter>();

	private readonly IHangfireJobAdapter _jobs = Substitute.For<IHangfireJobAdapter>();

	private readonly AnimeService _service;

	public AnimeServiceTests()
	{
		var clock = new FakeTimeProvider(Now);

		_service = new AnimeService(_animes, _runs, _source, _jobs, clock, NullLogger<AnimeService>.Instance);
	}

	private static AnimeBase Anime(int sourceId)
	{
		return new AnimeBase
		{
			SourceId = sourceId,
			Date = Summer2026,
			Title = $"Anime {sourceId}",
			Description = "",
			Studio = "",
			ImageUrl = "",
			Url = "",
			Format = AnimeFormat.Tv,
			IsAdult = false,
			Score = null,
			Popularity = 0,
			VotesCount = null,
			EpisodesCount = 12,
			Genres = [],
			Episodes = []
		};
	}

	private static RefreshRunEntity Run(Guid runId, RefreshStatus status = RefreshStatus.Running)
	{
		return new RefreshRunEntity
		{
			Id = ObjectId.GenerateNewId(),
			RunId = runId,
			Date = Summer2026,
			Status = status,
			Total = 0,
			StartedAt = Now,
			UpdatedAt = Now,
			FinishedAt = null,
			Error = null
		};
	}

	[Fact]
	public async Task Queues_a_refresh_when_the_season_is_idle()
	{
		_runs.GetActive(Summer2026, Arg.Any<CancellationToken>()).Returns((RefreshRunEntity?)null);
		_runs.Queue(Arg.Any<Guid>(), Summer2026, Now, Arg.Any<CancellationToken>())
			.Returns(call => Run(call.Arg<Guid>(), RefreshStatus.Queued));

		var result = await _service.QueueRefresh(Summer2026, TestContext.Current.CancellationToken);

		result.AlreadyRunning.ShouldBeFalse();
		result.Run.Status.ShouldBe(RefreshStatus.Queued);
		_jobs.Received(1).Enqueue(Arg.Any<Expression<Func<AnimeRefreshJob, Task>>>());
	}

	[Fact]
	public async Task Answers_with_the_run_already_in_flight_instead_of_queuing_a_second()
	{
		// This is what the controller turns into a 409. Enqueuing anyway would mean two identical
		// refreshes racing to write the same season.
		var existing = Run(Guid.NewGuid());
		_runs.GetActive(Summer2026, Arg.Any<CancellationToken>()).Returns(existing);

		var result = await _service.QueueRefresh(Summer2026, TestContext.Current.CancellationToken);

		result.AlreadyRunning.ShouldBeTrue();
		result.Run.RunId.ShouldBe(existing.RunId);
		_jobs.DidNotReceive().Enqueue(Arg.Any<Expression<Func<AnimeRefreshJob, Task>>>());
		await _runs.DidNotReceive().Queue(Arg.Any<Guid>(), Arg.Any<AnimeDate>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Stores_what_the_source_returned_and_closes_the_run()
	{
		var runId = Guid.NewGuid();
		_source.GetSeason(Summer2026, Arg.Any<CancellationToken>()).Returns([Anime(1), Anime(2), Anime(3)]);

		await _service.RefreshAll(Summer2026, runId, TestContext.Current.CancellationToken);

		await _animes.Received(1).Refresh(Summer2026,
			Arg.Is<IReadOnlyCollection<AnimeBase>>(animes => animes.Count == 3), Arg.Any<CancellationToken>());
		await _runs.Received(1).Finish(runId, RefreshStatus.Succeeded, 3, null, Now, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Marks_the_run_as_running_before_fetching_anything()
	{
		var runId = Guid.NewGuid();
		_source.GetSeason(Summer2026, Arg.Any<CancellationToken>()).Returns([Anime(1)]);

		await _service.RefreshAll(Summer2026, runId, TestContext.Current.CancellationToken);

		Received.InOrder(() =>
		{
			_runs.Begin(runId, Summer2026, Now, Arg.Any<CancellationToken>());
			_source.GetSeason(Summer2026, Arg.Any<CancellationToken>());
			_animes.Refresh(Summer2026, Arg.Any<IReadOnlyCollection<AnimeBase>>(), Arg.Any<CancellationToken>());
		});
	}

	[Fact]
	public async Task Records_why_a_refresh_failed_and_lets_the_scheduler_see_it()
	{
		var runId = Guid.NewGuid();
		_source.GetSeason(Summer2026, Arg.Any<CancellationToken>())
			.ThrowsAsync(new HttpRequestException("AniList rejected the query: Invalid season"));

		var error = await Should.ThrowAsync<HttpRequestException>(
			() => _service.RefreshAll(Summer2026, runId, TestContext.Current.CancellationToken));

		error.Message.ShouldContain("Invalid season");

		// Recorded as failed, and rethrown so Hangfire does not file it as a success.
		await _runs.Received(1).Finish(runId, RefreshStatus.Failed, 0,
			Arg.Is<string>(message => message.Contains("Invalid season")), Now, Arg.Any<CancellationToken>());

		// Nothing was written: a failed fetch must not empty the season it could not read.
		await _animes.DidNotReceive().Refresh(Arg.Any<AnimeDate>(), Arg.Any<IReadOnlyCollection<AnimeBase>>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Closes_a_failed_run_even_when_the_caller_cancelled()
	{
		// The token that killed the run cannot be the one used to record why it died, or the run
		// would stay marked as running forever.
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var runId = Guid.NewGuid();
		_source.GetSeason(Summer2026, Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

		await Should.ThrowAsync<OperationCanceledException>(
			() => _service.RefreshAll(Summer2026, runId, cancellation.Token));

		await _runs.Received(1).Finish(runId, RefreshStatus.Failed, 0, Arg.Any<string>(), Now, CancellationToken.None);
	}

	/// <summary>A clock that does not move, so a run's timestamps are exactly assertable.</summary>
	private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow()
		{
			return now;
		}
	}
}
