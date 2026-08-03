using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Adapters.MongoDB.Repositories.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Repositories;

internal class RefreshRunRepository(IMongoDatabase database, ILogger<RefreshRunRepository> logger)
	: BaseRepository<RefreshRunEntity>(database, logger), IRefreshRunRepository
{
	private static readonly FilterDefinitionBuilder<RefreshRunEntity> Filter = Builders<RefreshRunEntity>.Filter;

	private static readonly UpdateDefinitionBuilder<RefreshRunEntity> Update = Builders<RefreshRunEntity>.Update;

	public async Task<RefreshRunEntity> Queue(Guid runId, AnimeDate date, DateTimeOffset now, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(runId)} {Log.F(date)}");

		var entity = new RefreshRunEntity
		{
			RunId = runId,
			Date = date,
			Status = RefreshStatus.Queued,
			Total = 0,
			StartedAt = now,
			UpdatedAt = now,
			FinishedAt = null,
			Error = null
		};

		await EntityCollection.InsertOneAsync(entity, cancellationToken: cancellationToken);

		return entity;
	}

	public async Task Begin(Guid runId, AnimeDate date, DateTimeOffset now, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(runId)}");

		var update = Update
			.Set(run => run.Status, RefreshStatus.Running)
			.Set(run => run.UpdatedAt, now)
			.SetOnInsert(run => run.RunId, runId)
			.SetOnInsert(run => run.Date, date)
			.SetOnInsert(run => run.Total, 0)
			.SetOnInsert(run => run.StartedAt, now)
			.SetOnInsert(run => run.FinishedAt, (DateTimeOffset?)null)
			.SetOnInsert(run => run.Error, (string?)null);

		await EntityCollection.UpdateOneAsync(
			Filter.Eq(run => run.RunId, runId),
			update,
			new UpdateOptions { IsUpsert = true },
			cancellationToken);
	}

	public async Task Finish(Guid runId, RefreshStatus status, int total, string? error, DateTimeOffset now,
		CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(runId)} {Log.F(status)}");

		await EntityCollection.UpdateOneAsync(
			Filter.Eq(run => run.RunId, runId),
			Update
				.Set(run => run.Status, status)
				.Set(run => run.Total, total)
				.Set(run => run.Error, error)
				.Set(run => run.UpdatedAt, now)
				.Set(run => run.FinishedAt, now),
			cancellationToken: cancellationToken);
	}

	public async Task<RefreshRunEntity?> GetActive(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(date)}");

		var filter = Filter.And(
			Filter.In(run => run.Status, new[] { RefreshStatus.Queued, RefreshStatus.Running }),
			Filter.Eq(run => run.Date.Year, date.Year),
			Filter.Eq(run => run.Date.Season, date.Season));

		return await EntityCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<List<RefreshRunEntity>> GetRecent(int limit, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository($"{Log.F(limit)}");

		return await EntityCollection
			.Find(Filter.Empty)
			.SortByDescending(run => run.StartedAt)
			.Limit(limit)
			.ToListAsync(cancellationToken);
	}

	public async Task<long> MarkRunningAsInterrupted(DateTimeOffset now, CancellationToken cancellationToken = default)
	{
		using var trace = LogRepository();

		// Running only: a queued job is still sitting in Hangfire's own storage and will be picked up
		// after the restart, so closing it here would contradict what is about to happen.
		var result = await EntityCollection.UpdateManyAsync(
			Filter.Eq(run => run.Status, RefreshStatus.Running),
			Update
				.Set(run => run.Status, RefreshStatus.Interrupted)
				.Set(run => run.Error, "The process stopped while this refresh was running.")
				.Set(run => run.UpdatedAt, now)
				.Set(run => run.FinishedAt, now),
			cancellationToken: cancellationToken);

		return result.ModifiedCount;
	}
}
