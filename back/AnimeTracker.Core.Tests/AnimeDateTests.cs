using AnimeTracker.Abstractions.Models.Base.Anime;
using Xunit;

namespace AnimeTracker.Core.Tests;

public class AnimeDateTests
{
	[Theory]
	[InlineData(1, AnimeSeason.Winter)]
	[InlineData(3, AnimeSeason.Winter)]
	[InlineData(4, AnimeSeason.Spring)]
	[InlineData(6, AnimeSeason.Spring)]
	[InlineData(7, AnimeSeason.Summer)]
	[InlineData(9, AnimeSeason.Summer)]
	[InlineData(10, AnimeSeason.Fall)]
	[InlineData(12, AnimeSeason.Fall)]
	public void Maps_a_month_to_its_broadcast_season(int month, AnimeSeason expected)
	{
		Assert.Equal(expected, AnimeDate.Current(new DateOnly(2026, month, 15)).Season);
	}

	[Fact]
	public void December_belongs_to_the_autumn_of_its_own_year()
	{
		// The tempting off-by-one is to roll December into the next winter season.
		var date = AnimeDate.Current(new DateOnly(2026, 12, 31));

		Assert.Equal(new AnimeDate(2026, AnimeSeason.Fall), date);
	}

	[Fact]
	public void January_opens_the_winter_of_the_new_year()
	{
		Assert.Equal(new AnimeDate(2027, AnimeSeason.Winter), AnimeDate.Current(new DateOnly(2027, 1, 1)));
	}
}
