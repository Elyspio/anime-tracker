using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Base.Refresh;

/// <summary>
///     One execution of a season refresh or of the French dub sync that follows it, from the moment it
///     is queued to the moment it stops. A season refresh is a single fetch and lasts about a second, so
///     this is mostly a record of what happened rather than a progress report — which is exactly what a
///     job running unattended every night needs to leave behind.
/// </summary>
public class RefreshRunBase
{
	/// <summary>
	///     Identifies the run everywhere — minted when the job is queued, handed to Hangfire as a job
	///     argument, and returned to the caller. Deliberately ours rather than Hangfire's own job id,
	///     which the job could only read through a Hangfire type Core must not reference.
	/// </summary>
	public required Guid RunId { get; set; }

	public required AnimeDate Date { get; set; }

	/// <summary>Not required: runs stored before the field existed were all season refreshes.</summary>
	public RefreshKind Kind { get; set; } = RefreshKind.Season;

	public required RefreshStatus Status { get; set; }

	/// <summary>
	///     Animes stored for the season, or matched on a platform for a dub run. A dub run counts up while it
	///     works; a season refresh stays at zero until it has finished successfully.
	/// </summary>
	public required int Total { get; set; }

	public required DateTimeOffset StartedAt { get; set; }

	/// <summary>Last sign of life. A run whose UpdatedAt has stopped moving is stuck, not working.</summary>
	public required DateTimeOffset UpdatedAt { get; set; }

	public required DateTimeOffset? FinishedAt { get; set; }

	/// <summary>Exception message only — this is served anonymously, so never a stack trace.</summary>
	public required string? Error { get; set; }
}
