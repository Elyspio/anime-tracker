using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using AnimeTracker.Abstractions.Models.Base.Anime;
using HtmlAgilityPack;

namespace AnimeTracker.Adapters.Nautijon.Assemblers;

/// <summary>
///     Converts one tile of the season listing into an <see cref="AnimeBase" />.
///     Selectors match on a single class token rather than the whole attribute: the site adds
///     presentational classes (genres_scrollable and friends) without warning, and an exact match
///     turns that into a crash on every scrape.
/// </summary>
public partial class AnimeTileAssembler
{
	public AnimeBase Convert(AnimeDate date, HtmlDocument doc)
	{
		var node = doc.DocumentNode;

		var infos = node.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' infos ')]");
		var titleLink = node.SelectSingleNode("//h2/a");

		return new AnimeBase
		{
			Date = date,
			Title = Decode(titleLink?.InnerText),
			Studio = Decode(infos?.Descendants("a").FirstOrDefault()?.InnerText),
			Description = Decode(node.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' texte ')]")?.InnerText),
			ImageUrl = GetImageUrl(node),
			Url = $"https://nautiljon.com{titleLink?.Attributes["href"]?.Value ?? ""}",
			EpisodesCount = GetEpisodesCount(infos),
			Episodes = [],
			Tags = GetTags(node),
			Score = GetScore(node),
			Popularity = GetPopularity(node)
		};
	}

	private static string Decode(string? value)
	{
		return HttpUtility.HtmlDecode(value ?? "").Trim();
	}

	private static AnimeTag[] GetTags(HtmlNode node)
	{
		var container = node.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' tagsList ')]");

		return container?.Elements("a")
			.Select(link => new AnimeTag(Decode(link.InnerText), link.Attributes["href"]?.Value ?? ""))
			.Where(tag => tag.Name.Length > 0)
			.ToArray() ?? [];
	}

	/// <summary>
	///     The rating and member count sit in spans that also carry an icon glyph from a private-use
	///     font, so the text has to be reduced to its number rather than parsed whole.
	/// </summary>
	private static double? GetScore(HtmlNode node)
	{
		var text = Infos2Span(node, 1);
		if (text is null) return null;

		var match = DecimalNumber().Match(text.Replace(',', '.'));

		return match.Success && double.TryParse(match.Value, CultureInfo.InvariantCulture, out var score) ? score : null;
	}

	private static int GetPopularity(HtmlNode node)
	{
		var text = Infos2Span(node, 2);
		if (text is null) return 0;

		var digits = IntegerNumber().Match(text.Replace(" ", "").Replace(" ", ""));

		return digits.Success && int.TryParse(digits.Value, out var popularity) ? popularity : 0;
	}

	private static string? Infos2Span(HtmlNode node, int index)
	{
		var spans = node
			.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' infos2 ')]")
			?.Elements("span")
			.ToArray();

		return spans is not null && spans.Length > index ? spans[index].InnerText : null;
	}

	private static int? GetEpisodesCount(HtmlNode? infos)
	{
		var text = infos?.Elements("span").FirstOrDefault(span => span.InnerText.Contains("eps"))?.InnerText;
		if (text is null) return null;

		var match = IntegerNumber().Match(text);

		return match.Success && int.TryParse(match.Value, out var count) ? count : null;
	}

	/// <summary>Pulls the cover out of the inline <c>background-image:url(...)</c> declaration.</summary>
	private static string GetImageUrl(HtmlNode node)
	{
		var style = node
			.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' image ')]")
			?.Attributes["style"]?.Value;

		return style is null ? "" : BackgroundImageUrl().Match(style) is { Success: true } match ? match.Groups[1].Value : "";
	}

	[GeneratedRegex(@"\d+(\.\d+)?")]
	private static partial Regex DecimalNumber();

	[GeneratedRegex(@"\d+")]
	private static partial Regex IntegerNumber();

	[GeneratedRegex(@"url\(\s*['""]?(https?://[^'""\)]+)")]
	private static partial Regex BackgroundImageUrl();
}
