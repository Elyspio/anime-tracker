using System.Linq.Expressions;
using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Hangfire;
using Hangfire.Annotations;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Api.Adapters.Hangfire.Adapters;

public class HangfireHangfireJobAdapter : TracingAdapter, IHangfireJobAdapter
{
	public HangfireHangfireJobAdapter(ILogger<HangfireHangfireJobAdapter> logger) : base(logger)
	{
	}


	public async Task Schedule(string id, Expression<Func<Task>> methodCall, string cron, TimeSpan? delay = null)
	{
		await Task.Delay(delay ?? TimeSpan.Zero);

		RecurringJob.AddOrUpdate(id, methodCall, cron);
	}

	public void Clear(string id)
	{
		RecurringJob.RemoveIfExists(id);
	}
}