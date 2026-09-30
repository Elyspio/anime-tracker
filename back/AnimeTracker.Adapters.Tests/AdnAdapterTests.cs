using System.Net;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Adapters.Adn.Adapters;
using AnimeTracker.Adapters.Adn.Configs;
using AnimeTracker.Adapters.Adn.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Drives the ADN adapter against replies recorded under <c>Fixtures/adn</c> by <c>record.sh</c>.
///     Nothing here reaches the network.
/// </summary>
public class AdnAdapterTests
{
	private readonly List<HttpRequestMessage> _requests = [];

	private FakeHttpMessageHandler Recorded(Func<HttpRequestMessage, HttpResponseMessage?>? overrides = null)
	{
		return new FakeHttpMessageHandler(request =>
		{
			_requests.Add(request);

			if (overrides?.Invoke(request) is { } response) return response;

			return request.RequestUri!.AbsolutePath switch
			{
				"/show/catalog" => FakeHttpMessageHandler.Json(Fixtures.Read("adn/search-hero-without-a-class.json")),
				"/show/1331" => FakeHttpMessageHandler.Json(Fixtures.Read("adn/show-1331.json")),
				"/show/1350" => FakeHttpMessageHandler.Json("""{"show":{"id":1350,"title":"HELL MODE","url":"https://animationdigitalnetwork.com/video/1350-hell-mode"}}"""),
				"/video/show/1331" => FakeHttpMessageHandler.Json(Fixtures.Read("adn/videos-1331.json")),
				"/video/show/1350" => FakeHttpMessageHandler.Json(Fixtures.Read("adn/videos-1350.json")),
				"/show/999999" => FakeHttpMessageHandler.Json(Fixtures.Read("adn/show-999999.json"), HttpStatusCode.NotFound),
				var path => throw new InvalidOperationException($"No recording for {path}")
			};
		});
	}

	private static AdnAdapter Build(HttpMessageHandler handler)
	{
		var options = Options.Create(new AdnOptions { RequestDelayMs = 0 });
		var client = new AdnClient(new FakeHttpClientFactory(handler), options, TimeProvider.System, NullLogger<AdnClient>.Instance);

		return new AdnAdapter(client, NullLogger<AdnAdapter>.Instance);
	}

	[Fact]
	public async Task Asks_for_the_french_catalogue()
	{
		await Build(Recorded()).Search("hero without a class", TestContext.Current.CancellationToken);

		_requests.Single().Headers.GetValues("X-Target-Distribution").ShouldBe(["fr"]);
	}

	[Fact]
	public async Task Lists_search_hits_with_their_romaji_title()
	{
		var hits = await Build(Recorded()).Search("hero without a class", TestContext.Current.CancellationToken);

		var hit = hits.First();
		hit.Id.ShouldBe("1331");
		hit.Title.ShouldBe("Hero Without a Class: Who Even Needs Skills?!");
		// What AniList calls it: the title a match is made on.
		hit.AlternativeTitles.ShouldContain("Mushoku no Eiyuu - Betsu ni Skill Nanka Iranakattan da ga");
	}

	[Fact]
	public async Task Reads_a_show_with_its_episodes_and_their_french_audio()
	{
		var series = await Build(Recorded()).GetSeries("1331", TestContext.Current.CancellationToken);

		series.ShouldNotBeNull();
		series.Url.ShouldBe("https://animationdigitalnetwork.com/video/1331-hero-without-a-class-who-even-needs-skills");
		var season = series.Seasons.Single();
		season.Episodes!.Count.ShouldBe(12);
		season.Episodes.ShouldAllBe(episode => episode.French);
		season.Episodes[0].AirDate.ShouldBe(new DateOnly(2025, 9, 24));
	}

	[Fact]
	public async Task Splits_a_show_into_its_seasons()
	{
		// HELL MODE carries both seasons, the second numbered on from 13.
		var series = await Build(Recorded()).GetSeries("1350", TestContext.Current.CancellationToken);

		series!.Seasons.Select(season => season.Order).ShouldBe([1, 2]);
		series.Seasons[1].Episodes![0].Number.ShouldBe(13);
		series.Seasons.SelectMany(season => season.Episodes!).ShouldAllBe(episode => !episode.French);
	}

