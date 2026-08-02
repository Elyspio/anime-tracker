using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Adapters;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

public class NautijonAdapterTests
{
	[Theory]
	[InlineData(AnimeSeason.Winter, "hiver")]
	[InlineData(AnimeSeason.Spring, "printemps")]
	[InlineData(AnimeSeason.Summer, "été")]
	[InlineData(AnimeSeason.Fall, "automne")]
	public void Builds_the_season_url_from_the_french_season_name(AnimeSeason season, string slug)
	{
		var url = NautijonAdapter.GetSeasonUrl(new AnimeDate(2026, season));

		Assert.StartsWith($"https://www.nautiljon.com/animes/{slug}-2026.html", url);
	}

	[Fact]
	public async Task Requests_the_season_page_verbatim()
	{
		// The summer slug carries an accent. It leaves as UTF-8 percent-encoded once and only once —
		// a second pass through the solver's own escaping would produce %25C3%25A9 and a 404.
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Html("<html></html>"));
		var adapter = Build(handler);

		await adapter.GetAnimes(new AnimeDate(2026, AnimeSeason.Summer));

		var requested = Assert.Single(handler.Requests);
		Assert.Equal(
			"https://www.nautiljon.com/animes/%C3%A9t%C3%A9-2026.html?format=1&y=0&tri=p&public_averti=1&simulcast=",
			requested.Url);
	}

	[Fact]
	public async Task A_season_page_without_entries_yields_no_anime()
	{
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Html("<html><body><div id='content'></div></body></html>"));

		var animes = await Build(handler).GetAnimes(new AnimeDate(2026, AnimeSeason.Winter));

		Assert.Empty(animes);
	}

	private static NautijonAdapter Build(FakeHttpMessageHandler handler)
	{
		return new NautijonAdapter(
			new FakeHttpClientFactory(handler),
			new AnimeTileAssembler(),
			new AnimeEpisodesAssembler(),
			NullLogger<NautijonAdapter>.Instance);
	}
}
