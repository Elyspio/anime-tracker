using Elyspio.Utils.Telemetry.Technical.Helpers;
using AnimeTracker.Api.Abstractions.Interfaces.Business;
using AnimeTracker.Api.Abstractions.Interfaces.Repositories;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AnimeTracker.Api.Adapters.Mongo.Repositories.Base;

/// <inheritdoc cref="ICrudRepository{TEntity,TBase}" />
internal abstract class CrudRepository<TEntity, TBase>(IConfiguration configuration, ILogger logger) : BaseRepository<TEntity>(configuration, logger),
	ICrudRepository<TEntity, TBase> where TEntity : IEntity
{
	protected FilterDefinitionBuilder<TEntity> Filter = Builders<TEntity>.Filter;
	protected UpdateDefinitionBuilder<TEntity> Update = Builders<TEntity>.Update;

	public async Task<TEntity[]> Add(IReadOnlyCollection<TBase> bases)
	{
		using var logger = LogRepository($"{Log.F(bases)}", autoExit: false);

		var entities = bases!.Adapt<TEntity[]>();

		await EntityCollection.InsertManyAsync(entities, new InsertManyOptions { IsOrdered = false });

		logger.Exit($"Ids={entities.Select(e => e.Id)}");

		return entities;
	}

	public async Task<TEntity> Replace(ObjectId id, TBase @base)
	{
		using var logger = LogRepository($"{Log.F(id)} {Log.F(@base)}", autoExit: false);

		var entity = @base!.Adapt<TEntity>();

		entity.Id = id;

		var filter = Filter.Eq(e => e.Id, id);

		await EntityCollection.ReplaceOneAsync(filter, entity);

		return entity;
	}

	public async Task<List<TEntity>> GetAll()
	{
		using var logger = LogRepository(autoExit: false);

		var entities = await EntityCollection.AsQueryable().ToListAsync();

		logger.Exit($"{Log.F(entities.Count)}");

		return entities;
	}

	public async Task Delete(ObjectId id)
	{
		using var logger = LogRepository(autoExit: false);

		var filter = Filter.Eq(e => e.Id, id);

		var result = await EntityCollection.DeleteOneAsync(filter);

		logger.Exit($"{Log.F(result.DeletedCount)}");
	}

	public async Task<TEntity?> GetById(ObjectId id)
	{
		using var logger = LogRepository(autoExit: false);

		var filter = Filter.Eq(e => e.Id, id);

		var found = await EntityCollection.Find(filter).FirstOrDefaultAsync();

		logger.Exit($"{Log.F(found is not null)}");

		return found;
	}
}