using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.AniList.GraphQL;

namespace AnimeTracker.Adapters.AniList.Assemblers;

/// <summary>Turns one AniList media node into the model the rest of the application speaks.</summary>
internal class MediaAssembler
{
	/// <summary>
	///     Japan Standard Time, the calendar an airing schedule is expressed in. AniList publishes
	///     each slot as a Unix timestamp, and a late-night broadcast — 01:30 on a Saturday, which is
	///     an entire genre's usual slot — lands on the previous day once read as UTC. JST has never
	///     observed daylight saving, so the offset is a constant rather than a tzdb lookup.
	/// </summary>
	private static readonly TimeSpan BroadcastOffset = TimeSpan.FromHours(9);

	public AnimeBase Convert(AnimeDate date, Media media)
	{
		var title = Title(media);

		return new AnimeBase
		{
			SourceId = media.Id,
			Date = date,
			Title = title,
			AlternativeTitles = AlternativeTitles(media, title),
			Description = media.Description?.Trim() ?? "",
			Studio = media.Studios?.Nodes?.FirstOrDefault()?.Name ?? "",
			ImageUrl = media.CoverImage?.Large ?? "",
			Url = media.SiteUrl ?? "",
			Format = Format(media.Format),
			IsAdult = media.IsAdult,
			// AniList grades out of 100; every screen in this product speaks out of 10.
			Score = media.AverageScore is { } average ? average / 10d : null,
			Popularity = media.Popularity ?? 0,
			VotesCount = Votes(media),
			EpisodesCount = media.Episodes,
			Genres = media.Genres?.ToArray() ?? [],
			Episodes = Schedule(media),
			StreamingLinks = StreamingLinks(media)
		};
	}

	/// <summary>
	///     Romaji first: it is how the community and every simulcast catalogue name a show, and many
	///     anime never get an English title at all.
	/// </summary>
	private static string Title(Media media)
	{
		var romaji = media.Title?.Romaji;
		if (!string.IsNullOrWhiteSpace(romaji)) return romaji.Trim();

		return media.Title?.English?.Trim() ?? "";
	}

	/// <summary>
	///     English, native and synonyms, minus the displayed title. Synonyms repeat the other titles
	///     with different casing often enough that duplicates are compared case-insensitively.
	/// </summary>
	private static string[] AlternativeTitles(Media media, string title)
	{
		IEnumerable<string?> candidates = [media.Title?.English, media.Title?.Native, .. media.Synonyms ?? []];

		return candidates
			.Select(candidate => candidate?.Trim() ?? "")
			.Where(candidate => candidate.Length > 0 && !string.Equals(candidate, title, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	/// <summary>
	///     The number of grades, summed from the distribution buckets — AniList exposes the histogram
	///     but no total. Distinct from popularity, which counts everyone who listed the show.
	/// </summary>
	private static int? Votes(Media media)
	{
		var buckets = media.Stats?.ScoreDistribution;

		return buckets is null ? null : buckets.Sum(bucket => bucket.Amount);
	}

	private static AnimeFormat Format(string? format)
	{
		return format?.ToUpperInvariant() switch
		{
			"TV" => AnimeFormat.Tv,
			"TV_SHORT" => AnimeFormat.TvShort,
			"ONA" => AnimeFormat.Ona,
			"OVA" => AnimeFormat.Ova,
			"MOVIE" => AnimeFormat.Movie,
			"SPECIAL" => AnimeFormat.Special,
			"MUSIC" => AnimeFormat.Music,
			// A new media type upstream must not take a whole season down with it.
			_ => AnimeFormat.Unknown
		};
	}

	private static Episode[] Schedule(Media media)
	{
		var nodes = media.AiringSchedule?.Nodes;
		if (nodes is null) return [];

		return nodes
			.Select(node => new Episode(node.Episode, BroadcastDate(node.AiringAt)))
			.OrderBy(episode => episode.Number)
			.ToArray();
	}

	/// <summary>
	///     Where the show can be watched, one link per platform. A link to a platform's bare home page
	///     says nothing about which show it was meant for, so only links to a page are kept. Anything
	///     that is not a plain http(s) address is dropped too: the link ends up as an <c>href</c>, and
	///     the source is edited by anyone with an account.
	///     <para>
	///         The <c>isDisabled</c> flag is deliberately ignored. AniList sets it on links its checker
	///         could not fetch, and on the autumn 2026 season every disabled Crunchyroll link that named
	///         a series (7 of 7) pointed at a series that exists and matches the show — the checker is
	///         turned away by the platform, the link is not dead.
	///     </para>
	/// </summary>
	private static StreamingLink[] StreamingLinks(Media media)
	{
		return (media.ExternalLinks ?? [])
			.Where(link => string.Equals(link.Type, "STREAMING", StringComparison.OrdinalIgnoreCase))
			.Select(link => new StreamingLink(link.Site?.Trim() ?? "", link.Url?.Trim() ?? ""))
			.Where(link => link.Site.Length > 0 && IsPageOfASite(link.Url))
			.DistinctBy(link => link.Site, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private static bool IsPageOfASite(string url)
	{
		return Uri.TryCreate(url, UriKind.Absolute, out var uri)
			&& uri.Scheme is "http" or "https"
			&& uri.AbsolutePath.Length > 1;
	}

	private static DateOnly BroadcastDate(long airingAt)
	{
		return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(airingAt).ToOffset(BroadcastOffset).DateTime);
	}
}
