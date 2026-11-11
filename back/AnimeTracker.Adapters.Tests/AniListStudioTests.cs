using System.Net;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Adapters.AniList.Adapters;
using AnimeTracker.Adapters.AniList.Assemblers;
using AnimeTracker.Adapters.AniList.Configs;
using AnimeTracker.Adapters.AniList.GraphQL;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Drives the studio query against Studio Bind's filmography as AniList returned it, five works a
///     page. Nothing here reaches the network.
/// </summary>
public class AniListStudioTests
{
	private const int StudioBind = 6309;

	private static FakeHttpMessageHandler RecordedStudio()
	{
		var pages = new Queue<string>([
			Fixtures.Read("studio-6309-page1.json"),
			Fixtures.Read("studio-6309-page2.json"),
			Fixtures.Read("studio-6309-page3.json")
		]);

		return new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(pages.Dequeue()));
	}

	private static AniListAdapter Build(FakeHttpMessageHandler handler, AniListOptions? options = null)
	{
		var configured = Options.Create(options ?? new AniListOptions { PageSize = 5 });

		var client = new AniListClient(new FakeHttpClientFactory(handler), configured, NullLogger<AniListClient>.Instance);

		return new AniListAdapter(client, new MediaAssembler(), configured, NullLogger<AniListAdapter>.Instance);
	}

	private static async Task<StudioFilmographyBase> Recorded()
	{
		return (await Build(RecordedStudio()).GetStudio(StudioBind, TestContext.Current.CancellationToken))!;
	}

	[Fact]
	public async Task Pages_until_the_source_says_there_is_no_next_page()
	{
		var handler = RecordedStudio();

		var studio = await Build(handler).GetStudio(StudioBind, TestContext.Current.CancellationToken);

		// Five, five, then the two that end the walk: the third page says it is the last.
		studio!.Works.Count.ShouldBe(12);
		handler.Requests.Count.ShouldBe(3);
	}

	[Fact]
	public async Task Asks_for_the_main_works_of_the_studio_it_was_given()
	{
		var handler = RecordedStudio();

		await Build(handler).GetStudio(StudioBind, TestContext.Current.CancellationToken);

		handler.Requests[0].Body.ShouldContain("\"id\":6309");
		handler.Requests[0].Body.ShouldContain("isMain: true");
		handler.Requests[1].Body.ShouldContain("\"page\":2");
	}

	[Fact]
	public async Task Names_the_studio()
	{
		var studio = await Recorded();

		studio.StudioId.ShouldBe(StudioBind);
		studio.Name.ShouldBe("Studio Bind");
		studio.Url.ShouldBe("https://anilist.co/studio/6309");
	}

	[Fact]
	public async Task Maps_a_work()
	{
		var work = (await Recorded()).Works.Single(item => item.SourceId == 178789);

		work.Title.ShouldBe("Mushoku Tensei III: Isekai Ittara Honki Dasu");
		work.Url.ShouldBe("https://anilist.co/anime/178789");
		work.Format.ShouldBe(AnimeFormat.Tv);
		work.IsAdult.ShouldBeFalse();
		work.EpisodesCount.ShouldBe(14);
		work.Score.ShouldBe(8.6);
		work.ImageUrl.ShouldStartWith("https://s4.anilist.co/");
		work.StartDate.ShouldBe(new StudioWorkDate(2026, 7, 4));
		work.Season.ShouldBe(new AnimeDate(2026, AnimeSeason.Summer));
	}

	[Fact]
	public async Task Keeps_a_date_that_is_only_a_year()
	{
		// Announced for 2027 and nothing more: no month, no day, no season, no grade, no episode count.
		var work = (await Recorded()).Works.Single(item => item.SourceId == 217434);

		work.StartDate.ShouldBe(new StudioWorkDate(2027, null, null));
		work.Season.ShouldBeNull();
		work.Score.ShouldBeNull();
		work.EpisodesCount.ShouldBeNull();
	}

	[Fact]
	public async Task Derives_the_season_the_source_left_out_from_the_start_month()
	{
		// A special dated 2022-12-31 that AniList lists under no season.
		var work = (await Recorded()).Works.Single(item => item.SourceId == 160236);

		work.Format.ShouldBe(AnimeFormat.Special);
		work.Season.ShouldBe(new AnimeDate(2022, AnimeSeason.Fall));
	}

	[Fact]
	public async Task Answers_null_for_a_studio_the_source_does_not_know()
	{
		// What AniList really sends for an unknown id: a 404, with the error in the body.
		var handler = new FakeHttpMessageHandler(_ =>
			FakeHttpMessageHandler.Json("""{"errors":[{"message":"Not Found.","status":404}],"data":{"Studio":null}}""", HttpStatusCode.NotFound));

		(await Build(handler).GetStudio(99999999, TestContext.Current.CancellationToken)).ShouldBeNull();
	}

	[Fact]
	public async Task Surfaces_an_error_reported_inside_a_200()
	{
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.GraphQlError("Too complex"));

		var error = await Should.ThrowAsync<HttpRequestException>(
			() => Build(handler).GetStudio(StudioBind, TestContext.Current.CancellationToken));

		error.Message.ShouldContain("Too complex");
	}

	[Fact]
	public async Task Leaves_out_what_is_not_an_anime_and_what_came_back_twice()
	{
		const string page = """
			{"data":{"Studio":{"id":1,"name":" Studio ","siteUrl":null,"media":{"pageInfo":{"hasNextPage":false},"nodes":[
				{"id":10,"type":"ANIME","isAdult":false},
				{"id":10,"type":"ANIME","isAdult":false},
				{"id":11,"type":"MANGA","isAdult":false}
			]}}}}
			""";

		var studio = await Build(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(page))).GetStudio(1, TestContext.Current.CancellationToken);

		studio!.Name.ShouldBe("Studio");
		studio.Url.ShouldBeEmpty();
		studio.Works.Select(work => work.SourceId).ShouldBe([10]);
	}

	[Fact]
	public async Task Stops_paging_at_the_configured_ceiling()
	{
		var handler = new FakeHttpMessageHandler(_ =>
			FakeHttpMessageHandler.Json("""{"data":{"Studio":{"id":1,"name":"S","media":{"pageInfo":{"hasNextPage":true},"nodes":[]}}}}"""));

		await Build(handler, new AniListOptions { PageSize = 5, MaxPages = 3 }).GetStudio(1, TestContext.Current.CancellationToken);

		handler.Requests.Count.ShouldBe(3);
	}

	[Fact]
	public async Task Names_the_first_main_studio_of_one_anime()
	{
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(Fixtures.Read("media-studio-178789.json")));

		var studio = await Build(handler).GetMainStudio(178789, TestContext.Current.CancellationToken);

		studio.ShouldBe(new StudioReference(StudioBind, "Studio Bind"));
		handler.Requests.Single().Body.ShouldContain("\"id\":178789");
		handler.Requests.Single().Body.ShouldContain("isMain: true");
	}

	[Fact]
	public async Task Names_no_studio_for_an_anime_that_credits_none()
	{
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"data":{"Media":{"id":1,"studios":{"nodes":[]}}}}"""));

		(await Build(handler).GetMainStudio(1, TestContext.Current.CancellationToken)).ShouldBeNull();
	}

	[Fact]
	public async Task Names_no_studio_for_an_anime_the_source_does_not_know()
	{
		var handler = new FakeHttpMessageHandler(_ =>
			FakeHttpMessageHandler.Json("""{"errors":[{"message":"Not Found.","status":404}],"data":{"Media":null}}""", HttpStatusCode.NotFound));

		(await Build(handler).GetMainStudio(99999999, TestContext.Current.CancellationToken)).ShouldBeNull();
	}
}
