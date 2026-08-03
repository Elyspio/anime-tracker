using System.Text.Json.Serialization;

namespace AnimeTracker.Adapters.AniList.GraphQL;

/// <summary>
///     The shape of an AniList reply. Everything is nullable because GraphQL answers a partial
///     result with errors rather than a status code, and because most fields are genuinely absent
///     for an anime that has only just been announced.
/// </summary>
internal sealed record GraphQlResponse<T>(T? Data, IReadOnlyList<GraphQlError>? Errors);

internal sealed record GraphQlError(string? Message);

internal sealed record SeasonData([property: JsonPropertyName("Page")] MediaPage? Page);

internal sealed record MediaPage(PageInfo? PageInfo, IReadOnlyList<Media>? Media);

internal sealed record PageInfo(bool HasNextPage);

internal sealed record Media(
	int Id,
	string? SiteUrl,
	string? Format,
	bool IsAdult,
	int? Episodes,
	int? AverageScore,
	int? Popularity,
	string? Description,
	MediaTitle? Title,
	CoverImage? CoverImage,
	IReadOnlyList<string>? Genres,
	StudioConnection? Studios,
	MediaStats? Stats,
	AiringConnection? AiringSchedule);

internal sealed record MediaTitle(string? Romaji, string? English);

internal sealed record CoverImage(string? Large);

internal sealed record StudioConnection(IReadOnlyList<Studio>? Nodes);

internal sealed record Studio(string? Name);

internal sealed record MediaStats(IReadOnlyList<ScoreBucket>? ScoreDistribution);

internal sealed record ScoreBucket(int Amount);

internal sealed record AiringConnection(IReadOnlyList<AiringEpisode>? Nodes);

/// <summary><paramref name="AiringAt" /> is a Unix timestamp in seconds.</summary>
internal sealed record AiringEpisode(int Episode, long AiringAt);
