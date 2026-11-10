using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Abstractions.Interfaces.Services;

public interface IStudioService
{
	/// <summary>
	///     The studio's filmography. Read from the store when it is there; fetched from the source and
	///     stored the first time anyone asks. Nothing refreshes it afterwards but <see cref="Refresh" />.
	/// </summary>
	Task<StudioFilmography> Get(int studioId, CancellationToken cancellationToken = default);

	/// <summary>
	///     The filmography of the studio an anime credits. An anime stored before studios had an id
	///     carries only a name: the source is asked for that one anime's studio, the answer is kept on
	///     the anime, and its season never has to be refreshed for the link to work.
	/// </summary>
	Task<StudioFilmography> GetByAnime(int sourceId, CancellationToken cancellationToken = default);

	/// <summary>Fetches the filmography from the source again and replaces what is stored.</summary>
	Task<StudioFilmography> Refresh(int studioId, CancellationToken cancellationToken = default);
}
