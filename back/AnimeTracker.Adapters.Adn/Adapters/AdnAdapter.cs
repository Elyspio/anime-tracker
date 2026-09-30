using System.Globalization;
using System.Text.RegularExpressions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Adapters.Adn.Http;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Adapters.Adn.Adapters;

/// <summary>The only project that knows ADN's gateway exists.</summary>
internal partial class AdnAdapter(AdnClient client, ILogger<AdnAdapter> logger) : TracingAdapter(logger), IDubPlatformAdapter
{
	private const string French = "vf";

	private const int SearchSize = 6;

	/// <summary>The gateway's own ceiling on a page of videos.</summary>
	private const int VideoPage = 100;

	/// <summary>A guard against a show that never stops paging; the longest ones are a few hundred episodes.</summary>
	private const int MaxVideoPages = 20;

	public DubPlatform Platform => DubPlatform.Adn;

	public bool Owns(string url)
	{
		return Uri.TryCreate(url, UriKind.Absolute, out var uri)
			&& uri.Scheme is "http" or "https"
			&& (uri.Host.Equals("animationdigitalnetwork.com", StringComparison.OrdinalIgnoreCase)
				|| uri.Host.EndsWith(".animationdigitalnetwork.com", StringComparison.OrdinalIgnoreCase)
				|| uri.Host.Equals("animationdigitalnetwork.fr", StringComparison.OrdinalIgnoreCase)
				|| uri.Host.EndsWith(".animationdigitalnetwork.fr", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary><c>/video/1331-hero-without-a-class-who-even-needs-skills</c>, an episode page below it included.</summary>
	public string? ParseSeriesId(string url)
	{
		if (!Owns(url)) return null;

		return ShowPath().Match(new Uri(url).AbsolutePath) is { Success: true } match ? match.Groups["id"].Value : null;
	}

	public async Task<IReadOnlyList<PlatformSeriesSummary>> Search(string query, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(query)}");

		var reply = await client.Get<CatalogReply>("/show/catalog", new Dictionary<string, string>
		{
			["search"] = query,
			["limit"] = SearchSize.ToString(CultureInfo.InvariantCulture),
			["offset"] = "0"
		}, cancellationToken);

		return (reply?.Shows ?? [])
			.Where(show => show is { Id: not null, Title.Length: > 0 })
			.Select(show => new PlatformSeriesSummary(
				show.Id!.Value.ToString(CultureInfo.InvariantCulture),
				show.Title!,
				new[] { show.OriginalTitle, show.ShortTitle }.OfType<string>().Where(title => title.Length > 0).ToArray()))
			.ToArray();
	}

	/// <summary>
	///     A show and every season in it, episodes included: ADN lists all of a show's videos at once, so
	///     splitting them per season costs nothing more.
	/// </summary>
	public async Task<PlatformSeries?> GetSeries(string seriesId, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(seriesId)}");

		var show = (await client.Get<ShowReply>($"/show/{seriesId}", new Dictionary<string, string>(), cancellationToken))?.Show;
		if (show?.Id is null) return null;

		var videos = await Videos(seriesId, cancellationToken);

		var seasons = videos
			.GroupBy(video => video.Season ?? "1")
			.Select(season => new PlatformSeason(
				$"{seriesId}:{season.Key}",
				int.TryParse(season.Key, CultureInfo.InvariantCulture, out var order) ? order : 0,
				season.Select(Episode).ToArray()))
			.ToArray();

		return new PlatformSeries(seriesId, show.Title ?? "", show.Url ?? $"https://animationdigitalnetwork.com/video/{seriesId}", seasons);
	}

	public async Task<IReadOnlyList<PlatformEpisode>> GetEpisodes(string seriesId, string seasonId, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(seriesId)} {Log.F(seasonId)}");

		// Only reached if a caller asks for a season again: GetSeries already hands every one over.
		var season = seasonId.Split(':').Last();

		return (await Videos(seriesId, cancellationToken)).Where(video => (video.Season ?? "1") == season).Select(Episode).ToArray();
	}

	private async Task<List<VideoNode>> Videos(string seriesId, CancellationToken cancellationToken)
	{
		var videos = new List<VideoNode>();

		for (var page = 0; page < MaxVideoPages; page++)
		{
			var reply = await client.Get<VideosReply>($"/video/show/{seriesId}", new Dictionary<string, string>
			{
				["offset"] = (page * VideoPage).ToString(CultureInfo.InvariantCulture),
				["limit"] = VideoPage.ToString(CultureInfo.InvariantCulture),
				["order"] = "asc"
			}, cancellationToken);

			var batch = reply?.Videos ?? [];
			videos.AddRange(batch);

			if (batch.Count < VideoPage) break;
		}

		return videos;
	}

	private static PlatformEpisode Episode(VideoNode video)
	{
		var date = video.ReleaseDate is { } released ? DateOnly.FromDateTime(released.UtcDateTime) : (DateOnly?)null;

		return new PlatformEpisode(
			double.TryParse(video.ShortNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null,
			date,
			// A video announced ahead of its release lists its languages already: only a watchable
			// one counts as dubbed.
			video.Available != false && (video.Languages?.Contains(French) ?? false),
			date);
	}

	[GeneratedRegex(@"^/video/(?<id>\d+)(?:-[^/]*)?(?:/|$)")]
	private static partial Regex ShowPath();
}
