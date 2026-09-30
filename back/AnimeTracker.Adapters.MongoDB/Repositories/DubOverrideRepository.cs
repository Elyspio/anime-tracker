using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Adapters.MongoDB.Repositories.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories;

internal class DubOverrideRepository(IMongoDatabase database, ILogger<DubOverrideRepository> logger)
	: BaseRepository<DubOverrideEntity>(database, logger), IDubOverrideRepository
{
	private static readonly FilterDefinitionBuilder<DubOverrideEntity> Filter = Builders<DubOverrideEntity>.Filter;

	/// <summary>Set once the unique index exists; see <see cref="DubMatchRepository" /> for why it is not a Lazy.</summary>
	private volatile bool _indexed;

	public async Task<List<DubOverrideEntity>> GetBySourceIds(IReadOnlyCollection<int> sourceIds, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(sourceIds.Count)}");

		return await EntityCollection.Find(Filter.In(@override => @override.SourceId, sourceIds)).ToListAsync(cancellationToken);
	}

	public async Task Save(DubOverrideBase @override, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(@override.SourceId)} {Log.F(@override.Platform)} {Log.F(@override.Mode)}");

		await EnsureIndex(cancellationToken);

		await EntityCollection.ReplaceOneAsync(
			Key(@override.SourceId, @override.Platform),
			@override.Adapt<DubOverrideEntity>(),
			new ReplaceOptions { IsUpsert = true },
			cancellationToken);
	}

	public async Task Delete(int sourceId, DubPlatform platform, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(sourceId)} {Log.F(platform)}");

		await EntityCollection.DeleteOneAsync(Key(sourceId, platform), cancellationToken);
	}

	private static FilterDefinition<DubOverrideEntity> Key(int sourceId, DubPlatform platform)
	{
		return Filter.And(Filter.Eq(@override => @override.SourceId, sourceId), Filter.Eq(@override => @override.Platform, platform));
	}

	private async Task EnsureIndex(CancellationToken cancellationToken)
	{
		if (_indexed) return;

		await CreateIndexIfMissing([nameof(DubOverrideBase.SourceId), nameof(DubOverrideBase.Platform)], true, cancellationToken);
		_indexed = true;
	}
}
