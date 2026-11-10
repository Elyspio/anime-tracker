using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Studios;

namespace AnimeTracker.Abstractions.Interfaces.Adapters;

/// <summary>
///     Where anime data comes from. One call returns a whole season, episodes included — there is no
///     per-anime follow-up, which is what makes a refresh a matter of seconds.
/// </summary>
public interface IAnimeSourceAdapter
{
	Task<AnimeBase[]> GetSeason(AnimeDate date, CancellationToken cancellationToken = default);

	/// <summary>
	///     Everything a studio was the main studio of, in whatever order the source pages them. Null
	///     when the source knows no studio by that id.
	/// </summary>
	Task<StudioFilmographyBase?> GetStudio(int studioId, CancellationToken cancellationToken = default);
}
