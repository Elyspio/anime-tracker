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

	public async Task<AnimeEntity?> UpdateEpisodes(string animeUrl, Episode[] episodes, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(animeUrl)} {Log.F(episodes.Length)}");

		return await EntityCollection.FindOneAndUpdateAsync(
			anime => anime.Url == animeUrl,
			Update.Set(e => e.Episodes, episodes),
			new FindOneAndUpdateOptions<AnimeEntity> { ReturnDocument = ReturnDocument.After },
			cancellationToken);
	}

	public async Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> animes, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(date)} {Log.F(animes.Count)}");

		if (animes.Count == 0) return;

		var existing = (await EntityCollection.Find(SeasonFilter(date)).ToListAsync(cancellationToken))
			.ToDictionary(anime => anime.Url);

		var operations = animes.Select(WriteModel<AnimeEntity> (anime) =>
		{
			var entity = anime.Adapt<AnimeEntity>();

			if (!existing.TryGetValue(anime.Url, out var stored)) return new InsertOneModel<AnimeEntity>(entity);

			// Replacing wholesale would drop the episodes scraped on the previous pass: the season
			// list page does not carry them, they are fetched anime by anime afterwards.
			entity.Id = stored.Id;
			entity.Episodes = stored.Episodes;

			return new ReplaceOneModel<AnimeEntity>(Filter.Eq(e => e.Id, stored.Id), entity);
		});

		await EntityCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
	}

	private static FilterDefinition<AnimeEntity> SeasonFilter(AnimeDate date)
	{
		return Builders<AnimeEntity>.Filter.Where(anime => anime.Date.Year == date.Year && anime.Date.Season == date.Season);
	}
}
