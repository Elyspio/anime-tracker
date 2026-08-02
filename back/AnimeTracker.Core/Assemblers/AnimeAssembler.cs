using AnimeTracker.Abstractions.Common.Extensions;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Services;

namespace AnimeTracker.Core.Assemblers;

/// <summary>Entity to transport. Hand-written so the exposed shape stays greppable.</summary>
public static class AnimeAssembler
{
	public static Anime Convert(AnimeEntity entity, DateOnly today)
	{
		return new Anime
		{
			Id = entity.Id.AsGuid(),
			Date = entity.Date,
			Title = entity.Title,
			Studio = entity.Studio,
			Description = entity.Description,
			ImageUrl = entity.ImageUrl,
			Url = entity.Url,
			Score = entity.Score,
			Popularity = entity.Popularity,
			EpisodesCount = entity.EpisodesCount,
			Episodes = entity.Episodes,
			Tags = entity.Tags,
			Binge = BingePredictor.Predict(entity.Episodes, entity.EpisodesCount, today)
		};
	}
}
