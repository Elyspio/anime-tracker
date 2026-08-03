using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories.Base;

/// <inheritdoc cref="ICrudRepository{TEntity,TBase}" />
internal abstract class CrudRepository<TEntity, TBase>(IMongoDatabase database, ILogger logger)
	: BaseRepository<TEntity>(database, logger), ICrudRepository<TEntity, TBase> where TEntity : IEntity
{
	protected readonly FilterDefinitionBuilder<TEntity> Filter = Builders<TEntity>.Filter;
	protected readonly UpdateDefinitionBuilder<TEntity> Update = Builders<TEntity>.Update;

	public async Task<TEntity[]> Add(IReadOnlyCollection<TBase> bases, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(bases.Count)}");

		var entities = bases.Adapt<TEntity[]>();

		await EntityCollection.InsertManyAsync(entities, new InsertManyOptions { IsOrdered = false }, cancellationToken);

		return entities;
	}

	public async Task<TEntity> Replace(ObjectId id, TBase @base, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(id)}");

		var entity = @base!.Adapt<TEntity>();
		entity.Id = id;

		await EntityCollection.ReplaceOneAsync(Filter.Eq(e => e.Id, id), entity, cancellationToken: cancellationToken);

		return entity;
	}

	public async Task<List<TEntity>> GetAll(CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository();

		return await EntityCollection.Find(Filter.Empty).ToListAsync(cancellationToken);
	}

	public async Task Delete(ObjectId id, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(id)}");

		await EntityCollection.DeleteOneAsync(Filter.Eq(e => e.Id, id), cancellationToken);
	}

	public async Task<TEntity?> GetById(ObjectId id, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(id)}");

		return await EntityCollection.Find(Filter.Eq(e => e.Id, id)).FirstOrDefaultAsync(cancellationToken);
	}
}
