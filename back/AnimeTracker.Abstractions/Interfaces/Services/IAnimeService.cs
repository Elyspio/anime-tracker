using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Abstractions.Interfaces.Services;

public interface IAnimeService
{
	/// <summary>Every anime of a season, ordered by how soon it becomes bingeable.</summary>
	Task<IReadOnlyCollection<Anime>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>
	///     Queues a season refresh and returns the job id. Scraping a season takes tens of minutes,
	///     which is far longer than any HTTP client will wait, so callers never run it inline.
	/// </summary>
	string QueueRefresh(AnimeDate date);

	/// <summary>Re-scrapes a season: the anime list, then each anime's episodes.</summary>
	Task RefreshAll(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>Re-scrapes the episodes of a single anime.</summary>
	Task Refresh(string animeUrl, CancellationToken cancellationToken = default);
}
