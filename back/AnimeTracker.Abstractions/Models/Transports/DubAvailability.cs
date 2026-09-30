using AnimeTracker.Abstractions.Models.Base.Dub;

namespace AnimeTracker.Abstractions.Models.Transports;

/// <summary>
///     The French dub of an anime on one platform, as of today. Computed on every read from the stored
///     measurement and the airing schedule, never stored: "up to date" goes stale the day the next
///     episode airs in Japan.
/// </summary>
public sealed record DubAvailability
{
	public required DubPlatform Platform { get; init; }

	/// <summary>The series page on the platform.</summary>
	public required string Url { get; init; }

	/// <summary>
	///     Episodes with French audio, counted among the announced ones when the total is known — the
	///     numerator of "8 of 12".
	/// </summary>
	public required int FrenchEpisodes { get; init; }

	/// <summary>Episodes announced, as AniList says. Null when it does not.</summary>
	public required int? TotalEpisodes { get; init; }

	/// <summary>
	///     At least one episode has aired in Japan, and every one that has is out in French on this
	///     platform. Compared episode by episode, never by counting.
	/// </summary>
	public required bool UpToDate { get; init; }

	/// <summary>The total is announced, and every episode from the first to the last is out in French here.</summary>
	public required bool Complete { get; init; }

	public required DateTimeOffset CheckedAt { get; init; }
}
