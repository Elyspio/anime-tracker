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
			SourceId = entity.SourceId,
			Date = entity.Date,
			Title = entity.Title,
			AlternativeTitles = entity.AlternativeTitles,
			Description = entity.Description,
			Studio = entity.Studio,
			ImageUrl = entity.ImageUrl,
			Url = entity.Url,
			Format = entity.Format,
			IsAdult = entity.IsAdult,
			Score = entity.Score,
			Popularity = entity.Popularity,
			VotesCount = entity.VotesCount,
			EpisodesCount = entity.EpisodesCount,
			Genres = entity.Genres,
			Episodes = entity.Episodes,
			Binge = BingePredictor.Predict(entity.Episodes, entity.EpisodesCount, today)
		};
	}
}
