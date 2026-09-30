using System.Net;
using System.Text;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Adapters.Crunchyroll.Adapters;
using AnimeTracker.Adapters.Crunchyroll.Configs;
using AnimeTracker.Adapters.Crunchyroll.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Drives the Crunchyroll adapter against replies recorded under <c>Fixtures/crunchyroll</c> by
///     <c>record.sh</c>. Nothing here reaches the network.
/// </summary>
public class CrunchyrollAdapterTests
{
	private const string Series = "G24H1N3MP";

	private const string Season = "GS00374452JAJP";

	private readonly List<HttpRequestMessage> _requests = [];

	/// <summary>Answers each of the adapter's paths with its recording; <paramref name="overrides" /> go first.</summary>
	private FakeHttpMessageHandler Recorded(Func<HttpRequestMessage, HttpResponseMessage?>? overrides = null)
	{
		return new FakeHttpMessageHandler(request =>
		{
			_requests.Add(request);

			if (overrides?.Invoke(request) is { } response) return response;

			return request.RequestUri!.AbsolutePath switch
			{
				"/" => Html(Fixtures.Read("crunchyroll/home.html")),
				"/auth/v1/token" => FakeHttpMessageHandler.Json(Fixtures.Read("crunchyroll/token.json")),
				"/content/v2/discover/search" => FakeHttpMessageHandler.Json(Fixtures.Read("crunchyroll/search-mushoku-tensei.json")),
				$"/content/v2/cms/series/{Series}" => FakeHttpMessageHandler.Json(Fixtures.Read($"crunchyroll/series-{Series}.json")),
				$"/content/v2/cms/series/{Series}/seasons" => FakeHttpMessageHandler.Json(Fixtures.Read($"crunchyroll/seasons-{Series}.json")),
				$"/content/v2/cms/seasons/{Season}/episodes" => FakeHttpMessageHandler.Json(Fixtures.Read($"crunchyroll/episodes-{Season}.json")),
				"/content/v2/cms/series/GZZZZZZZZ" => FakeHttpMessageHandler.Json(Fixtures.Read("crunchyroll/series-GZZZZZZZZ.json"), HttpStatusCode.NotFound),
				var path => throw new InvalidOperationException($"No recording for {path}")
			};
		});
	}

