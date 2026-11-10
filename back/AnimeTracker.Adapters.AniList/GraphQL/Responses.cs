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
	IReadOnlyList<string?>? Synonyms,
	CoverImage? CoverImage,
	IReadOnlyList<string>? Genres,
	StudioConnection? Studios,
	MediaStats? Stats,
	AiringConnection? AiringSchedule,
	IReadOnlyList<ExternalLink>? ExternalLinks);

internal sealed record MediaTitle(string? Romaji, string? English, string? Native);

internal sealed record CoverImage(string? ExtraLarge, string? Large);

internal sealed record StudioConnection(IReadOnlyList<Studio>? Nodes);

internal sealed record Studio(int? Id, string? Name);

internal sealed record StudioData([property: JsonPropertyName("Studio")] StudioDetails? Studio);

internal sealed record StudioDetails(int Id, string? Name, string? SiteUrl, StudioMediaConnection? Media);

internal sealed record StudioMediaConnection(PageInfo? PageInfo, IReadOnlyList<StudioMedia>? Nodes);

/// <summary>
///     A media node as the studio query asks for it: enough to list a work, not to predict anything.
///     <paramref name="Type" /> is ANIME or MANGA — a studio's media is not filtered by type upstream.
/// </summary>
internal sealed record StudioMedia(
	int Id,
	string? SiteUrl,
	string? Type,
	string? Format,
	bool IsAdult,
	int? Episodes,
	int? AverageScore,
	MediaTitle? Title,
	CoverImage? CoverImage,
	FuzzyDate? StartDate,
	string? Season,
	int? SeasonYear);

/// <summary>AniList's date: each part is published on its own, and any of them can be missing.</summary>
internal sealed record FuzzyDate(int? Year, int? Month, int? Day);

internal sealed record MediaStats(IReadOnlyList<ScoreBucket>? ScoreDistribution);

internal sealed record ScoreBucket(int Amount);

internal sealed record AiringConnection(IReadOnlyList<AiringEpisode>? Nodes);

/// <summary><paramref name="AiringAt" /> is a Unix timestamp in seconds.</summary>
internal sealed record AiringEpisode(int Episode, long AiringAt);

/// <summary><paramref name="Type" /> is INFO, STREAMING or SOCIAL.</summary>
internal sealed record ExternalLink(string? Site, string? Type, string? Url);
