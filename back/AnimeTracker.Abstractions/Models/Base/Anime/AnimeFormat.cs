namespace AnimeTracker.Abstractions.Models.Base.Anime;

/// <summary>
///     How an anime is released. Mirrors AniList's MediaFormat for the values a season can contain,
///     and is a contract with the hand-written TypeScript union: the grid filters on it, and only
///     the episodic formats are shown by default.
/// </summary>
public enum AnimeFormat
{
	/// <summary>Anything the source reports that this enum does not know about.</summary>
	Unknown,

	Tv,

	/// <summary>A TV series of very short episodes. Same weekly rhythm, three minutes at a time.</summary>
	TvShort,

	/// <summary>Released on a streaming platform rather than broadcast. Weekly like TV.</summary>
	Ona,

	Ova,

	Movie,

	Special,

	Music
}