	[Fact]
	public async Task Pages_through_a_long_show()
	{
		var calls = 0;
		var adapter = Build(Recorded(request =>
		{
			if (request.RequestUri!.AbsolutePath != "/video/show/1331") return null;

			// The gateway caps a page at 100: a full page means there may be more.
			var full = string.Join(',', Enumerable.Range(1, 100).Select(number => $$"""{"shortNumber":"{{number}}","season":"1","languages":["vf"],"available":true}"""));
			return FakeHttpMessageHandler.Json(calls++ == 0 ? $$"""{"videos":[{{full}}]}""" : """{"videos":[{"shortNumber":"101","season":"1","languages":[],"available":true}]}""");
		}));

		var series = await adapter.GetSeries("1331", TestContext.Current.CancellationToken);

		series!.Seasons.Single().Episodes!.Count.ShouldBe(101);
		_requests.Where(request => request.RequestUri!.AbsolutePath == "/video/show/1331").Select(request => request.RequestUri!.Query)
			.ShouldBe(["?offset=0&limit=100&order=asc", "?offset=100&limit=100&order=asc"]);
	}

	[Fact]
	public async Task Does_not_count_a_dub_that_cannot_be_watched_yet()
	{
		var adapter = Build(Recorded(request => request.RequestUri!.AbsolutePath == "/video/show/1331"
			? FakeHttpMessageHandler.Json("""{"videos":[{"shortNumber":"1","season":"1","languages":["vostf","vf"],"available":false},{"shortNumber":"OAV","season":"1","languages":["vf"],"available":true}]}""")
			: null));

		var episodes = (await adapter.GetSeries("1331", TestContext.Current.CancellationToken))!.Seasons.Single().Episodes!;

		episodes[0].French.ShouldBeFalse();
		// A special numbered "OAV" is no AniList episode, but its audio is still read.
		episodes[1].Number.ShouldBeNull();
		episodes[1].French.ShouldBeTrue();
	}

	[Fact]
	public async Task Returns_no_series_for_a_show_adn_does_not_have()
	{
		(await Build(Recorded()).GetSeries("999999", TestContext.Current.CancellationToken)).ShouldBeNull();
	}

	[Theory]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.TooManyRequests)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task Reports_a_refusal_or_an_outage_as_the_platform_being_unavailable(HttpStatusCode status)
	{
		var adapter = Build(Recorded(_ => FakeHttpMessageHandler.Json("{}", status)));

		await Should.ThrowAsync<DubPlatformUnavailableException>(() => adapter.Search("frieren", TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Reports_an_unreachable_proxy_as_the_platform_being_unavailable()
	{
		var adapter = Build(new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused")));

		await Should.ThrowAsync<DubPlatformUnavailableException>(() => adapter.GetSeries("1331", TestContext.Current.CancellationToken));
	}

	[Theory]
	[InlineData("https://animationdigitalnetwork.com/video/1331-hero-without-a-class-who-even-needs-skills", "1331")]
	[InlineData("https://animationdigitalnetwork.com/video/1331-hero-without-a-class-who-even-needs-skills/29932-episode-1", "1331")]
	[InlineData("https://animationdigitalnetwork.fr/video/1350", "1350")]
	public void Reads_the_show_id_off_a_show_page(string url, string expected)
	{
		Build(Recorded()).ParseSeriesId(url).ShouldBe(expected);
	}

	[Theory]
	[InlineData("https://animationdigitalnetwork.com/")]
	[InlineData("https://animationdigitalnetwork.com/catalog")]
	[InlineData("https://animationdigitalnetwork.com.evil.test/video/1331")]
	[InlineData("https://www.crunchyroll.com/series/G24H1N3MP")]
	public void Finds_no_show_id_elsewhere(string url)
	{
		Build(Recorded()).ParseSeriesId(url).ShouldBeNull();
	}
}
