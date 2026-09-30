using System.Globalization;
using System.Text.RegularExpressions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Adapters.Crunchyroll.Configs;
using AnimeTracker.Adapters.Crunchyroll.Http;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimeTracker.Adapters.Crunchyroll.Adapters;

/// <summary>The only project that knows Crunchyroll's API exists.</summary>
internal partial class CrunchyrollAdapter(
	CrunchyrollClient client,
	IOptions<CrunchyrollOptions> options,
	ILogger<CrunchyrollAdapter> logger
) : TracingAdapter(logger), IDubPlatformAdapter
{
	private const string French = "fr-FR";

	/// <summary>Enough to see the right series near the top without paying for a page of noise.</summary>
	private const int SearchSize = 6;

	public DubPlatform Platform => DubPlatform.Crunchyroll;

	public bool Owns(string url)
	{
		return Uri.TryCreate(url, UriKind.Absolute, out var uri)
			&& uri.Scheme is "http" or "https"
			&& (uri.Host.Equals("crunchyroll.com", StringComparison.OrdinalIgnoreCase)
				|| uri.Host.EndsWith(".crunchyroll.com", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	///     <c>/series/GT00377907/black-torch</c>, with or without the slug, with or without a locale
	///     prefix (<c>/fr/</c>, <c>/pt-pt/</c>). The old slug-only addresses carry no id and are refused.
	/// </summary>
	public string? ParseSeriesId(string url)
	{
		if (!Owns(url)) return null;

		return SeriesPath().Match(new Uri(url).AbsolutePath) is { Success: true } match
			? match.Groups["id"].Value.ToUpperInvariant()
			: null;
	}

	public async Task<IReadOnlyList<PlatformSeriesSummary>> Search(string query, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(query)}");

		var reply = await client.Get<ListReply<SearchGroup>>("/content/v2/discover/search", new Dictionary<string, string>
		{
			["q"] = query,
			["n"] = SearchSize.ToString(CultureInfo.InvariantCulture),
			["type"] = "series",
			["locale"] = options.Value.Locale
		}, cancellationToken);

		return (reply?.Data ?? [])
			.Where(group => group.Type == "series")
			.SelectMany(group => group.Items ?? [])
			.Where(series => series is { Id.Length: > 0, Title.Length: > 0 })
			.Select(series => new PlatformSeriesSummary(series.Id!, series.Title!, []))
			.ToArray();
	}

	public async Task<PlatformSeries?> GetSeries(string seriesId, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(seriesId)}");

		var series = (await client.Get<ListReply<SeriesNode>>($"/content/v2/cms/series/{seriesId}", Locale(), cancellationToken))?.Data?.FirstOrDefault();
		if (series?.Id is null) return null;

		var seasons = (await client.Get<ListReply<SeasonNode>>($"/content/v2/cms/series/{seriesId}/seasons", Locale(), cancellationToken))?.Data ?? [];

		var originals = seasons.Where(IsOriginal).ToArray();

		return new PlatformSeries(
			series.Id,
			series.Title ?? "",
			SeriesUrl(series),
			// Only the seasons in their original audio: an older series also lists each dub as a season
			// of its own, which would put two seasons on the same broadcast date and block alignment.
			// The dubs are read off each episode's versions instead.
			(originals.Length > 0 ? originals : seasons)
			.Where(season => season.Id is not null)
			.Select(season => new PlatformSeason(season.Id!, (int)(season.SeasonSequenceNumber ?? season.SeasonNumber ?? 0), null))
			.ToArray());
	}

	public async Task<IReadOnlyList<PlatformEpisode>> GetEpisodes(string seriesId, string seasonId, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(seriesId)} {Log.F(seasonId)}");

		var episodes = (await client.Get<ListReply<EpisodeNode>>($"/content/v2/cms/seasons/{seasonId}/episodes", Locale(), cancellationToken))?.Data ?? [];

		return episodes
			.Select(episode => new PlatformEpisode(
				episode.SequenceNumber,
				AirDate(episode.EpisodeAirDate),
				// Every audio track of the episode is listed on it. is_dubbed is no help: it is true as
				// soon as any dub exists, English included.
				episode.Versions is { Count: > 0 } versions
					? versions.Any(version => version.AudioLocale == French)
					: episode.AudioLocale == French,
				ReleaseDate(episode.PremiumAvailableDate)))
			.ToArray();
	}

	private Dictionary<string, string> Locale()
	{
		return new Dictionary<string, string> { ["locale"] = options.Value.Locale };
	}

	private string SeriesUrl(SeriesNode series)
	{
		var root = options.Value.BaseUrl.TrimEnd('/');

		return string.IsNullOrWhiteSpace(series.SlugTitle) ? $"{root}/series/{series.Id}" : $"{root}/series/{series.Id}/{series.SlugTitle}";
	}

	private static bool IsOriginal(SeasonNode season)
	{
		return season.Versions is not { Count: > 0 } versions || versions.Any(version => version.Original == true && version.Guid == season.Id);
	}

	/// <summary>The day part of the timestamp: Crunchyroll dates a broadcast at midnight UTC on its day.</summary>
	private static DateOnly? AirDate(string? timestamp)
	{
		return DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
			? DateOnly.FromDateTime(date.UtcDateTime)
			: null;
	}

	/// <summary>
	///     When the platform released it. An anonymous visitor is shown 9998-11-30 on some dates as a
	///     placeholder, which is no date at all.
	/// </summary>
	private static DateOnly? ReleaseDate(string? timestamp)
	{
		return AirDate(timestamp) is { Year: < 9000 } date ? date : null;
	}

	[GeneratedRegex(@"^/(?:[a-z]{2}(?:-[a-z]{2})?/)?series/(?<id>[A-Za-z0-9]+)(?:/|$)", RegexOptions.IgnoreCase)]
	private static partial Regex SeriesPath();
}
