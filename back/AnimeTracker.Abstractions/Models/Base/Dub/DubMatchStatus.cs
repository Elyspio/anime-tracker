namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>Where the last attempt at matching an anime to a platform series ended. Serialised by name.</summary>
public enum DubMatchStatus
{
	/// <summary>One series, and within it one season, line up with the anime: the dub is measured.</summary>
	Matched,

	/// <summary>No series of the platform carries the anime's title. Most animes, on most platforms.</summary>
	NotFound,

	/// <summary>
	///     A series was found, but no single season of it lines up with the anime's first episode — or
	///     several series did. The dub is unknown, and an admin has to say which series it is.
	/// </summary>
	Unaligned
}
