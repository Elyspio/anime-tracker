using System.ComponentModel.DataAnnotations;

namespace AnimeTracker.Adapters.AniList.Configs;

/// <summary>
///     AniList settings. The API is public and needs no credentials, so there is nothing here a
///     deployment is obliged to supply.
/// </summary>
public sealed class AniListOptions
{
	public const string SectionName = "AniList";

	[Required]
	public string Endpoint { get; set; } = "https://graphql.anilist.co";

	/// <summary>
	///     Media per page. AniList caps this at 50, and a season is two pages at most, so the whole
	///     refresh costs two of the 30 requests a minute the API allows.
	/// </summary>
	[Range(1, 50)]
	public int PageSize { get; set; } = 50;

	/// <summary>
	///     Guard against paging forever if the API ever stops setting hasNextPage honestly. No real
	///     season comes close.
	/// </summary>
	[Range(1, 100)]
	public int MaxPages { get; set; } = 20;
}
