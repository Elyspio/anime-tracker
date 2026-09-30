namespace AnimeTracker.Abstractions.Models.Base.Refresh;

/// <summary>
///     What a run refreshes. Serialised by name, and mirrored by a hand-written TypeScript union in the
///     frontend. Runs stored before the field existed read back as <see cref="Season" />.
/// </summary>
public enum RefreshKind
{
	/// <summary>The season listing and schedule, from AniList. About a second.</summary>
	Season,

	/// <summary>The French dub of every anime of the season, from the streaming platforms. Minutes.</summary>
	Dub
}
