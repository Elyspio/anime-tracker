using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Core.Assemblers;

/// <summary>
///     Entity to transport. What is shown and in what order is decided here, on every read, so a
///     change of rule applies to the filmographies already stored.
/// </summary>
public static class StudioAssembler
{
	public static StudioFilmography Convert(StudioFilmographyEntity entity)
	{
		return new StudioFilmography
		{
			StudioId = entity.StudioId,
			Name = entity.Name,
			Url = entity.Url,
			Works = Order(entity.Works.Where(work => work.Format != AnimeFormat.Music)),
			FetchedAt = entity.FetchedAt
		};
	}

	/// <summary>
	///     Newest first. A work nobody has dated is the newest thing the studio has, so it opens the
	///     list; for the same reason a missing month or day sorts ahead of every known one in its year
	///     — "2027" is later than anything the studio has dated in 2027, as far as anyone knows.
	/// </summary>
	public static StudioWork[] Order(IEnumerable<StudioWork> works)
	{
		return works
			.OrderByDescending(work => work.StartDate is null)
			.ThenByDescending(work => work.StartDate?.Year)
			.ThenByDescending(work => work.StartDate?.Month ?? int.MaxValue)
			.ThenByDescending(work => work.StartDate?.Day ?? int.MaxValue)
			.ThenBy(work => work.Title, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}
}
