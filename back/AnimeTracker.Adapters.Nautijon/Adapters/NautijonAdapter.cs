using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using AnimeTracker.Adapters.Nautijon.FlareSolverr;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Adapters.Nautijon.Adapters;

public class NautijonAdapter(
	FlareSolverrClient flareSolverr,
	AnimeTileAssembler animeTileAssembler,
	AnimeEpisodesAssembler episodesAssembler,
	ILogger<NautijonAdapter> logger
) : TracingAdapter(logger), INautijonAdapter
{
	public async Task<AnimeBase[]> GetAnimes(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(date)}");

		var dom = await Load(GetSeasonUrl(date), cancellationToken);

		// Shows carried over from the previous season live in their own block; they are not this
		// season's releases and would pollute the countdown list.
		var nodes = dom.DocumentNode.SelectNodes("//div[@class='elt' and not(ancestor::div[@id='saison_continue'])]");

		if (nodes is null) return [];

		return nodes.Select(node =>
		{
			var tile = new HtmlDocument();
			tile.LoadHtml(node.InnerHtml);
			return animeTileAssembler.Convert(date, tile);
		}).ToArray();
	}

	public async Task<Episode[]> GetAnimeEpisodes(string animeUrl, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(animeUrl)}");

		return episodesAssembler.Convert(await Load(animeUrl, cancellationToken));
	}

	internal static string GetSeasonUrl(AnimeDate date)
	{
		var season = date.Season switch
		{
			AnimeSeason.Winter => "hiver",
			AnimeSeason.Spring => "printemps",
			AnimeSeason.Summer => "été",
			AnimeSeason.Fall => "automne",
			_ => throw new ArgumentOutOfRangeException(nameof(date), date.Season, null)
		};

		return $"https://www.nautiljon.com/animes/{season}-{date.Year}.html?format=1&y=0&tri=p&public_averti=1&simulcast=";
	}

	private async Task<HtmlDocument> Load(string url, CancellationToken cancellationToken)
	{
		var dom = new HtmlDocument();
		dom.LoadHtml(await flareSolverr.GetHtml(url, cancellationToken));

		return dom;
	}
}
