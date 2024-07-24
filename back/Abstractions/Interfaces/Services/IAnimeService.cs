using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Abstractions.Models.Transports;

namespace AnimeTracker.Api.Abstractions.Interfaces.Services;

public interface IAnimeService
{
	Task<IReadOnlyCollection<Anime>> GetAll();
	Task Refresh(string animeUrl);

	/// <summary>
	/// Refresh all animes
	/// </summary>
	/// <param name="date"></param>
	/// <returns></returns>
	Task RefreshAll(AnimeDate date);
}