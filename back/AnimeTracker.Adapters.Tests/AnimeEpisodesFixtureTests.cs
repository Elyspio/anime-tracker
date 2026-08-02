using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using HtmlAgilityPack;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>Runs the episode parser over a recorded anime page.</summary>
public class AnimeEpisodesFixtureTests
{
	private static Episode[] Parse()
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(Fixtures.Read("anime-mushoku-tensei-iii.html"));

		return new AnimeEpisodesAssembler().Convert(doc);
	}

	[Fact]
	public void Reads_the_episodes_listed_on_the_page()
	{
		var episodes = Parse();

		Assert.NotEmpty(episodes);
		Assert.All(episodes, episode => Assert.False(string.IsNullOrWhiteSpace(episode.Title)));
		Assert.Equal(episodes.Select(episode => episode.Number).Order(), episodes.Select(episode => episode.Number));
	}

	[Fact]
	public void Reads_french_day_first_dates()
	{
		// 05/07/2026 is 5 July, not 7 May. Parsing it against the ambient culture — the invariant
		// one inside a container — silently moves the whole schedule by two months.
		var first = Parse().First(episode => episode.Number == 1);

		Assert.Equal(new DateOnly(2026, 7, 5), first.ReleaseDate);
	}

	[Fact]
	public void Reads_a_weekly_schedule_across_the_listed_episodes()
	{
		var dates = Parse()
			.Select(episode => episode.ReleaseDate)
			.OfType<DateOnly>()
			.Distinct()
			.Order()
			.ToArray();

		Assert.True(dates.Length >= 3, $"expected several dated episodes, got {dates.Length}");

		var gaps = dates.Zip(dates.Skip(1), (previous, next) => next.DayNumber - previous.DayNumber).ToArray();
		Assert.All(gaps, gap => Assert.InRange(gap, 1, 21));
	}

	[Fact]
	public void Keeps_the_french_title_text_decoded()
	{
		Assert.All(Parse(), episode =>
		{
			Assert.DoesNotContain("&amp;", episode.Title);
			Assert.DoesNotContain("&#0", episode.Title);
		});
	}
}
