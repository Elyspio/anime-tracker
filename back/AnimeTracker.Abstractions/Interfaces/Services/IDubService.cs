using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Abstractions.Interfaces.Services;

/// <summary>The French dub: matching a season's animes to platform series, and letting an admin correct it.</summary>
public interface IDubService
{
	/// <summary>
	///     Queues the dub sync of a season and returns its run. Null when nothing was queued: a sync of
	///     that season is already queued or running, or no platform is configured. A run that could not
	///     be handed to the scheduler is recorded as failed and returned.
	/// </summary>
	Task<RefreshRun?> QueueSync(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>Matches every aired anime of the season on every platform, reporting against <paramref name="runId" />.</summary>
	Task Sync(AnimeDate date, Guid runId, CancellationToken cancellationToken = default);

	/// <summary>The season's matches worth an admin's look, most popular anime first.</summary>
	Task<IReadOnlyCollection<DubCase>> GetCases(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>Saves an admin's correction, then applies it to that one anime straight away.</summary>
	Task<DubOverrideResult> SetOverride(int sourceId, DubPlatform platform, DubOverrideRequest request,
		CancellationToken cancellationToken = default);
}
