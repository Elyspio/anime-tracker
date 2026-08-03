using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Base.Refresh;

/// <summary>
///     One execution of a season refresh, from the moment it is queued to the moment it stops.
///     A refresh is now a single fetch and lasts about a second, so this is a record of what
///     happened rather than a progress report — which is exactly what a job running unattended
///     every night needs to leave behind.
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

	public required RefreshStatus Status { get; set; }

	/// <summary>Animes stored for the season. Zero until the run has finished successfully.</summary>
	public required int Total { get; set; }

	public required DateTimeOffset StartedAt { get; set; }

	/// <summary>Last sign of life. A run whose UpdatedAt has stopped moving is stuck, not working.</summary>
	public required DateTimeOffset UpdatedAt { get; set; }

	public required DateTimeOffset? FinishedAt { get; set; }

	/// <summary>Exception message only — this is served anonymously, so never a stack trace.</summary>
	public required string? Error { get; set; }
}
