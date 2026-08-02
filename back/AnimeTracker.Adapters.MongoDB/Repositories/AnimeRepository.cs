using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Adapters.MongoDB.Repositories.Base;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Entities;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace AnimeTracker.Adapters.MongoDB.Repositories;

internal class AnimeRepository(IConfiguration configuration, ILogger<BaseRepository<AnimeEntity>> logger) : CrudRepository<AnimeEntity, AnimeBase>(configuration, logger),	IAnimeRepository
{
	public async Task<AnimeEntity> UpdateEpisodes(string animeUrl, Episode[] episodes)
	{
		using var _ = LogRepository($"{Log.F(animeUrl)} {Log.F(episodes.Length)}");

		var update = Builders<AnimeEntity>.Update.Set(e => e.Episodes, episodes);

		return await EntityCollection.FindOneAndUpdateAsync(anime => anime.Url == animeUrl, update, new ()
		{
			ReturnDocument = ReturnDocument.After
		});

	}

	public async Task Refresh(AnimeDate date, IReadOnlyCollection<AnimeBase> animes)
	{
		using var _ = LogRepository($"{Log.F(date)} {Log.F(animes.Count)}");

		var existingAnimes = (await EntityCollection.AsQueryable().Where(anime => anime.Date.Season == date.Season && anime.Date.Year == date.Year).ToListAsync()).ToDictionary(anime => anime.Url);

		var operations =  animes.Select(WriteModel<AnimeEntity> (anime) =>
		{
			var animeEntity = anime.Adapt<AnimeEntity>();

			if (!existingAnimes.TryGetValue(anime.Url, out var existingAnime))
			{
				return new InsertOneModel<AnimeEntity>(animeEntity);
			}

			animeEntity.Id = existingAnime.Id;
			return new ReplaceOneModel<AnimeEntity>(Filter.Eq(e => e.Id, existingAnime.Id), animeEntity);

		});

		await EntityCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false });
	}
}