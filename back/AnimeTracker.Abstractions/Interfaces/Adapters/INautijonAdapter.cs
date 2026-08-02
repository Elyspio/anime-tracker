using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Interfaces.Adapters;

public interface INautijonAdapter
{
	Task<AnimeBase[]> GetAnimes(AnimeDate date);
	Task<Episode[]> GetAnimeEpisodes(string animeUrl);
}