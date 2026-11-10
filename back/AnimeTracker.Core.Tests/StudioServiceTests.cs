using System.Net;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Core.Assemblers;
using AnimeTracker.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>
///     When a filmography is read from the store, when the source is asked, and what is shown of it.
///     Every port is substituted.
/// </summary>
public class StudioServiceTests
{
	private const int StudioId = 6309;

	private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

	private readonly IStudioFilmographyRepository _studios = Substitute.For<IStudioFilmographyRepository>();

	private readonly IAnimeRepository _animes = Substitute.For<IAnimeRepository>();

	private readonly IAnimeSourceAdapter _source = Substitute.For<IAnimeSourceAdapter>();

	private readonly StudioService _service;

	public StudioServiceTests()
	{
		_service = new StudioService(_studios, _animes, _source, new FixedTimeProvider(Now), NullLogger<StudioService>.Instance);

		_studios.Save(Arg.Any<StudioFilmographyBase>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
			.Returns(call => Entity(call.Arg<StudioFilmographyBase>().Works.ToArray(), call.Arg<DateTimeOffset>()));
	}

	private static StudioWork Work(int sourceId, StudioWorkDate? start, string title = "", AnimeFormat format = AnimeFormat.Tv)
	{
		return new StudioWork
		{
			SourceId = sourceId,
			Title = title == "" ? $"Work {sourceId}" : title,
			ImageUrl = "",
			Url = "",
			Format = format,
			IsAdult = false,
			Score = null,
			EpisodesCount = null,
			StartDate = start,
			Season = null
		};
	}

	private static StudioFilmographyBase Source(params StudioWork[] works)
	{
		return new StudioFilmographyBase { StudioId = StudioId, Name = "Studio Bind", Url = "https://anilist.co/studio/6309", Works = works };
	}

	private static StudioFilmographyEntity Entity(StudioWork[] works, DateTimeOffset fetchedAt)
	{
		return new StudioFilmographyEntity { StudioId = StudioId, Name = "Studio Bind", Url = "https://anilist.co/studio/6309", Works = works, FetchedAt = fetchedAt };
	}

	private void Stored(params StudioWork[] works)
	{
		_studios.Get(StudioId, Arg.Any<CancellationToken>()).Returns(Entity(works, Now.AddDays(-30)));
	}

	private void Credited()
	{
		_animes.HasStudio(StudioId, Arg.Any<CancellationToken>()).Returns(true);
	}

	[Fact]
	public async Task Serves_a_stored_filmography_without_asking_the_source()
	{
		Stored(Work(1, new StudioWorkDate(2024, 4, 8)));

		var studio = await _service.Get(StudioId, TestContext.Current.CancellationToken);

		studio.Name.ShouldBe("Studio Bind");
		// However old: nothing refreshes a filmography but the admin's button.
		studio.FetchedAt.ShouldBe(Now.AddDays(-30));
		await _source.DidNotReceiveWithAnyArgs().GetStudio(default, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Fetches_and_stores_the_first_time_a_credited_studio_is_asked_for()
	{
		Credited();
		_source.GetStudio(StudioId, Arg.Any<CancellationToken>()).Returns(Source(Work(1, new StudioWorkDate(2024, 4, 8))));

		var studio = await _service.Get(StudioId, TestContext.Current.CancellationToken);

		studio.Works.Single().SourceId.ShouldBe(1);
		studio.FetchedAt.ShouldBe(Now);
		await _studios.Received(1).Save(Arg.Is<StudioFilmographyBase>(saved => saved.StudioId == StudioId), Now, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Refuses_a_studio_no_stored_anime_credits_without_asking_the_source()
	{
		// The read is anonymous: walking the ids must not spend the source's rate limit.
		var error = await Should.ThrowAsync<HttpException>(() => _service.Get(StudioId, TestContext.Current.CancellationToken));

		error.Code.ShouldBe(HttpStatusCode.NotFound);
		await _source.DidNotReceiveWithAnyArgs().GetStudio(default, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Answers_404_when_the_source_does_not_know_the_studio()
	{
		Credited();
		_source.GetStudio(StudioId, Arg.Any<CancellationToken>()).Returns((StudioFilmographyBase?)null);

		var error = await Should.ThrowAsync<HttpException>(() => _service.Get(StudioId, TestContext.Current.CancellationToken));

		error.Code.ShouldBe(HttpStatusCode.NotFound);
		await _studios.DidNotReceiveWithAnyArgs().Save(default!, default, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Reports_a_source_failure_as_a_bad_gateway_carrying_its_message()
	{
		Credited();
		_source.GetStudio(StudioId, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("AniList rejected the query: Too complex"));

		var error = await Should.ThrowAsync<HttpException>(() => _service.Get(StudioId, TestContext.Current.CancellationToken));

		error.Code.ShouldBe(HttpStatusCode.BadGateway);
		error.Message.ShouldContain("Too complex");
	}

	[Fact]
	public async Task Refresh_asks_the_source_even_when_a_filmography_is_stored()
	{
		Stored(Work(1, new StudioWorkDate(2024, 4, 8)));
		_source.GetStudio(StudioId, Arg.Any<CancellationToken>())
			.Returns(Source(Work(1, new StudioWorkDate(2024, 4, 8)), Work(2, new StudioWorkDate(2026, 7, 4))));

		var studio = await _service.Refresh(StudioId, TestContext.Current.CancellationToken);

		studio.Works.Select(work => work.SourceId).ShouldBe([2, 1]);
		studio.FetchedAt.ShouldBe(Now);
	}

	[Fact]
	public async Task Refresh_keeps_what_is_stored_when_the_source_fails()
	{
		Stored(Work(1, new StudioWorkDate(2024, 4, 8)));
		_source.GetStudio(StudioId, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("down"));

		await Should.ThrowAsync<HttpException>(() => _service.Refresh(StudioId, TestContext.Current.CancellationToken));

		await _studios.DidNotReceiveWithAnyArgs().Save(default!, default, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Two_first_reads_at_once_cost_the_source_one_fetch()
	{
		Credited();

		StudioFilmographyEntity? saved = null;
		var release = new TaskCompletionSource<StudioFilmographyBase?>();

		_studios.Get(StudioId, Arg.Any<CancellationToken>()).Returns(_ => saved);
		_studios.Save(Arg.Any<StudioFilmographyBase>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
			.Returns(call => saved = Entity(call.Arg<StudioFilmographyBase>().Works.ToArray(), Now));
		_source.GetStudio(StudioId, Arg.Any<CancellationToken>()).Returns(release.Task);

		var first = _service.Get(StudioId, TestContext.Current.CancellationToken);
		var second = _service.Get(StudioId, TestContext.Current.CancellationToken);

		release.SetResult(Source(Work(1, new StudioWorkDate(2024, 4, 8))));

		(await first).Works.Count.ShouldBe(1);
		(await second).Works.Count.ShouldBe(1);
		await _source.Received(1).GetStudio(StudioId, Arg.Any<CancellationToken>());
	}

	[Fact]
	public void Orders_newest_first_with_the_undated_ahead()
	{
		var ordered = StudioAssembler.Order([
			Work(1, new StudioWorkDate(2021, 1, 11)),
			Work(2, new StudioWorkDate(2026, 7, 4)),
			Work(3, null, "Zeta"),
			Work(4, new StudioWorkDate(2026, 10, 1)),
			Work(5, null, "Alpha"),
			Work(6, new StudioWorkDate(2026, 7, 20))
		]);

		ordered.Select(work => work.SourceId).ShouldBe([5, 3, 4, 6, 2, 1]);
	}

	[Fact]
	public void Orders_a_partial_date_ahead_of_every_known_one_in_its_year()
	{
		// "2027" is later than anything dated in 2027, as far as anyone knows; same for a month without a day.
		var ordered = StudioAssembler.Order([
			Work(1, new StudioWorkDate(2027, 12, 25)),
			Work(2, new StudioWorkDate(2027, null, null)),
			Work(3, new StudioWorkDate(2027, 12, null)),
			Work(4, new StudioWorkDate(2028, 1, 1))
		]);

		ordered.Select(work => work.SourceId).ShouldBe([4, 2, 3, 1]);
	}

	[Fact]
	public async Task Leaves_music_videos_out_of_what_is_shown()
	{
		Stored(Work(1, new StudioWorkDate(2024, 4, 8)), Work(2, new StudioWorkDate(2025, 1, 1), format: AnimeFormat.Music));

		var studio = await _service.Get(StudioId, TestContext.Current.CancellationToken);

		studio.Works.Select(work => work.SourceId).ShouldBe([1]);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow()
		{
			return now;
		}
	}
}
