using System.Globalization;
using System.Web;
using AnimeTracker.Abstractions.Models.Base.Anime;
using HtmlAgilityPack;

namespace AnimeTracker.Adapters.Nautijon.Assemblers;

public class AnimeEpisodesAssembler
{
	/// <summary>
	///     The site is French and dates its episodes dd/MM/yyyy. Parsing them against the ambient
	///     culture reads 05/07/2026 as 5 May under the invariant culture a container defaults to,
	///     which is two months of error injected straight into the binge countdown.
	/// </summary>
	private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

	public Episode[] Convert(HtmlDocument doc)
	{
		var urls = GetEpisodesUrls(doc);

		var rows = doc.DocumentNode.SelectNodes("//div[@id='episodes']//tbody/tr");
		if (rows is null) return [];

		return rows.Select(row => ParseRow(row, urls)).OfType<Episode>().ToArray();
	}

	private static Episode? ParseRow(HtmlNode row, Dictionary<double, string> urls)
	{
		var cells = row.SelectNodes("td");

		// Header and separator rows share the table; they carry no episode number.
		if (cells is null || cells.Count < 2) return null;
		if (!double.TryParse(cells[0].InnerText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
			return null;

		var link = cells[1].SelectSingleNode("a");
		if (link is null) return null;

		return new Episode
		{
			Number = number,
			Title = HttpUtility.HtmlDecode(link.InnerText.Trim()),
			Url = urls.GetValueOrDefault(number),
			ReleaseDate = cells.Count >= 3 ? ParseDate(cells[2].InnerText) : null
		};
	}

	private static DateOnly? ParseDate(string text)
	{
		var trimmed = HttpUtility.HtmlDecode(text).Trim();

		return DateOnly.TryParseExact(trimmed, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
			? date
			: null;
	}

	/// <summary>Streaming links, keyed by episode number, from the simulcast panel.</summary>
	private static Dictionary<double, string> GetEpisodesUrls(HtmlDocument doc)
	{
		var links = doc.DocumentNode.SelectNodes("//div[@id='simulcast']//tbody/tr/td/a");
		if (links is null) return [];

		var result = new Dictionary<double, string>();

		foreach (var link in links)
		{
			var parts = HttpUtility.HtmlDecode(link.InnerText).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2) continue;
			if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) continue;

			var href = link.Attributes["href"]?.Value;
			if (href is not null) result.TryAdd(number, href);
		}

		return result;
	}
}
