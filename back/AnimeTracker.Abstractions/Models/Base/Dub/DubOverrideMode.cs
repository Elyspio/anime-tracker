namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>An admin's say on how an anime is matched to a platform. Serialised by name.</summary>
public enum DubOverrideMode
{
	/// <summary>No override: the sync matches on its own. Nothing is stored for it.</summary>
	Auto,

	/// <summary>This series and no other. A pinned series that stops lining up is reported, never replaced.</summary>
	Pinned,

	/// <summary>No series of this platform is this anime, whatever the titles say.</summary>
	Blocked
}
