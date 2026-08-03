using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

public interface IAnimeRepository : ICrudRepository<AnimeEntity, AnimeBase>
{
	Task<List<AnimeEntity>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>
	///     Replaces a season with what the source just returned, keyed by the source's own id.
	///     Nothing is merged with what was stored: the fetch carries every field, episodes included,
	///     so a stored value can only be older than the one replacing it.
	/// </summary>
	Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> animes, CancellationToken cancellationToken = default);
}
