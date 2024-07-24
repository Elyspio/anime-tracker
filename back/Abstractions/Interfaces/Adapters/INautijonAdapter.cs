using AnimeTracker.Api.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Api.Abstractions.Interfaces.Adapters;

public interface INautijonAdapter
{
	Task<AnimeBase[]> GetAnimes(AnimeDate date);
	Task<Episode[]> GetAnimeEpisodes(string animeUrl);
}