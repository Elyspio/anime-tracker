using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Core.Services;
using Microsoft.Extensions.Hosting;

namespace AnimeTracker.Core.Hosted;

/// <summary>Registers the daily season refresh with the scheduler. Exempt from tracing: no behaviour of its own.</summary>
public class AnimeHostedService(
	IHangfireJobAdapter hangfireJobAdapter,
	IRefreshRunRepository refreshRunRepository,
	AnimeRefreshJob refreshJob,
	TimeProvider timeProvider
) : IHostedService
{
	private const string DailyAtNoon = "0 12 * * *";

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		// A run marked as running cannot have survived the process that was running it. The
		// deployment runs a single replica, so nothing else can own one either — closing them here is
		// what stops the dashboard from showing yesterday's crash as still in progress.
		await refreshRunRepository.MarkRunningAsInterrupted(timeProvider.GetUtcNow(), cancellationToken);

		await hangfireJobAdapter.Schedule(AnimeRefreshJob.RecurringJobId, () => refreshJob.RefreshCurrentSeason(), DailyAtNoon);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		hangfireJobAdapter.Clear(AnimeRefreshJob.RecurringJobId);
		return Task.CompletedTask;
	}
}
