using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Abstractions.Models.Entities;

namespace AnimeTracker.Api.Abstractions.Interfaces.Repositories;

public interface IAnimeRepository : ICrudRepository<AnimeEntity, AnimeBase>
{
	Task<AnimeEntity> UpdateEpisodes(string animeUrl, Episode[] episodes);
	Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> existingAnimes);
}