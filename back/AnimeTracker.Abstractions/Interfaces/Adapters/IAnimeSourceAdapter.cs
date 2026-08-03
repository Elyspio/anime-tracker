using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Interfaces.Adapters;

/// <summary>
///     Where anime data comes from. One call returns a whole season, episodes included — there is no
///     per-anime follow-up, which is what makes a refresh a matter of seconds.
/// </summary>
public interface IAnimeSourceAdapter
{
	Task<AnimeBase[]> GetSeason(AnimeDate date, CancellationToken cancellationToken = default);
}
