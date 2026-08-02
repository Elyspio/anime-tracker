using System.Globalization;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using HtmlAgilityPack;

namespace AnimeTracker.Api.Adapters.Rest.Assemblers;

/// <summary>
/// Converts the HTML document from the anime list page into an AnimeBase object.
/// </summary>
public class AnimeTileAssembler
{
	public AnimeBase Convert(AnimeDate date, HtmlDocument doc)
	{
		var node = doc.DocumentNode;

		var traveler = doc.CreateNavigator()!;

		var header = (HtmlNodeNavigator)traveler.SelectSingleNode("//div[@class='title']")!;

		var infos = (HtmlNodeNavigator)traveler.SelectSingleNode("//div[@class='infos']")!;

		var tags = ((HtmlNodeNavigator)traveler.SelectSingleNode("//div[@class='genres tagsList']")!).CurrentNode.ChildNodes
			.Select(n => new AnimeTag(n.InnerText, n.Attributes["href"].Value))
			.ToArray();

		var title = node.SelectSingleNode("//h2/a").InnerText;

		var imgLink = GetImgLink(node);

		var epCount = GetEpisodesCount(infos);

		var animePath = ((HtmlNodeNavigator?)header.SelectSingleNode("//h2/a"))?.CurrentNode.Attributes["href"].Value ?? "";



		return new AnimeBase
		{
			Date = date,
			Title = title,
			Studio = infos.CurrentNode.Descendants("a").FirstOrDefault()?.InnerText ?? "",
			Description = ((HtmlNodeNavigator?)traveler.SelectSingleNode("texte"))?.CurrentNode.InnerText ?? "",
			ImageUrl = imgLink,
			Url = $"https://nautiljon.com{animePath}",
			EpisodesCount = epCount,
			Episodes = [],
			Tags = tags ?? [],
			Score = GetScore(node),
			Popularity = GetPopularity(node)
		};
	}

	private static double? GetScore(HtmlNode node)
	{
		double? score = null;
		var scoreStr = node.SelectSingleNode("//div[@class='infos2']")?.ChildNodes[1].InnerText.Trim();
		if (scoreStr is not null)
		{
			try
			{
				score = double.Parse(scoreStr.Split('/')[0], CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		return score;
	}

	private static int GetPopularity(HtmlNode node)
	{
		var popularityStr = node.SelectSingleNode("//div[@class='infos2']")?.ChildNodes[2].InnerText.Trim();

		return int.Parse(popularityStr?.Split('/')[0] ?? "-1");
	}
	private static int? GetEpisodesCount(HtmlNodeNavigator infos)
	{
		var epCountStr = infos.CurrentNode.SelectNodes("//span").FirstOrDefault(s => s.InnerText.Contains("eps"))?.InnerText;

		return epCountStr is not null && int.TryParse(epCountStr.Split(' ')[0], out var epCount) ? epCount : null;
	}

	private static string GetImgLink(HtmlNode node)
	{
		var imgStyle = node.SelectSingleNode("//div[@class='image relative']").Attributes["style"].Value;

		const string startMarker = "https";
		const string endMarker = ")";

		var indexOf = imgStyle.IndexOf(startMarker, StringComparison.Ordinal);
		return imgStyle[indexOf..imgStyle.LastIndexOf(endMarker, StringComparison.Ordinal)];
	}
}