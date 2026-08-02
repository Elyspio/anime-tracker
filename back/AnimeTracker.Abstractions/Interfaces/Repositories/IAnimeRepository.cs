using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

public interface IAnimeRepository : ICrudRepository<AnimeEntity, AnimeBase>
{
	Task<List<AnimeEntity>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default);

	Task<AnimeEntity?> UpdateEpisodes(string animeUrl, Episode[] episodes, CancellationToken cancellationToken = default);

	/// <summary>Upserts the scraped season, keyed by anime URL. Episodes are refreshed separately.</summary>
	Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> animes, CancellationToken cancellationToken = default);
}
