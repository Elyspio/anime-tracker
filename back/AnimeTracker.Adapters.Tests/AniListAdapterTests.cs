using AnimeTracker.Abstractions.Models.Base.Anime;
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
///     Drives the adapter against recorded AniList replies. Nothing here reaches the network.
/// </summary>
public class AniListAdapterTests
{
	private static readonly AnimeDate Summer2026 = new(2026, AnimeSeason.Summer);

	/// <summary>Serves the two recorded pages in order, then a page that ends the walk.</summary>
	private static FakeHttpMessageHandler RecordedSeason()
	{
		var pages = new Queue<string>([
			Fixtures.Read("season-summer-2026-page1.json"),
			Fixtures.Read("season-summer-2026-page2.json")
		]);

		return new FakeHttpMessageHandler(_ =>
			pages.Count > 0 ? FakeHttpMessageHandler.Json(pages.Dequeue()) : FakeHttpMessageHandler.EmptyPage());
	}

	private static AniListAdapter Build(FakeHttpMessageHandler handler, AniListOptions? options = null)
	{
		var configured = Options.Create(options ?? new AniListOptions { PageSize = 5 });

		var client = new AniListClient(new FakeHttpClientFactory(handler), configured, NullLogger<AniListClient>.Instance);

		return new AniListAdapter(client, new MediaAssembler(), configured, NullLogger<AniListAdapter>.Instance);
	}

	private static async Task<AnimeBase[]> RecordedAnimes()
	{
		return await Build(RecordedSeason()).GetSeason(Summer2026, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Pages_until_the_source_says_there_is_no_next_page()
	{
		var handler = RecordedSeason();

		var animes = await Build(handler).GetSeason(Summer2026, TestContext.Current.CancellationToken);

		// Two recorded pages of five, then the page that stops the walk.
		animes.Length.ShouldBe(10);
		handler.Requests.Count.ShouldBe(3);
		handler.Requests.ShouldAllBe(request => request.Method == HttpMethod.Post);
	}

	[Fact]
	public async Task Asks_for_the_season_it_was_given()
	{
		var handler = RecordedSeason();

		await Build(handler).GetSeason(new AnimeDate(2027, AnimeSeason.Winter), TestContext.Current.CancellationToken);

		var body = handler.Requests[0].Body;

		body.ShouldContain("\"season\":\"WINTER\"");
		body.ShouldContain("\"year\":2027");
		body.ShouldContain("\"page\":1");
	}

	[Fact]
	public async Task Maps_a_media_node_onto_the_domain_model()
	{
		var anime = (await RecordedAnimes()).Single(item => item.SourceId == 178789);

		anime.Title.ShouldBe("Mushoku Tensei III: Isekai Ittara Honki Dasu");
		anime.Studio.ShouldBe("Studio Bind");
		anime.Url.ShouldBe("https://anilist.co/anime/178789");
		anime.Format.ShouldBe(AnimeFormat.Tv);
		anime.IsAdult.ShouldBeFalse();
		anime.EpisodesCount.ShouldBe(14);
		anime.Popularity.ShouldBe(157452);
		anime.Genres.ShouldBe(["Adventure", "Drama", "Ecchi", "Fantasy"]);
		anime.ImageUrl.ShouldStartWith("https://s4.anilist.co/");
		anime.Date.ShouldBe(Summer2026);
	}

	[Fact]
	public async Task Converts_the_score_out_of_ten()
	{
		// AniList grades out of 100 and this one is at 84.
		(await RecordedAnimes()).Single(item => item.SourceId == 178789).Score.ShouldBe(8.4);
	}

	[Fact]
	public async Task Sums_the_score_distribution_into_a_vote_count()
	{
		// The API publishes the histogram but no total; 14008 is the sum of its buckets.
		(await RecordedAnimes()).Single(item => item.SourceId == 178789).VotesCount.ShouldBe(14008);
	}

	[Fact]
	public async Task Dates_episodes_by_the_japanese_broadcast_day()
	{
		var episodes = (await RecordedAnimes()).Single(item => item.SourceId == 178789).Episodes.ToArray();

		episodes.Length.ShouldBe(14);
		episodes.Select(episode => episode.Number).ShouldBe(Enumerable.Range(1, 14));
		episodes[0].ReleaseDate.ShouldBe(new DateOnly(2026, 7, 4));

		// Episode 13's slot is 2026-09-20 15:00 UTC, which is midnight on the 21st in Tokyo. Read as
		// UTC it would land a day early — and late-night slots are the norm.
		episodes[12].ReleaseDate.ShouldBe(new DateOnly(2026, 9, 21));
	}

	[Fact]
	public async Task Collects_every_other_title_the_source_knows()
	{
		var anime = (await RecordedAnimes()).Single(item => item.SourceId == 178789);

		anime.AlternativeTitles.ShouldContain("Mushoku Tensei: Jobless Reincarnation Season 3");
		anime.AlternativeTitles.ShouldContain("Mushoku Tensei: Isekai Ittara Honki Dasu 3rd Season");
		anime.AlternativeTitles.ShouldNotContain(anime.Title);
	}

	[Fact]
	public async Task Surfaces_an_error_reported_inside_a_200()
	{
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.GraphQlError("Invalid season"));

		var error = await Should.ThrowAsync<HttpRequestException>(
			() => Build(handler).GetSeason(Summer2026, TestContext.Current.CancellationToken));

		error.Message.ShouldContain("Invalid season");
	}

	[Fact]
	public async Task Stops_paging_at_the_configured_ceiling()
	{
		// A source that never stops saying "there is more" must not page forever.
		var handler = new FakeHttpMessageHandler(_ =>
			FakeHttpMessageHandler.Json("""{"data":{"Page":{"pageInfo":{"hasNextPage":true},"media":[]}}}"""));

		await Build(handler, new AniListOptions { PageSize = 5, MaxPages = 3 })
			.GetSeason(Summer2026, TestContext.Current.CancellationToken);

		handler.Requests.Count.ShouldBe(3);
	}
}
