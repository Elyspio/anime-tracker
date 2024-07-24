using System.Globalization;
using System.Web;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using HtmlAgilityPack;

namespace AnimeTracker.Api.Adapters.Rest.Assemblers;

public class AnimeEpisodesAssembler
{
	public Episode[] Convert(HtmlDocument doc)
	{
		var node = doc.DocumentNode;

		var urls = GetEpisodesUrls(doc);

		return node.SelectNodes("//div[@id='episodes']//tbody/tr")?.Select(tr =>
		{
			var tds = tr.SelectNodes("td");
			var number = System.Convert.ToDouble(tds[0].InnerText.Trim(), new CultureInfo("en-US", false));
			var link = tds[1].SelectSingleNode("a");

			var url = link.Attributes["href"].Value;
			var title = HttpUtility.HtmlDecode(link.InnerText.Trim());


			DateOnly? releaseDate = null;


			if (tds.Count == 3 && DateOnly.TryParse(tds[2].InnerText, out var parsedDate))
			{
				releaseDate = parsedDate;
			}

			return new Episode
			{
				Number = number,
				Title = title,
				Url = urls.GetValueOrDefault(number) ?? null,
				ReleaseDate = releaseDate
			};
		}).ToArray() ?? [];
	}

	private Dictionary<double, string> GetEpisodesUrls(HtmlDocument doc)
	{
		var node = doc.DocumentNode;

		var episodesNodes = node.SelectNodes("//div[@id='simulcast']//tbody/tr/td/a") ?? new HtmlNodeCollection(null);

		if (episodesNodes.Count == 0) return [];

		return episodesNodes.Select(linkNode =>
		{
			var text = linkNode.InnerText.Trim();
			var numberText = text.Split(' ')[1];
			var number = System.Convert.ToDouble(numberText, new CultureInfo("en-US", false));

			var url = linkNode.Attributes["href"].Value;

			return new KeyValuePair<double, string>(number, url);
		}).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}

}