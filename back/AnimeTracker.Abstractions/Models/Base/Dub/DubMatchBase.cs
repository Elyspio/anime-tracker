using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>
///     What the last sync learned about one anime on one platform. This is the measurement, and only
///     the measurement: whether the dub is up to date or complete depends on today's date and is
///     computed on every read, like the binge prediction.
/// </summary>
public class DubMatchBase
{
	/// <summary>The anime's AniList id — its identity, like everywhere else.</summary>
	public required int SourceId { get; set; }

	/// <summary>The season the anime belongs to, so a sync can drop the matches of animes that left it.</summary>
	public required AnimeDate Date { get; set; }

	public required DubPlatform Platform { get; set; }

	public required DubMatchStatus Status { get; set; }

	/// <summary>Null when nothing was found.</summary>
	public required DubMatchMethod? Method { get; set; }

	/// <summary>The platform's own series id. Set when a series was found, aligned or not.</summary>
	public required string? SeriesId { get; set; }

	/// <summary>The platform's title for the series, so an admin can tell a wrong match at a glance.</summary>
	public required string? SeriesTitle { get; set; }

	/// <summary>The series page on the platform: where a reader who wants the dub is sent.</summary>
	public required string? Url { get; set; }

	/// <summary>
	///     AniList episode numbers the platform has at all, after alignment. Empty unless
	///     <see cref="Status" /> is <see cref="DubMatchStatus.Matched" />.
	/// </summary>
	public required IReadOnlyCollection<int> AvailableEpisodes { get; set; }

	/// <summary>AniList episode numbers the platform has with French audio. A subset of <see cref="AvailableEpisodes" />.</summary>
	public required IReadOnlyCollection<int> FrenchEpisodes { get; set; }

	public required DateTimeOffset CheckedAt { get; set; }
}
