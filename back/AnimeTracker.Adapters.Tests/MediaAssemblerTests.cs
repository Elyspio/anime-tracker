using System.Text.Json;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Adapters.AniList.Assemblers;
using AnimeTracker.Adapters.AniList.GraphQL;
using Shouldly;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Pins the mapping *rules* over hand-written nodes, where the recorded fixtures pin the API's
///     shape. Both layers matter: a fixture cannot express a field AniList has never returned yet.
/// </summary>
public class MediaAssemblerTests
{
	private static readonly AnimeDate Summer2026 = new(2026, AnimeSeason.Summer);

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	private static AnimeBase Convert(string node)
	{
		var media = JsonSerializer.Deserialize<Media>(node, Json)!;

		return new MediaAssembler().Convert(Summer2026, media);
	}

	[Fact]
	public void Prefers_the_highest_resolution_cover_and_falls_back_to_large()
	{
		Convert("""{"id":1,"coverImage":{"extraLarge":"high","large":"medium"}}""").ImageUrl.ShouldBe("high");
		Convert("""{"id":1,"coverImage":{"extraLarge":null,"large":"medium"}}""").ImageUrl.ShouldBe("medium");
		Convert("""{"id":1}""").ImageUrl.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("TV", AnimeFormat.Tv)]
	[InlineData("TV_SHORT", AnimeFormat.TvShort)]
	[InlineData("ONA", AnimeFormat.Ona)]
	[InlineData("OVA", AnimeFormat.Ova)]
	[InlineData("MOVIE", AnimeFormat.Movie)]
	[InlineData("SPECIAL", AnimeFormat.Special)]
	[InlineData("MUSIC", AnimeFormat.Music)]
	public void Maps_every_format_it_knows(string format, AnimeFormat expected)
	{
		Convert($$"""{"id":1,"format":"{{format}}"}""").Format.ShouldBe(expected);
	}

	[Theory]
	[InlineData("\"MANGA\"")]
	[InlineData("null")]
	public void Turns_a_format_it_does_not_know_into_Unknown(string format)
	{
		// A new media type upstream must not take a whole season down with it.
		Convert($$"""{"id":1,"format":{{format}}}""").Format.ShouldBe(AnimeFormat.Unknown);
	}

	[Fact]
	public void Prefers_the_romaji_title()
	{
		var anime = Convert("""{"id":1,"title":{"romaji":"Sousou no Frieren","english":"Frieren"}}""");

		anime.Title.ShouldBe("Sousou no Frieren");
	}

	[Fact]
	public void Falls_back_to_the_english_title_when_there_is_no_romaji()
	{
		Convert("""{"id":1,"title":{"romaji":null,"english":"Frieren"}}""").Title.ShouldBe("Frieren");
	}

	[Fact]
	public void Collects_the_english_native_and_synonym_titles()
	{
		var anime = Convert("""
			{"id":1,"title":{"romaji":"Sousou no Frieren","english":"Frieren: Beyond Journey's End","native":"葬送のフリーレン"},
			 "synonyms":["Frieren at the Funeral"]}
			""");

		anime.AlternativeTitles.ShouldBe(["Frieren: Beyond Journey's End", "葬送のフリーレン", "Frieren at the Funeral"]);
	}

	[Fact]
	public void Leaves_the_displayed_title_and_duplicates_out_of_the_alternatives()
	{
		// Synonyms routinely echo the other titles, casing aside; the fallback title is already shown.
		var anime = Convert("""
			{"id":1,"title":{"romaji":null,"english":"Frieren","native":" 葬送のフリーレン "},
			 "synonyms":["frieren","葬送のフリーレン","", "  ", null,"Sousou no Frieren","SOUSOU NO FRIEREN"]}
			""");

		anime.Title.ShouldBe("Frieren");
		anime.AlternativeTitles.ShouldBe(["葬送のフリーレン", "Sousou no Frieren"]);
	}

	[Fact]
	public void Keeps_an_unannounced_episode_count_null()
	{
		// A show scheduled before its total is announced — the prediction must stay UnknownEnd.
		var anime = Convert("""{"id":1,"episodes":null,"airingSchedule":{"nodes":[{"episode":1,"airingAt":1783162800}]}}""");

		anime.EpisodesCount.ShouldBeNull();
	}

