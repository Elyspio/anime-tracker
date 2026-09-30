using System.Text.Json.Serialization;

namespace AnimeTracker.Adapters.Crunchyroll.Http;

// The shape of Crunchyroll's replies, as recorded under the tests' Fixtures/crunchyroll. Everything is
// nullable: the API is undocumented, and a field it stops sending must cost one anime, not the sync.

internal sealed record TokenReply(
	[property: JsonPropertyName("access_token")] string? AccessToken,
	[property: JsonPropertyName("expires_in")] int? ExpiresIn);

/// <summary>Every CMS endpoint wraps its payload in <c>data</c>, even the one that returns a single series.</summary>
internal sealed record ListReply<T>(IReadOnlyList<T>? Data);

/// <summary>Search groups its hits by type; only the <c>series</c> group is ever asked for.</summary>
internal sealed record SearchGroup(string? Type, IReadOnlyList<SeriesNode>? Items);

internal sealed record SeriesNode(
	string? Id,
	string? Title,
	[property: JsonPropertyName("slug_title")] string? SlugTitle);

internal sealed record SeasonNode(
	string? Id,
	[property: JsonPropertyName("season_number")] double? SeasonNumber,
	[property: JsonPropertyName("season_sequence_number")] double? SeasonSequenceNumber,
	IReadOnlyList<VersionNode>? Versions);

/// <summary>
///     One audio track of a season or an episode. On a season, <paramref name="Original" /> marks the
///     Japanese one; older series also list each dub as a season of its own, which is how those are
///     told apart from the real seasons.
/// </summary>
internal sealed record VersionNode(
	[property: JsonPropertyName("audio_locale")] string? AudioLocale,
	string? Guid,
	bool? Original);

/// <param name="SequenceNumber">
///     The position within the season — 1, 2, 3 — where <c>episode_number</c> keeps counting from the
///     previous season (13, 14…) and is null for specials. Fractional for a recap slotted in between.
/// </param>
/// <param name="EpisodeAirDate">The Japanese broadcast, midnight UTC on the day. A string: an empty one must not throw.</param>
internal sealed record EpisodeNode(
	[property: JsonPropertyName("sequence_number")] double? SequenceNumber,
	[property: JsonPropertyName("episode_air_date")] string? EpisodeAirDate,
	[property: JsonPropertyName("audio_locale")] string? AudioLocale,
	IReadOnlyList<VersionNode>? Versions,
	[property: JsonPropertyName("premium_available_date")] string? PremiumAvailableDate = null);