	private static HttpResponseMessage Html(string page)
	{
		return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page, Encoding.UTF8, "text/html") };
	}

	private static HttpResponseMessage Challenge()
	{
		var response = Html("<!DOCTYPE html><html><head><title>Just a moment...</title></head></html>");
		response.StatusCode = HttpStatusCode.Forbidden;
		response.Headers.Add("cf-mitigated", "challenge");

		return response;
	}

	private static CrunchyrollAdapter Build(HttpMessageHandler handler)
	{
		var options = Options.Create(new CrunchyrollOptions { RequestDelayMs = 0 });
		var client = new CrunchyrollClient(new FakeHttpClientFactory(handler), options, TimeProvider.System, NullLogger<CrunchyrollClient>.Instance);

		return new CrunchyrollAdapter(client, options, NullLogger<CrunchyrollAdapter>.Instance);
	}

	[Fact]
	public async Task Asks_for_an_anonymous_token_with_the_client_id_the_home_page_publishes()
	{
		await Build(Recorded()).Search("mushoku tensei", TestContext.Current.CancellationToken);

		var token = _requests.Single(request => request.RequestUri!.AbsolutePath == "/auth/v1/token");
		token.Method.ShouldBe(HttpMethod.Post);
		token.Headers.Authorization!.Scheme.ShouldBe("Basic");
		// The id read out of the recorded page, followed by an empty secret.
		Encoding.UTF8.GetString(Convert.FromBase64String(token.Headers.Authorization.Parameter!)).ShouldBe("kmj7imhjt_q90lcbzzsj:");

		var search = _requests.Single(request => request.RequestUri!.AbsolutePath == "/content/v2/discover/search");
		search.Headers.Authorization!.ToString().ShouldBe("Bearer recorded-token");
	}

	[Fact]
	public async Task Speaks_http_1_1_as_a_browser_on_every_request()
	{
		// What Cloudflare was measured to let through; HTTP/2 and the .NET user agent are challenged.
		await Build(Recorded()).GetSeries(Series, TestContext.Current.CancellationToken);

		_requests.ShouldAllBe(request => request.Version == HttpVersion.Version11 && request.VersionPolicy == HttpVersionPolicy.RequestVersionExact);
		_requests.ShouldAllBe(request => request.Headers.UserAgent.ToString().Contains("Chrome/"));
	}

	[Fact]
	public async Task Sends_everything_to_the_gateway_when_there_is_one_but_links_to_the_public_site()
	{
		var options = Options.Create(new CrunchyrollOptions { RequestDelayMs = 0, Gateway = "http://10.0.1.123:8889" });
		var client = new CrunchyrollClient(new FakeHttpClientFactory(Recorded()), options, TimeProvider.System, NullLogger<CrunchyrollClient>.Instance);
		var adapter = new CrunchyrollAdapter(client, options, NullLogger<CrunchyrollAdapter>.Instance);

		var series = await adapter.GetSeries(Series, TestContext.Current.CancellationToken);

		_requests.ShouldAllBe(request => request.RequestUri!.Scheme == "http" && request.RequestUri.Authority == "10.0.1.123:8889");
		// The reader is sent to Crunchyroll itself, never to the gateway.
		series!.Url.ShouldStartWith("https://www.crunchyroll.com/series/");
	}

	[Fact]
	public async Task Keeps_its_token_until_it_expires()
	{
		var adapter = Build(Recorded());

		await adapter.Search("mushoku tensei", TestContext.Current.CancellationToken);
		await adapter.GetSeries(Series, TestContext.Current.CancellationToken);

		_requests.Count(request => request.RequestUri!.AbsolutePath == "/auth/v1/token").ShouldBe(1);
		_requests.Count(request => request.RequestUri!.AbsolutePath == "/").ShouldBe(1);
	}

	[Fact]
	public async Task Asks_for_a_fresh_token_once_when_the_api_turns_one_down()
	{
		var refused = false;
		var adapter = Build(Recorded(request =>
		{
			if (request.RequestUri!.AbsolutePath != "/content/v2/discover/search" || refused) return null;

			refused = true;
			return FakeHttpMessageHandler.Json("""{"code":"auth.invalid_token"}""", HttpStatusCode.Unauthorized);
		}));

		var hits = await adapter.Search("mushoku tensei", TestContext.Current.CancellationToken);

		hits.ShouldNotBeEmpty();
		_requests.Count(request => request.RequestUri!.AbsolutePath == "/auth/v1/token").ShouldBe(2);
	}

	[Fact]
	public async Task Lists_the_series_the_search_returns()
	{
		var hits = await Build(Recorded()).Search("mushoku tensei", TestContext.Current.CancellationToken);

		hits.Count.ShouldBe(6);
		hits[0].Id.ShouldBe(Series);
		hits[0].Title.ShouldBe("Mushoku Tensei: Jobless Reincarnation");

		var search = _requests.Single(request => request.RequestUri!.AbsolutePath == "/content/v2/discover/search");
		search.RequestUri!.Query.ShouldContain("q=mushoku%20tensei");
		search.RequestUri.Query.ShouldContain("type=series");
	}

	[Fact]
	public async Task Reads_a_series_with_its_seasons_in_order()
	{
		var series = await Build(Recorded()).GetSeries(Series, TestContext.Current.CancellationToken);

		series.ShouldNotBeNull();
		series.Title.ShouldBe("Mushoku Tensei: Jobless Reincarnation");
		series.Url.ShouldBe("https://www.crunchyroll.com/series/G24H1N3MP/mushoku-tensei-jobless-reincarnation");
		series.Seasons.Select(season => (season.Order, season.Id)).ShouldBe([(1, "G609CX3J4"), (2, "G6NQCJ9P1"), (3, Season)]);
		// Crunchyroll lists episodes separately: the caller asks for the seasons it needs.
		series.Seasons.ShouldAllBe(season => season.Episodes == null);
	}

	[Fact]
	public async Task Reads_the_french_audio_off_each_episode()
	{
		var episodes = await Build(Recorded()).GetEpisodes(Series, Season, TestContext.Current.CancellationToken);

		episodes.Count.ShouldBe(14);
		episodes.Select(episode => episode.Number).ShouldBe(Enumerable.Range(1, 14).Select(number => (double?)number));
		// Recorded 29 September 2026: the French dub had reached episode 11.
		episodes.Where(episode => episode.French).Select(episode => episode.Number).ShouldBe(Enumerable.Range(1, 11).Select(number => (double?)number));
		episodes[0].AirDate.ShouldBe(new DateOnly(2026, 7, 4));
		episodes[0].ReleaseDate.ShouldBe(new DateOnly(2026, 7, 4));
	}

	[Fact]
	public async Task Returns_no_series_for_an_id_crunchyroll_does_not_know()
	{
		(await Build(Recorded()).GetSeries("GZZZZZZZZ", TestContext.Current.CancellationToken)).ShouldBeNull();
	}

	[Fact]
	public async Task Reports_a_challenge_as_the_platform_being_unavailable()
	{
		var adapter = Build(Recorded(request => request.RequestUri!.AbsolutePath == "/" ? Challenge() : null));

		var error = await Should.ThrowAsync<DubPlatformUnavailableException>(() => adapter.Search("mushoku tensei", TestContext.Current.CancellationToken));

		error.Message.ShouldContain("challenge");
	}

	[Theory]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.TooManyRequests)]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task Reports_a_refusal_or_an_outage_as_the_platform_being_unavailable(HttpStatusCode status)
	{
		// Never as an empty catalogue: that would erase every match the sync had.
		var adapter = Build(Recorded(request => request.RequestUri!.AbsolutePath == $"/content/v2/cms/series/{Series}"
			? FakeHttpMessageHandler.Json("{}", status)
			: null));

		await Should.ThrowAsync<DubPlatformUnavailableException>(() => adapter.GetSeries(Series, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Reports_an_unreachable_proxy_as_the_platform_being_unavailable()
	{
		var adapter = Build(new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused (10.0.1.123:8888)")));

		var error = await Should.ThrowAsync<DubPlatformUnavailableException>(() => adapter.Search("frieren", TestContext.Current.CancellationToken));

		error.Message.ShouldContain("Connection refused");
	}

	[Fact]
	public async Task Reports_a_home_page_without_a_client_id_as_the_platform_being_unavailable()
	{
		var adapter = Build(Recorded(request => request.RequestUri!.AbsolutePath == "/" ? Html("<html></html>") : null));

		await Should.ThrowAsync<DubPlatformUnavailableException>(() => adapter.Search("frieren", TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Keeps_only_the_seasons_in_their_original_audio()
	{
		// Older series list each dub as a season of its own, aired the same days as the original.
		var adapter = Build(Recorded(request => request.RequestUri!.AbsolutePath == $"/content/v2/cms/series/{Series}/seasons"
			? FakeHttpMessageHandler.Json("""
				{"total":2,"data":[
				  {"id":"GJA","season_sequence_number":1,"versions":[{"audio_locale":"ja-JP","guid":"GJA","original":true},{"audio_locale":"fr-FR","guid":"GFR","original":false}]},
				  {"id":"GFR","season_sequence_number":1,"versions":[{"audio_locale":"ja-JP","guid":"GJA","original":true},{"audio_locale":"fr-FR","guid":"GFR","original":false}]}]}
				""")
			: null));

		var series = await adapter.GetSeries(Series, TestContext.Current.CancellationToken);

		series!.Seasons.Select(season => season.Id).ShouldBe(["GJA"]);
	}

	[Fact]
	public async Task Survives_episodes_with_missing_fields()
	{
		// A recap slotted as 6.5, a special with no number, a date left blank, an episode without versions.
		var adapter = Build(Recorded(request => request.RequestUri!.AbsolutePath == $"/content/v2/cms/seasons/{Season}/episodes"
			? FakeHttpMessageHandler.Json("""
				{"total":4,"data":[
				  {"sequence_number":6.5,"episode_air_date":"2026-08-08T00:00:00Z","versions":[{"audio_locale":"fr-FR"}]},
				  {"sequence_number":null,"episode_air_date":"","versions":[]},
				  {"sequence_number":2,"episode_air_date":null,"audio_locale":"fr-FR"},
				  {}]}
				""")
			: null));

		var episodes = await adapter.GetEpisodes(Series, Season, TestContext.Current.CancellationToken);

		episodes.Select(episode => episode.Number).ShouldBe([6.5, null, 2, null]);
		episodes.Select(episode => episode.AirDate).ShouldBe([new DateOnly(2026, 8, 8), null, null, null]);
		episodes.Select(episode => episode.French).ShouldBe([true, false, true, false]);
	}

	[Theory]
	[InlineData("https://www.crunchyroll.com/series/GT00377907/black-torch", "GT00377907")]
	[InlineData("https://www.crunchyroll.com/series/G24H1N3MP", "G24H1N3MP")]
	[InlineData("https://www.crunchyroll.com/fr/series/G24H1N3MP/mushoku-tensei", "G24H1N3MP")]
	[InlineData("https://www.crunchyroll.com/pt-pt/series/GG5H5XQMD/as-a-reincarnated-aristocrat", "GG5H5XQMD")]
	[InlineData("https://crunchyroll.com/series/gt00384001/fx-fighter-kurumi-chan", "GT00384001")]
	[InlineData("http://www.crunchyroll.com/series/G3KHEVDJ7/", "G3KHEVDJ7")]
	public void Reads_the_series_id_off_a_series_page(string url, string expected)
	{
		Build(Recorded()).ParseSeriesId(url).ShouldBe(expected);
	}

	[Theory]
	[InlineData("https://www.crunchyroll.com/")]
	[InlineData("https://www.crunchyroll.com/the-detective-is-already-dead")]
	[InlineData("https://www.crunchyroll.com/watch/GE00374453JAJP/episode-1")]
	[InlineData("https://crunchyroll.com.evil.test/series/G24H1N3MP")]
	[InlineData("https://evil.test/www.crunchyroll.com/series/G24H1N3MP")]
	[InlineData("javascript:alert(1)")]
	[InlineData("not a url")]
	public void Finds_no_series_id_elsewhere(string url)
	{
		Build(Recorded()).ParseSeriesId(url).ShouldBeNull();
	}

	[Theory]
	[InlineData("https://www.crunchyroll.com/aoashi", true)]
	[InlineData("https://crunchyroll.com", true)]
	[InlineData("https://static.crunchyroll.com/x", true)]
	[InlineData("https://www.netflix.com/title/1", false)]
	[InlineData("https://notcrunchyroll.com/", false)]
	public void Knows_its_own_domain(string url, bool expected)
	{
		Build(Recorded()).Owns(url).ShouldBe(expected);
	}
}
