namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>
///     A streaming platform the French dub is measured on. Serialised by name, and mirrored by a
///     hand-written TypeScript union in the frontend — renaming a member is a breaking change on both
///     sides, and a stored match or override carries it too.
/// </summary>
public enum DubPlatform
{
	Crunchyroll,
	Adn
}
