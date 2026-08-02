using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories.Base;

/// <summary>Manages one entity type's collection.</summary>
/// <typeparam name="T">Entity implementation.</typeparam>
public abstract class BaseRepository<T> : TracingRepository
{
	private readonly IMongoDatabase _database;
	private readonly string _collectionName;

	protected BaseRepository(IMongoDatabase database, ILogger logger) : base(logger)
	{
		_database = database;
		_collectionName = typeof(T).Name[..^"Entity".Length];
	}

	protected IMongoCollection<T> EntityCollection => _database.GetCollection<T>(_collectionName);

	protected async Task CreateIndexIfMissing(ICollection<string> properties, bool unique = false, CancellationToken cancellationToken = default)
	{
		var indexName = string.Join("-", properties);

		var cursor = await EntityCollection.Indexes.ListAsync(cancellationToken);
		var indexes = await cursor.ToListAsync(cancellationToken);
		if (indexes.Any(index => index["name"].AsString == indexName)) return;

		var keys = Builders<T>.IndexKeys;
		var definition = keys.Combine(properties.Select(property => keys.Ascending(property)));

		await EntityCollection.Indexes.CreateOneAsync(
			new CreateIndexModel<T>(definition, new CreateIndexOptions { Unique = unique, Name = indexName }),
			cancellationToken: cancellationToken);
	}
}