	[Fact]
	public void Leaves_an_ungraded_anime_without_a_score()
	{
		var anime = Convert("""{"id":1,"averageScore":null,"stats":null}""");

		anime.Score.ShouldBeNull();
		anime.VotesCount.ShouldBeNull();
	}

	[Fact]
	public void Counts_no_votes_rather_than_none_when_the_histogram_is_empty()
	{
		// An empty distribution is a real answer — nobody graded it — and differs from no stats at all.
		Convert("""{"id":1,"stats":{"scoreDistribution":[]}}""").VotesCount.ShouldBe(0);
	}

	[Fact]
	public void Orders_the_schedule_by_episode_number()
	{
		var anime = Convert("""
			{"id":1,"airingSchedule":{"nodes":[
			  {"episode":3,"airingAt":1783767600},
			  {"episode":1,"airingAt":1783162800},
			  {"episode":2,"airingAt":1783465200}]}}
			""");

		anime.Episodes.Select(episode => episode.Number).ShouldBe([1, 2, 3]);
	}

	[Fact]
	public void Dates_a_late_night_slot_by_the_day_it_airs_in_tokyo()
	{
		// 2026-09-20 15:00 UTC is 2026-09-21 00:00 in Japan. The 01:30 JST slot is an entire genre's
		// habit, so reading these as UTC would move a whole schedule back by one day.
		var anime = Convert("""{"id":1,"airingSchedule":{"nodes":[{"episode":1,"airingAt":1789916400}]}}""");

		anime.Episodes.Single().ReleaseDate.ShouldBe(new DateOnly(2026, 9, 21));
	}

	[Fact]
	public void Keeps_only_the_streaming_links()
	{
		var anime = Convert("""
			{"id":1,"externalLinks":[
			  {"site":"Twitter","type":"SOCIAL","isDisabled":false,"url":"https://x.com/frieren_pr"},
			  {"site":"Official Site","type":"INFO","isDisabled":false,"url":"https://frieren-anime.jp/"},
			  {"site":"Crunchyroll","type":"STREAMING","isDisabled":false,"url":"https://www.crunchyroll.com/series/GG5H5XQMD/frieren"}]}
			""");

		anime.StreamingLinks.ShouldBe([new StreamingLink("Crunchyroll", "https://www.crunchyroll.com/series/GG5H5XQMD/frieren")]);
	}

	[Fact]
	public void Keeps_a_link_the_source_has_flagged_as_disabled()
	{
		// AniList sets isDisabled on links its checker could not fetch. On a recent season most of the
		// Crunchyroll ones carry it, yet every one that named a series pointed at a series that exists.
		var anime = Convert("""
			{"id":1,"externalLinks":[
			  {"site":"Crunchyroll","type":"STREAMING","isDisabled":true,"url":"https://www.crunchyroll.com/series/G3KHEVDJ7/the-apothecary-diaries"},
			  {"site":"Netflix","type":"STREAMING","isDisabled":false,"url":"https://www.netflix.com/title/82760630"}]}
			""");

		anime.StreamingLinks.Select(link => link.Site).ShouldBe(["Crunchyroll", "Netflix"]);
	}

