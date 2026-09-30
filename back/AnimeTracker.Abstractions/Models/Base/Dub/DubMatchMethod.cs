namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>How the platform series was found. Serialised by name.</summary>
public enum DubMatchMethod
{
	/// <summary>AniList lists the series among the anime's streaming links.</summary>
	Link,

	/// <summary>A title search on the platform. The least certain of the three, so it is listed for review.</summary>
	Search,

	/// <summary>An admin named the series.</summary>
	Pinned
}
