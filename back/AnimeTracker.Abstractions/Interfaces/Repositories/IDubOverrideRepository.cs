using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

/// <summary>An admin's corrections. Nothing but an admin action adds or removes one.</summary>
public interface IDubOverrideRepository
{
	Task<List<DubOverrideEntity>> GetBySourceIds(IReadOnlyCollection<int> sourceIds, CancellationToken cancellationToken = default);

	/// <summary>Replaces the override of the anime on the platform, or adds it.</summary>
	Task Save(DubOverrideBase @override, CancellationToken cancellationToken = default);

	Task Delete(int sourceId, DubPlatform platform, CancellationToken cancellationToken = default);
}
