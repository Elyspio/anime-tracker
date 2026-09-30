using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services.Dub;

/// <summary>The scheduler's entry point for a dub sync, for the same reason <see cref="AnimeRefreshJob" /> is one.</summary>
public class DubSyncJob(IDubService dubService, ILogger<DubSyncJob> logger) : TracingService(logger)
{
	/// <summary>Takes the season apart, like the refresh job: primitives survive in Hangfire's storage.</summary>
	// ReSharper disable once MemberCanBePrivate.Global — called by Hangfire through an expression.
	public async Task SyncSeason(int year, AnimeSeason season, Guid runId)
	{
		using var trace = LogService($"{Log.F(year)} {Log.F(season)} {Log.F(runId)}");

		await dubService.Sync(new AnimeDate(year, season), runId);
	}
}
