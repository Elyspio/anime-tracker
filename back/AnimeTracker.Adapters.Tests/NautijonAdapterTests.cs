using System.Text.Json;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Adapters;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using AnimeTracker.Adapters.Nautijon.Configs;
using AnimeTracker.Adapters.Nautijon.FlareSolverr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
	public async Task Asks_the_solver_for_the_page_rather_than_fetching_it_directly()
	{
		// The whole point of the FlareSolverr client: one POST to the solver, carrying the target
		// URL in the body. Fetching nautiljon.com directly is what Cloudflare blocks.
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Solved("<html></html>"));

		await Build(handler).GetAnimes(new AnimeDate(2026, AnimeSeason.Summer));

		var request = Assert.Single(handler.Requests);
		Assert.Equal(HttpMethod.Post, request.Method);
		Assert.Equal("http://solver.invalid/v1", request.Url);

		using var body = JsonDocument.Parse(request.Body);
		Assert.Equal("request.get", body.RootElement.GetProperty("cmd").GetString());
		Assert.Equal(NautijonAdapter.GetSeasonUrl(new AnimeDate(2026, AnimeSeason.Summer)), body.RootElement.GetProperty("url").GetString());
		Assert.Equal(60_000, body.RootElement.GetProperty("maxTimeout").GetInt32());
	}

	[Fact]
	public async Task A_season_page_without_entries_yields_no_anime()
	{
		var handler = new FakeHttpMessageHandler(_ =>
			FakeHttpMessageHandler.Solved("<html><body><div id='content'></div></body></html>"));

		Assert.Empty(await Build(handler).GetAnimes(new AnimeDate(2026, AnimeSeason.Winter)));
	}

	[Fact]
	public async Task A_solver_failure_is_surfaced_with_its_message()
	{
		// The solver answers 200 even when it could not fetch anything, so the envelope's own
		// status is the only signal that the HTML is missing.
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.SolverError("Challenge not solved!"));

		var error = await Assert.ThrowsAsync<HttpRequestException>(
			() => Build(handler).GetAnimes(new AnimeDate(2026, AnimeSeason.Winter)));

		Assert.Contains("Challenge not solved!", error.Message);
	}

	[Fact]
	public async Task A_site_error_behind_a_successful_solve_is_surfaced_too()
	{
		var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Solved("<html>gone</html>", 404));

		var error = await Assert.ThrowsAsync<HttpRequestException>(
			() => Build(handler).GetAnimeEpisodes("https://www.nautiljon.com/animes/x.html"));

		Assert.Equal(System.Net.HttpStatusCode.NotFound, error.StatusCode);
	}

	private static NautijonAdapter Build(FakeHttpMessageHandler handler)
	{
		var options = Options.Create(new NautijonOptions { FlareSolverrUrl = "http://solver.invalid/", SolverTimeoutMs = 60_000 });

		var solver = new FlareSolverrClient(
			new FakeHttpClientFactory(handler, new Uri("http://solver.invalid/")),
			options,
			NullLogger<FlareSolverrClient>.Instance);

		return new NautijonAdapter(solver, new AnimeTileAssembler(), new AnimeEpisodesAssembler(), NullLogger<NautijonAdapter>.Instance);
	}
}
