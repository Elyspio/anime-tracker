using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Adapters.MongoDB.Repositories.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories;

internal class AnimeRepository(IMongoDatabase database, ILogger<AnimeRepository> logger)
	: CrudRepository<AnimeEntity, AnimeBase>(database, logger), IAnimeRepository
{
	public async Task<List<AnimeEntity>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(date)}");

		return await EntityCollection.Find(SeasonFilter(date)).ToListAsync(cancellationToken);
	}

	public async Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> animes, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(date)} {Log.F(animes.Count)}");

		if (animes.Count == 0) return;

		var existing = (await EntityCollection.Find(SeasonFilter(date)).ToListAsync(cancellationToken))
			.ToDictionary(anime => anime.SourceId);

		// Replace wholesale: one fetch carries every field an anime has, episodes included, so there
		// is nothing stored worth merging in. Only the document id has to survive.
		var operations = animes.Select(WriteModel<AnimeEntity> (anime) =>
		{
			var entity = anime.Adapt<AnimeEntity>();

			if (!existing.TryGetValue(anime.SourceId, out var stored)) return new InsertOneModel<AnimeEntity>(entity);

			entity.Id = stored.Id;

			return new ReplaceOneModel<AnimeEntity>(Filter.Eq(e => e.Id, stored.Id), entity);
		});

		await EntityCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
	}

	private static FilterDefinition<AnimeEntity> SeasonFilter(AnimeDate date)
	{
		return Builders<AnimeEntity>.Filter.Where(anime => anime.Date.Year == date.Year && anime.Date.Season == date.Season);
	}
}
