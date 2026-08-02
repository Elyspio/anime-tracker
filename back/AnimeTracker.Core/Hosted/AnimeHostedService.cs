using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Core.Services;
using Microsoft.Extensions.Hosting;

namespace AnimeTracker.Core.Hosted;

/// <summary>Registers the daily season refresh with the scheduler. Exempt from tracing: no behaviour of its own.</summary>
public class AnimeHostedService(IHangfireJobAdapter hangfireJobAdapter, AnimeRefreshJob refreshJob) : IHostedService
{
	private const string DailyAtNoon = "0 12 * * *";

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await hangfireJobAdapter.Schedule(AnimeRefreshJob.RecurringJobId, () => refreshJob.RefreshCurrentSeason(), DailyAtNoon);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		hangfireJobAdapter.Clear(AnimeRefreshJob.RecurringJobId);
		return Task.CompletedTask;
	}
}
