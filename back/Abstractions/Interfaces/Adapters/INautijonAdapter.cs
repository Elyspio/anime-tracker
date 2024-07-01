using AnimeTracker.Api.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Api.Abstractions.Interfaces.Adapters;

public interface INautijonAdapter
{
	Task<IReadOnlyCollection<Anime>> GetAnimes(AnimeDate date);
}