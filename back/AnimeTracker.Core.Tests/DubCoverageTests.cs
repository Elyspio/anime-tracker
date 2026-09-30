using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Core.Services.Dub;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>Up to date and complete, computed on read. Sets of episodes, never counts.</summary>
public class DubCoverageTests
{
	private static readonly DateOnly Today = new(2026, 7, 26);

	private static readonly DateTimeOffset Checked = new(2026, 7, 26, 3, 0, 0, TimeSpan.Zero);

	/// <summary>Twelve weekly episodes from 4 July: four have aired by <see cref="Today" />.</summary>
	private static readonly Episode[] Schedule = Enumerable.Range(1, 12).Select(number => new Episode(number, new DateOnly(2026, 7, 4).AddDays(7 * (number - 1)))).ToArray();

	private static DubMatchBase Match(DubPlatform platform, IReadOnlyCollection<int> french, DubMatchStatus status = DubMatchStatus.Matched)
	{
		return new DubMatchBase
		{
			SourceId = 1,
			Date = new AnimeDate(2026, AnimeSeason.Summer),
			Platform = platform,
			Status = status,
			Method = DubMatchMethod.Link,
			SeriesId = "S",
			SeriesTitle = "Series",
			Url = status == DubMatchStatus.NotFound ? null : $"https://{platform}.example/series/S",
			AvailableEpisodes = Enumerable.Range(1, 12).ToArray(),
			FrenchEpisodes = french,
			CheckedAt = Checked
		};
	}

	[Fact]
	public void Is_up_to_date_when_every_aired_episode_is_dubbed()
	{
		var dub = DubCoverage.Evaluate(Schedule, 12, [Match(DubPlatform.Crunchyroll, [1, 2, 3, 4])], Today).Single();

		dub.UpToDate.ShouldBeTrue();
		dub.Complete.ShouldBeFalse();
		dub.FrenchEpisodes.ShouldBe(4);
		dub.TotalEpisodes.ShouldBe(12);
		dub.CheckedAt.ShouldBe(Checked);
	}

	[Fact]
	public void Is_not_up_to_date_with_a_hole_even_when_the_count_matches()
	{
		// Four dubbed, four aired — but the third is missing: this is the case counting gets wrong.
		var dub = DubCoverage.Evaluate(Schedule, 12, [Match(DubPlatform.Crunchyroll, [1, 2, 4, 5])], Today).Single();

		dub.UpToDate.ShouldBeFalse();
	}

	[Fact]
	public void Is_not_up_to_date_before_anything_has_aired()
	{
		// "Every aired episode is dubbed" is vacuously true of nothing aired; the filter would then
		// list every show that has not started.
		var dub = DubCoverage.Evaluate(Schedule, 12, [Match(DubPlatform.Crunchyroll, [])], new DateOnly(2026, 7, 1)).Single();

		dub.UpToDate.ShouldBeFalse();
	}

	[Fact]
	public void Is_complete_when_every_announced_episode_is_dubbed()
	{
		var dub = DubCoverage.Evaluate(Schedule, 12, [Match(DubPlatform.Adn, Enumerable.Range(1, 12).ToArray())], new DateOnly(2026, 10, 1)).Single();

		dub.Complete.ShouldBeTrue();
		dub.UpToDate.ShouldBeTrue();
	}

	[Fact]
	public void Can_be_complete_with_a_known_total_and_no_published_slot()
	{
		// The prediction says UnknownEnd for this one; the dub only needs the total.
		var dub = DubCoverage.Evaluate([], 3, [Match(DubPlatform.Adn, [1, 2, 3])], Today).Single();

		dub.Complete.ShouldBeTrue();
	}

	[Fact]
	public void Is_never_complete_without_an_announced_total()
	{
		var dub = DubCoverage.Evaluate(Schedule, null, [Match(DubPlatform.Adn, Enumerable.Range(1, 12).ToArray())], Today).Single();

		dub.Complete.ShouldBeFalse();
		dub.FrenchEpisodes.ShouldBe(12);
		dub.TotalEpisodes.ShouldBeNull();
	}

	[Fact]
	public void Counts_only_the_announced_episodes_toward_the_total()
	{
		// Thirteen on the platform against twelve announced: the extra one is no proof of anything.
		var dub = DubCoverage.Evaluate(Schedule, 12, [Match(DubPlatform.Crunchyroll, Enumerable.Range(1, 13).ToArray())], Today).Single();

		dub.FrenchEpisodes.ShouldBe(12);
		dub.Complete.ShouldBeTrue();
	}

	[Fact]
	public void Says_nothing_about_a_platform_the_anime_was_not_matched_on()
	{
		// Unknown is not absent: no availability at all, rather than one with no French episode.
		DubCoverage.Evaluate(Schedule, 12, [
			Match(DubPlatform.Crunchyroll, [], DubMatchStatus.NotFound),
			Match(DubPlatform.Adn, [], DubMatchStatus.Unaligned)
		], Today).ShouldBeEmpty();
	}

	[Fact]
	public void Returns_a_matched_platform_with_no_french_episode_for_its_page()
	{
		var dub = DubCoverage.Evaluate(Schedule, 12, [Match(DubPlatform.Crunchyroll, [])], Today).Single();

		dub.FrenchEpisodes.ShouldBe(0);
		dub.UpToDate.ShouldBeFalse();
	}

	[Fact]
	public void Lists_the_best_platform_first()
	{
		var dubs = DubCoverage.Evaluate(Schedule, 12, [
			Match(DubPlatform.Crunchyroll, [1, 2]),
			Match(DubPlatform.Adn, [1, 2, 3, 4])
		], Today);

		dubs.Select(dub => dub.Platform).ShouldBe([DubPlatform.Adn, DubPlatform.Crunchyroll]);
	}
}
