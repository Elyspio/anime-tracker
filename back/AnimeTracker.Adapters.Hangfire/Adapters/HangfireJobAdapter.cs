using System.Linq.Expressions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Adapters.Hangfire.Adapters;

public class HangfireJobAdapter(ILogger<HangfireJobAdapter> logger) : TracingAdapter(logger), IHangfireJobAdapter
{
	public Task Schedule(string id, Expression<Func<Task>> methodCall, string cron)
	{
		using var trace = LogAdapter($"{Log.F(id)} {Log.F(cron)}");

		RecurringJob.AddOrUpdate(id, methodCall, cron);

		return Task.CompletedTask;
	}

	public void Clear(string id)
	{
		using var trace = LogAdapter($"{Log.F(id)}");

		RecurringJob.RemoveIfExists(id);
	}
}
