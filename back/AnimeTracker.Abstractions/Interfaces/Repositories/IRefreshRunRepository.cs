using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

/// <summary>
///     Records what each season refresh did. Hangfire knows a job ran; only this knows which season
///     it touched, how many animes it stored, and what stopped it.
/// </summary>
public interface IRefreshRunRepository
{
	/// <summary>Records a run accepted by the API, before any worker has picked it up.</summary>
	Task<RefreshRunEntity> Queue(Guid runId, AnimeDate date, DateTimeOffset now, CancellationToken cancellationToken = default);

	/// <summary>
	///     Marks the run as running. Upserts, because the daily recurring job starts its own run
	///     without ever passing through the API.
	/// </summary>
	Task Begin(Guid runId, AnimeDate date, DateTimeOffset now, CancellationToken cancellationToken = default);

	/// <summary>Closes a run, recording how many animes it stored and why it stopped.</summary>
	Task Finish(Guid runId, RefreshStatus status, int total, string? error, DateTimeOffset now,
		CancellationToken cancellationToken = default);

	/// <summary>The run queued or running for a season, if any. What makes a duplicate refresh a 409.</summary>
	Task<RefreshRunEntity?> GetActive(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>Most recent runs first, all seasons.</summary>
	Task<List<RefreshRunEntity>> GetRecent(int limit, CancellationToken cancellationToken = default);

	/// <summary>
	///     Closes every run still marked running as <see cref="RefreshStatus.Interrupted" />. Called at
	///     startup: the deployment runs a single replica, so no other process can own a live run.
	/// </summary>
	Task<long> MarkRunningAsInterrupted(DateTimeOffset now, CancellationToken cancellationToken = default);
}
