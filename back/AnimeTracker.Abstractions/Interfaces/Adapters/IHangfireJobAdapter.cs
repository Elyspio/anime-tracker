using System.Linq.Expressions;

namespace AnimeTracker.Abstractions.Interfaces.Adapters;

public interface IHangfireJobAdapter
{
	/// <summary>Registers or updates a recurring job.</summary>
	Task Schedule(string id, Expression<Func<Task>> methodCall, string cron);

	/// <summary>
	///     Queues a one-off job and returns its id immediately. The target is resolved from the
	///     container when a worker picks it up, so nothing here holds an instance.
	/// </summary>
	string Enqueue<T>(Expression<Func<T, Task>> methodCall) where T : notnull;

	void Clear(string id);
}
