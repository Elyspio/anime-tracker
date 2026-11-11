using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Abstractions.Models.Entities;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

/// <summary>
///     One filmography per studio, keyed by the studio's AniList id. Kept apart from the animes: a
///     season refresh replaces those wholesale, and a filmography spans every season there is.
/// </summary>
public interface IStudioFilmographyRepository
{
	Task<StudioFilmographyEntity?> Get(int studioId, CancellationToken cancellationToken = default);

	/// <summary>Replaces the studio's filmography, or adds it.</summary>
	Task<StudioFilmographyEntity> Save(StudioFilmographyBase filmography, DateTimeOffset fetchedAt, CancellationToken cancellationToken = default);
}
