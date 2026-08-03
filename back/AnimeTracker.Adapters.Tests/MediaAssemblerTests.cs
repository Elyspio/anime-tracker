using System.Text.Json;
using AnimeTracker.Abstractions.Models.Base.Anime;
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
	public void Survives_a_node_with_nothing_but_an_id()
	{
		// Announced-but-empty entries appear in every season and must not break the walk.
		var anime = Convert("""{"id":42}""");

		anime.SourceId.ShouldBe(42);
		anime.Title.ShouldBeEmpty();
		anime.Genres.ShouldBeEmpty();
		anime.Episodes.ShouldBeEmpty();
		anime.Popularity.ShouldBe(0);
	}
}
