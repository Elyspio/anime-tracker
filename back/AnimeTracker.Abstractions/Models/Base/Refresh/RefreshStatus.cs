namespace AnimeTracker.Abstractions.Models.Base.Refresh;

/// <summary>
///     Lifecycle of a season refresh. Serialised by name, and mirrored by a hand-written TypeScript
///     union in the frontend — renaming a member is a breaking change on both sides.
/// </summary>
public enum RefreshStatus
{
	/// <summary>Accepted and waiting for a worker. A refresh queued behind another one sits here.</summary>
	Queued,

	/// <summary>A worker is walking the season right now.</summary>
	Running,

	/// <summary>The walk finished and every anime was either scraped or deliberately skipped.</summary>
	Succeeded,

	/// <summary>The walk stopped on an error, or finished with animes it could not scrape.</summary>
	Failed,

	/// <summary>
	///     The process died while this run was in flight. Nothing is walking it any more, so it is
	///     closed at the next startup rather than left claiming progress forever.
	/// </summary>
	Interrupted
}
