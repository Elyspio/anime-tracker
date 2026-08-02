using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

public interface IAnimeRepository : ICrudRepository<AnimeEntity, AnimeBase>
{
	Task<AnimeEntity> UpdateEpisodes(string animeUrl, Episode[] episodes);
	Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> existingAnimes);
}