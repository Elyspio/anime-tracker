namespace AnimeTracker.Adapters.Adn.Http;

// The shape of ADN's replies, as recorded under the tests' Fixtures/adn. Nullable throughout, like
// Crunchyroll's: an undocumented field that goes missing must cost one anime, not the sync.

internal sealed record CatalogReply(IReadOnlyList<ShowNode>? Shows);

internal sealed record ShowReply(ShowNode? Show);

/// <param name="OriginalTitle">The romaji title, which is what AniList names most shows by.</param>
/// <param name="Url">The show's page on the website.</param>
internal sealed record ShowNode(int? Id, string? Title, string? OriginalTitle, string? ShortTitle, string? Url);

internal sealed record VideosReply(IReadOnlyList<VideoNode>? Videos);

/// <param name="ShortNumber">"1", "12" — a string, and not always a number.</param>
/// <param name="Season">
///     "1", "2": one show often carries several seasons, the later ones numbered on from the earlier.
/// </param>
/// <param name="Languages"><c>vostf</c>, <c>vf</c> — the French dub is <c>vf</c>.</param>
/// <param name="Available">False for a video announced but not watchable yet.</param>
internal sealed record VideoNode(
	string? ShortNumber,
	string? Season,
	DateTimeOffset? ReleaseDate,
	IReadOnlyList<string>? Languages,
	bool? Available);
