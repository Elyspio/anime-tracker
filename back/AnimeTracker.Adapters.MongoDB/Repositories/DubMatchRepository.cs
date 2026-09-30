using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Adapters.MongoDB.Repositories.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories;

internal class DubMatchRepository(IMongoDatabase database, ILogger<DubMatchRepository> logger)
	: BaseRepository<DubMatchEntity>(database, logger), IDubMatchRepository
{
	private static readonly FilterDefinitionBuilder<DubMatchEntity> Filter = Builders<DubMatchEntity>.Filter;

	/// <summary>
	///     Set once the unique index exists. One match per anime and platform is enforced by the store:
	///     an admin applying an override while the nightly sync reaches the same anime would otherwise
	///     upsert it twice. Not a Lazy: a failed attempt has to be retried, not cached until a restart.
	/// </summary>
	private volatile bool _indexed;

	public async Task<List<DubMatchEntity>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(date)}");

		return await EntityCollection
			.Find(Filter.Where(match => match.Date.Year == date.Year && match.Date.Season == date.Season))
			.ToListAsync(cancellationToken);
	}

	public async Task Save(DubMatchBase match, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(match.SourceId)} {Log.F(match.Platform)} {Log.F(match.Status)}");

		await EnsureIndex(cancellationToken);

		await EntityCollection.ReplaceOneAsync(
			Key(match.SourceId, match.Platform),
			match.Adapt<DubMatchEntity>(),
			new ReplaceOptions { IsUpsert = true },
			cancellationToken);
	}

	public async Task Delete(int sourceId, DubPlatform platform, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(sourceId)} {Log.F(platform)}");

		await EntityCollection.DeleteOneAsync(Key(sourceId, platform), cancellationToken);
	}

	public async Task<long> DeleteAllBut(AnimeDate date, IReadOnlyCollection<int> sourceIds, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(date)} {Log.F(sourceIds.Count)}");

		var result = await EntityCollection.DeleteManyAsync(Filter.And(
			Filter.Eq(match => match.Date.Year, date.Year),
			Filter.Eq(match => match.Date.Season, date.Season),
			Filter.Nin(match => match.SourceId, sourceIds)), cancellationToken);

		return result.DeletedCount;
	}

	private static FilterDefinition<DubMatchEntity> Key(int sourceId, DubPlatform platform)
	{
		return Filter.And(Filter.Eq(match => match.SourceId, sourceId), Filter.Eq(match => match.Platform, platform));
	}

	private async Task EnsureIndex(CancellationToken cancellationToken)
	{
		if (_indexed) return;

		await CreateIndexIfMissing([nameof(DubMatchBase.SourceId), nameof(DubMatchBase.Platform)], true, cancellationToken);
		_indexed = true;
	}
}
