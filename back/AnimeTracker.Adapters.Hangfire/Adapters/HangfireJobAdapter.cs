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

	public string Enqueue<T>(Expression<Func<T, Task>> methodCall) where T : notnull
	{
		using var trace = LogAdapter($"{Log.F(typeof(T).Name)}");

		return BackgroundJob.Enqueue(methodCall);
	}

	public void Clear(string id)
	{
		using var trace = LogAdapter($"{Log.F(id)}");

		RecurringJob.RemoveIfExists(id);
	}
}
