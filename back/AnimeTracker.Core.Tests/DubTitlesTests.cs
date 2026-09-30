using AnimeTracker.Core.Services.Dub;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>
///     Title matching is the lenient half of a match; the date is the strict one. These pin how lenient,
///     with titles taken from the summer and autumn 2026 seasons.
/// </summary>
public class DubTitlesTests
{
	[Theory]
	[InlineData("Black Clover 2nd Season", "black clover")]
	[InlineData("Ao no Hako Season 2", "ao no hako")]
	[InlineData("Chitose-kun wa Ramune Bin no Naka Part 2", "chitose kun wa ramune bin no naka")]
	[InlineData("Clevatess II", "clevatess")]
	[InlineData("Otomege Sekai wa Mob ni Kibishii Sekai desu 2", "otomege sekai wa mob ni kibishii sekai desu")]
	[InlineData("Blade Runner Black Out 2022 (VOSTA)", "blade runner black out")]
	[InlineData("Magic Knight Rayearth (2026)", "magic knight rayearth")]
	[InlineData("Kenshin le vagabond (2023)", "kenshin le vagabond")]
	[InlineData("Attack on Titan: The Final Season", "attack on titan")]
	[InlineData("Les Carnets de l'apothicaire", "les carnets de l apothicaire")]
	[InlineData("Ｆｒｉｅｒｅｎ", "frieren")]
	public void Normalizes_a_title_down_to_the_show(string title, string expected)
	{
		DubTitles.Normalize(title).ShouldBe(expected);
	}

	[Fact]
	public void Keeps_a_number_that_is_the_whole_title()
	{
		// "86" and "100" are shows, not seasons: only a trailing number after other words is dropped.
		DubTitles.Normalize("86").ShouldBe("86");
		DubTitles.Normalize("Mob Psycho 100").ShouldBe("mob psycho");
	}

	[Fact]
	public void Keeps_japanese_voicing_marks()
	{
		// Only Latin accents are folded: が must not become か, or two native titles would collide.
		DubTitles.Normalize("ダンダダン").ShouldBe("ダンダダン");
	}

	[Fact]
	public void Matches_a_season_to_the_franchise_the_platform_lists()
	{
		DubTitles.Matches(["Black Clover 2nd Season"], ["Black Clover"]).ShouldBeTrue();
	}

	[Fact]
	public void Matches_on_the_part_before_the_subtitle()
	{
		// AniList's romaji and Crunchyroll's English only share their head.
		DubTitles.Matches(["Mushoku Tensei III: Isekai Ittara Honki Dasu"], ["Mushoku Tensei: Jobless Reincarnation"]).ShouldBeTrue();
	}

	[Fact]
	public void Matches_on_any_of_the_anime_titles()
	{
		// ADN lists the romaji as its original title; the English one is only on AniList's side.
		DubTitles.Matches(
			["Tensei Shitara Ken Deshita 2nd Season", "Reincarnated as a Sword Season 2"],
			["Reincarnated as a Sword"]).ShouldBeTrue();
	}

	[Fact]
	public void Does_not_match_on_a_head_too_short_to_mean_anything()
	{
		// "Re" out of "Re:Zero" would match every title that happens to start with "Re:".
		DubTitles.Matches(["Re:Zero kara Hajimeru Isekai Seikatsu"], ["Re:Monster"]).ShouldBeFalse();
	}

	[Fact]
	public void Does_not_match_different_shows()
	{
		DubTitles.Matches(["Mushoku Tensei III: Isekai Ittara Honki Dasu"], ["Mushibugyo (VOSTA)"]).ShouldBeFalse();
	}

	[Fact]
	public void Searches_under_the_english_title_first()
	{
		var queries = DubTitles.Queries("Tensei Shitara Ken Deshita 2nd Season", ["Reincarnated as a Sword Season 2", "転生したら剣でした 第2期"]);

		queries.ShouldBe(["reincarnated as a sword", "tensei shitara ken deshita"]);
	}

	[Fact]
	public void Searches_under_the_head_of_a_long_title_too()
	{
		var queries = DubTitles.Queries("Mushoku Tensei III: Isekai Ittara Honki Dasu", ["Mushoku Tensei: Jobless Reincarnation Season 3"]);

		queries.ShouldBe(["mushoku tensei jobless reincarnation", "mushoku tensei iii isekai ittara honki dasu", "mushoku tensei"]);
	}

	[Fact]
	public void Searches_under_the_romaji_alone_when_there_is_no_english_title()
	{
		DubTitles.Queries("Hotaru no Yomeiri", ["蛍の嫁入り"]).ShouldBe(["hotaru no yomeiri"]);
	}
}
