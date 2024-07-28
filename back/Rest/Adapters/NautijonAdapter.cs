using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Adapters.Rest.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using AnimeTracker.Api.Adapters.Rest.Utils.Extensions;

namespace AnimeTracker.Api.Adapters.Rest.Adapters;

public class NautijonAdapter : TracingAdapter, INautijonAdapter
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly AnimeTileAssembler _animeTileAssembler;
	private readonly AnimeEpisodesAssembler _episodesAssembler;

	public const string ClientName = "Nautijon";

	public NautijonAdapter(ILogger<NautijonAdapter> logger, IHttpClientFactory httpClientFactory, AnimeTileAssembler animeTileAssembler, AnimeEpisodesAssembler episodesAssembler) : base(logger)
	{
		_httpClientFactory = httpClientFactory;
		_animeTileAssembler = animeTileAssembler;
		_episodesAssembler = episodesAssembler;
	}


	private string GetAnimesUrl(AnimeDate date)
	{
		var season = date.Season switch
		{
			AnimeSeason.Winter => "hiver",
			AnimeSeason.Spring => "printemps",
			AnimeSeason.Summer => "été",
			AnimeSeason.Fall => "automne",
			_ => throw new ArgumentOutOfRangeException(nameof(date.Season), date.Season, null)
		};

		return $"https://www.nautiljon.com/animes/{season}-{date.Year}.html?format=1&y=0&tri=p&public_averti=1&simulcast=";
	}

	public async Task<AnimeBase[]> GetAnimes(AnimeDate date)
	{
		using var _ = LogAdapter($"{Log.F(date)}");

		var html = await GetAnimesHtml(date);

		var dom = new HtmlDocument();
		dom.LoadHtml(html);

		var nodes = dom.DocumentNode.SelectNodes("//div[@class='elt' and not(ancestor::div[@id='saison_continue'])]");

		var animes =  nodes.Select(a =>
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(a.InnerHtml);
			return _animeTileAssembler.Convert(date, doc);
		}).ToArray();

		return animes;
	}


	public async Task<Episode[]> GetAnimeEpisodes(string animeUrl)
	{
		using var _ = LogAdapter($"{Log.F(animeUrl)}");

		var html = await  GetHtml(animeUrl);

		var dom = new HtmlDocument();
		dom.LoadHtml(html);


		return _episodesAssembler.Convert(dom);

	}

	private async Task<string> GetHtml(string url)
	{
		using var client = GetHttpClient();

		var content = await client.GetAsync(url);

		content.EnsureSuccessStatusCode();

		return await content.Content.ReadAsStringAsync();
	}



	private async Task<string> GetAnimesHtml(AnimeDate date)
	{
		var url = GetAnimesUrl(date);

		return await GetHtml(url);
	}

	private HttpClient GetHttpClient()
	{
		var client = _httpClientFactory.CreateClient(NautijonAdapter.ClientName);


		return client;
	}
}