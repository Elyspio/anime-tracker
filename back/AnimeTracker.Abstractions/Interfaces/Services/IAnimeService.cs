using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Abstractions.Interfaces.Services;

public interface IAnimeService
{
	/// <summary>Every anime of a season, ordered by how soon it becomes bingeable.</summary>
	Task<IReadOnlyCollection<Anime>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>
	///     Queues a season refresh and returns the run to follow. The fetch itself takes about a
	///     second, but it stays on the scheduler: that is what gives the nightly job and the button a
	///     single path, and what leaves a written trace of every run. A season already being
	///     refreshed is answered with that run rather than a second one.
	/// </summary>
	Task<RefreshQueueResult> QueueRefresh(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>Most recent refresh runs, newest first.</summary>
	Task<IReadOnlyCollection<RefreshRun>> GetRecentRuns(int limit, CancellationToken cancellationToken = default);

	/// <summary>Fetches a season from the source and replaces what is stored, reporting against <paramref name="runId" />.</summary>
	Task RefreshAll(AnimeDate date, Guid runId, CancellationToken cancellationToken = default);
}