	[Theory]
	[InlineData("https://www.crunchyroll.com/")]
	[InlineData("https://Crunchyroll.com")]
	[InlineData("http://www.netflix.com")]
	public void Drops_a_link_to_the_bare_home_page_of_a_platform(string url)
	{
		// A home page does not say which show it was meant for.
		Convert($$"""{"id":1,"externalLinks":[{"site":"Platform","type":"STREAMING","isDisabled":false,"url":"{{url}}"}]}""")
			.StreamingLinks.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("javascript:alert(document.cookie)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	[InlineData("ftp://files.example.com/frieren")]
	[InlineData("/series/frieren")]
	[InlineData("not a link")]
	[InlineData("")]
	public void Drops_a_link_that_is_not_an_absolute_http_address(string url)
	{
		// The link is rendered as an href, and anyone with an account can edit the source.
		Convert($$"""{"id":1,"externalLinks":[{"site":"Platform","type":"STREAMING","isDisabled":false,"url":"{{url}}"}]}""")
			.StreamingLinks.ShouldBeEmpty();
	}

	[Fact]
	public void Keeps_the_first_link_of_a_platform_named_twice_whatever_the_casing()
	{
		var anime = Convert("""
			{"id":1,"externalLinks":[
			  {"site":"OceanVeil","type":"STREAMING","isDisabled":false,"url":"https://oceanveil.net/anime_titles/461"},
			  {"site":"oceanveil","type":"STREAMING","isDisabled":false,"url":"https://oceanveil.net/anime_titles/462"}]}
			""");

		anime.StreamingLinks.Single().Url.ShouldBe("https://oceanveil.net/anime_titles/461");
	}

	[Fact]
	public void Drops_a_link_with_no_platform_name()
	{
		Convert("""{"id":1,"externalLinks":[{"site":null,"type":"STREAMING","isDisabled":false,"url":"https://example.com/frieren"}]}""")
			.StreamingLinks.ShouldBeEmpty();
	}

	[Fact]
	public void Survives_a_node_with_nothing_but_an_id()
	{
		// Announced-but-empty entries appear in every season and must not break the walk.
		var anime = Convert("""{"id":42}""");

		anime.SourceId.ShouldBe(42);
		anime.Title.ShouldBeEmpty();
		anime.AlternativeTitles.ShouldBeEmpty();
		anime.Genres.ShouldBeEmpty();
		anime.Episodes.ShouldBeEmpty();
		anime.StreamingLinks.ShouldBeEmpty();
		anime.Popularity.ShouldBe(0);
	}

	[Fact]
	public void Keeps_the_id_of_the_first_main_studio_beside_its_name()
	{
		var anime = Convert("""{"id":1,"studios":{"nodes":[{"id":6309,"name":"Studio Bind"},{"id":11,"name":"MADHOUSE"}]}}""");

		anime.Studio.ShouldBe("Studio Bind");
		anime.StudioId.ShouldBe(6309);
	}

	[Fact]
	public void Leaves_the_studio_id_null_when_no_studio_is_credited()
	{
		// Without an id there is no filmography to link to, and the card says "Unknown studio".
		Convert("""{"id":1,"studios":{"nodes":[]}}""").StudioId.ShouldBeNull();
		Convert("""{"id":1}""").StudioId.ShouldBeNull();
	}

	private static StudioWork Work(string node)
	{
		return new MediaAssembler().ConvertWork(JsonSerializer.Deserialize<StudioMedia>(node, Json)!);
	}

	[Fact]
	public void A_work_nobody_dated_has_no_start_date_and_no_season()
	{
		var work = Work("""{"id":1,"startDate":{"year":null,"month":null,"day":null}}""");

		work.StartDate.ShouldBeNull();
		work.Season.ShouldBeNull();
		Work("""{"id":1}""").StartDate.ShouldBeNull();
	}

	[Fact]
	public void A_month_without_a_year_is_not_a_date()
	{
		Work("""{"id":1,"startDate":{"year":null,"month":7,"day":4}}""").StartDate.ShouldBeNull();
	}

	[Fact]
	public void Prefers_the_season_the_source_names_over_the_start_month()
	{
		// A show that premieres in the last days of December is listed under the winter it runs in.
		var work = Work("""{"id":1,"startDate":{"year":2024,"month":12,"day":28},"season":"WINTER","seasonYear":2025}""");

		work.Season.ShouldBe(new AnimeDate(2025, AnimeSeason.Winter));
	}

	[Theory]
	[InlineData(1, AnimeSeason.Winter)]
	[InlineData(4, AnimeSeason.Spring)]
	[InlineData(9, AnimeSeason.Summer)]
	[InlineData(10, AnimeSeason.Fall)]
	public void Derives_a_missing_season_from_the_start_month(int month, AnimeSeason expected)
	{
		Work($$$"""{"id":1,"startDate":{"year":2020,"month":{{{month}}},"day":null}}""").Season.ShouldBe(new AnimeDate(2020, expected));
	}

	[Fact]
	public void Names_no_season_for_a_month_that_is_not_one()
	{
		Work("""{"id":1,"startDate":{"year":2020,"month":13,"day":null}}""").Season.ShouldBeNull();
	}

	[Fact]
	public void Maps_a_work_with_nothing_but_an_id()
	{
		var work = Work("""{"id":1}""");

		work.Title.ShouldBeEmpty();
		work.ImageUrl.ShouldBeEmpty();
		work.Url.ShouldBeEmpty();
		work.Format.ShouldBe(AnimeFormat.Unknown);
		work.Score.ShouldBeNull();
	}
}
