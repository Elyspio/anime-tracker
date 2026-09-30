using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

/// <summary>
///     One measurement per anime and platform. Kept apart from the animes because a season refresh
///     replaces those wholesale, and apart from the overrides because a sync must never touch those.
/// </summary>
public interface IDubMatchRepository
{
	Task<List<DubMatchEntity>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>Replaces the measurement of the match's anime on the match's platform, or adds it.</summary>
	Task Save(DubMatchBase match, CancellationToken cancellationToken = default);

	Task Delete(int sourceId, DubPlatform platform, CancellationToken cancellationToken = default);

	/// <summary>Drops the matches of a season's animes that are no longer in it.</summary>
	Task<long> DeleteAllBut(AnimeDate date, IReadOnlyCollection<int> sourceIds, CancellationToken cancellationToken = default);
}
