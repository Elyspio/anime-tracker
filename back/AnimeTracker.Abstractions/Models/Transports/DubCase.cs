using AnimeTracker.Abstractions.Models.Base.Dub;

namespace AnimeTracker.Abstractions.Models.Transports;

/// <summary>
///     A match an admin may want to look at: one that could not be made although AniList lists the
///     platform, one made by a title search, or one an admin already overrode. Public, like the season:
///     it is a fact about a public schedule.
/// </summary>
public sealed record DubCase
{
	public required int SourceId { get; init; }

	public required string Title { get; init; }

	public required DubPlatform Platform { get; init; }

	/// <summary>Null when the anime has not been matched on this platform yet.</summary>
	public required DubMatchStatus? Status { get; init; }

	public required DubMatchMethod? Method { get; init; }

	public required string? SeriesTitle { get; init; }

	public required string? SeriesUrl { get; init; }

	public required int FrenchEpisodes { get; init; }

	public required DubOverrideMode Override { get; init; }

	public required DateTimeOffset? CheckedAt { get; init; }
}

/// <summary>What an admin sends to correct a match.</summary>
/// <param name="Url">The platform's series page. Required to pin, ignored otherwise.</param>
public sealed record DubOverrideRequest(DubOverrideMode Mode, string? Url);

/// <summary>
///     The case once the override is saved and applied. The override is saved even when applying it
///     fails; <paramref name="Error" /> then says why, and the next sync applies it.
/// </summary>
public sealed record DubOverrideResult(DubCase Case, string? Error);
