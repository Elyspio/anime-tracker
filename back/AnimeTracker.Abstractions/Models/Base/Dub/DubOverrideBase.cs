namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>
///     An admin's correction of how an anime is matched to a platform. Stored apart from the match it
///     corrects, so no sync can overwrite it; only another admin action removes it.
/// </summary>
public class DubOverrideBase
{
	public required int SourceId { get; set; }

	public required DubPlatform Platform { get; set; }

	/// <summary><see cref="DubOverrideMode.Pinned" /> or <see cref="DubOverrideMode.Blocked" />: an automatic match stores nothing.</summary>
	public required DubOverrideMode Mode { get; set; }

	/// <summary>The pinned series. Null when blocked.</summary>
	public required string? SeriesId { get; set; }

	public required DateTimeOffset UpdatedAt { get; set; }
}
