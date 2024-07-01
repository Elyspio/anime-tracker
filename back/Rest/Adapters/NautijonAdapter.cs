using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Adapters.Rest.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Api.Adapters.Rest.Adapters;

public class NautijonAdapter : TracingAdapter, INautijonAdapter
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly AnimeAssembler _animeAssembler;

	public NautijonAdapter(ILogger<NautijonAdapter> logger, IHttpClientFactory httpClientFactory, AnimeAssembler animeAssembler) : base(logger)
	{
		_httpClientFactory = httpClientFactory;
		_animeAssembler = animeAssembler;
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

		return $"https://www.nautiljon.com/animes/{season}-{date.Year}.html?format=1&y=0&tri=&public_averti=1&simulcast=";
	}

	public async Task<IReadOnlyCollection<Anime>> GetAnimes(AnimeDate date)
	{
		using var _ = LogAdapter($"{Log.F(date)}");

		var html = await GetAnimesHtml(date);

		var dom = new HtmlDocument();
		dom.LoadHtml(html);

		var nodes =  dom.DocumentNode.SelectNodes("//div[@class='elt']");

		return nodes.Select(a =>
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(a.InnerHtml);
			return _animeAssembler.Convert(date, doc);
		}).ToArray();
	}



	private async Task<string> GetAnimesHtml(AnimeDate date)
	{
		var url = GetAnimesUrl(date);

		using var client = _httpClientFactory.CreateClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");


		var content = await client.GetAsync(url);

		content.EnsureSuccessStatusCode();

		return await content.Content.ReadAsStringAsync();
	}
}