using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Core.Services.Dub;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>
///     Which platform season an anime is, and which episode is which. The seasons below reproduce the
///     shapes met on Crunchyroll and ADN in the summer 2026 season.
/// </summary>
public class DubAlignerTests
{
	private static readonly Episode Anchor = new(1, new DateOnly(2026, 7, 4));

	/// <summary>A weekly season starting on <paramref name="start" />, numbered from <paramref name="first" />.</summary>
	private static LoadedSeason Weekly(string id, DateOnly start, int count, int first = 1, Func<int, bool>? french = null)
	{
		var episodes = Enumerable.Range(0, count)
			.Select(index => new PlatformEpisode(first + index, start.AddDays(7 * index), french?.Invoke(first + index) ?? false))
			.ToArray();

		return new LoadedSeason(new PlatformSeason(id, 1, episodes), episodes);
	}

	[Fact]
	public void Lines_up_the_season_that_aired_with_the_first_episode()
	{
		var alignment = DubAligner.Align(Anchor, [
			Weekly("s3", new DateOnly(2026, 7, 4), 14, french: number => number <= 11),
			Weekly("s2", new DateOnly(2024, 4, 8), 12)
		]);

		alignment.ShouldNotBeNull();
		alignment.SeasonId.ShouldBe("s3");
		alignment.Available.ShouldBe(Enumerable.Range(1, 14));
		alignment.French.ShouldBe(Enumerable.Range(1, 11));
	}

	[Fact]
	public void Renumbers_a_season_that_keeps_counting_from_the_previous_one()
	{
		// ADN numbers HELL MODE's second season 13 to 25; AniList numbers it 1 to 13.
		var alignment = DubAligner.Align(Anchor, [Weekly("s2", new DateOnly(2026, 7, 3), 13, first: 13, french: number => number == 13)]);

		alignment.ShouldNotBeNull();
		alignment.Available.ShouldBe(Enumerable.Range(1, 13));
		alignment.French.ShouldBe([1]);
	}

	[Fact]
	public void Finds_the_second_cour_inside_a_season_that_holds_both()
	{
		// One platform season of 24 episodes for two AniList entries: the second starts at 13.
		var secondCour = new Episode(1, new DateOnly(2026, 7, 4));
		var alignment = DubAligner.Align(secondCour, [Weekly("s1", new DateOnly(2026, 4, 11), 24)]);

		alignment.ShouldNotBeNull();
		alignment.Available.ShouldBe(Enumerable.Range(1, 12));
	}

	[Fact]
	public void Tolerates_a_platform_that_releases_a_day_late()
	{
		DubAligner.Align(Anchor, [Weekly("s1", new DateOnly(2026, 7, 5), 12)]).ShouldNotBeNull();
	}

	[Fact]
	public void Takes_the_lower_number_of_a_double_premiere()
	{
		var start = new DateOnly(2026, 7, 4);
		PlatformEpisode[] episodes = [new(1, start, false), new(2, start, false), new(3, start.AddDays(7), false)];

		var alignment = DubAligner.Align(Anchor, [new LoadedSeason(new PlatformSeason("s1", 1, episodes), episodes)]);

		alignment.ShouldNotBeNull();
		alignment.Available.ShouldBe([1, 2, 3]);
	}

	[Fact]
	public void Leaves_unknown_an_anime_no_season_aired_with()
	{
		// The series exists, the season for this anime is not up yet.
		DubAligner.Align(Anchor, [Weekly("s1", new DateOnly(2024, 4, 6), 12)]).ShouldBeNull();
	}

	[Fact]
	public void Leaves_unknown_an_anime_two_seasons_aired_with()
	{
		// A dub listed as a season of its own, on the same days as the original: either could be it.
		DubAligner.Align(Anchor, [
			Weekly("original", new DateOnly(2026, 7, 4), 12),
			Weekly("dub", new DateOnly(2026, 7, 4), 12)
		]).ShouldBeNull();
	}

	[Fact]
	public void Leaves_unknown_a_season_with_two_episodes_on_one_number()
	{
		// A split episode, or a re-upload: which of the two carries the dub cannot be told.
		var start = new DateOnly(2026, 7, 4);
		PlatformEpisode[] episodes = [new(1, start, false), new(2, start.AddDays(7), true), new(2, start.AddDays(7), false)];

		DubAligner.Align(Anchor, [new LoadedSeason(new PlatformSeason("s1", 1, episodes), episodes)]).ShouldBeNull();
	}

	[Fact]
	public void Ignores_recaps_and_specials()
	{
		var start = new DateOnly(2026, 7, 4);
		PlatformEpisode[] episodes =
		[
			new(1, start, true),
			new(6.5, start.AddDays(40), true),
			new(null, start.AddDays(50), true),
			new(2, start.AddDays(7), true)
		];

		var alignment = DubAligner.Align(Anchor, [new LoadedSeason(new PlatformSeason("s1", 1, episodes), episodes)]);

		alignment.ShouldNotBeNull();
		alignment.Available.ShouldBe([1, 2]);
		alignment.French.ShouldBe([1, 2]);
	}

	[Fact]
	public void Anchors_on_the_release_date_when_the_broadcast_date_is_wrong()
	{
		// Crunchyroll dated "The Oblivious Saint" episode 1 a year early; its release date is right.
		PlatformEpisode[] episodes =
		[
			new(1, new DateOnly(2025, 7, 4), false, new DateOnly(2026, 7, 4)),
			new(2, new DateOnly(2026, 7, 11), false, new DateOnly(2026, 7, 11))
		];

		DubAligner.Align(Anchor, [new LoadedSeason(new PlatformSeason("s1", 1, episodes), episodes)]).ShouldNotBeNull();
	}

	[Fact]
	public void Does_not_anchor_on_an_undated_episode()
	{
		PlatformEpisode[] episodes = [new(1, null, true), new(2, null, true)];

		DubAligner.Align(Anchor, [new LoadedSeason(new PlatformSeason("s1", 1, episodes), episodes)]).ShouldBeNull();
	}

	[Fact]
	public void Stops_reading_at_the_season_that_started_by_the_anchor()
	{
		DubAligner.StartsBy(Weekly("s3", new DateOnly(2026, 7, 4), 12).Episodes, Anchor.ReleaseDate).ShouldBeTrue();
		DubAligner.StartsBy(Weekly("s1", new DateOnly(2024, 4, 6), 24).Episodes, Anchor.ReleaseDate).ShouldBeTrue();
	}

	[Fact]
	public void Keeps_reading_past_a_season_that_started_after_the_anchor()
	{
		// A sequel already listed when an older season is synced: its episodes are all later.
		DubAligner.StartsBy(Weekly("s4", new DateOnly(2026, 10, 3), 12).Episodes, Anchor.ReleaseDate).ShouldBeFalse();
		DubAligner.StartsBy([new PlatformEpisode(1, null, false)], Anchor.ReleaseDate).ShouldBeFalse();
	}

	[Fact]
	public void Anchors_on_the_lowest_episode_of_the_schedule()
	{
		DubAligner.Anchor([new Episode(2, new DateOnly(2026, 7, 11)), new Episode(1, new DateOnly(2026, 7, 4))])
			.ShouldBe(new Episode(1, new DateOnly(2026, 7, 4)));
		DubAligner.Anchor([]).ShouldBeNull();
	}
}
