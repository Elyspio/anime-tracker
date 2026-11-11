using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Adapters.MongoDB.Repositories.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories;

internal class StudioFilmographyRepository(IMongoDatabase database, ILogger<StudioFilmographyRepository> logger)
	: BaseRepository<StudioFilmographyEntity>(database, logger), IStudioFilmographyRepository
{
	private static readonly FilterDefinitionBuilder<StudioFilmographyEntity> Filter = Builders<StudioFilmographyEntity>.Filter;

	/// <summary>
	///     Set once the unique index exists. One filmography per studio is enforced by the store, not
	///     only by the service's gate. Not a Lazy: a failed attempt has to be retried, not cached until
	///     a restart.
	/// </summary>
	private volatile bool _indexed;

	public async Task<StudioFilmographyEntity?> Get(int studioId, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(studioId)}");

		return await EntityCollection.Find(Filter.Eq(studio => studio.StudioId, studioId)).FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<StudioFilmographyEntity> Save(StudioFilmographyBase filmography, DateTimeOffset fetchedAt, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(filmography.StudioId)} {Log.F(filmography.Works.Count)}");

		await EnsureIndex(cancellationToken);

		var entity = new StudioFilmographyEntity
		{
			StudioId = filmography.StudioId,
			Name = filmography.Name,
			Url = filmography.Url,
			Works = filmography.Works,
			FetchedAt = fetchedAt
		};

		await EntityCollection.ReplaceOneAsync(
			Filter.Eq(studio => studio.StudioId, filmography.StudioId),
			entity,
			new ReplaceOptions { IsUpsert = true },
			cancellationToken);

		return entity;
	}

	private async Task EnsureIndex(CancellationToken cancellationToken)
	{
		if (_indexed) return;

		await CreateIndexIfMissing([nameof(StudioFilmographyBase.StudioId)], true, cancellationToken);
		_indexed = true;
	}
}
