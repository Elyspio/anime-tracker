using System.Linq.Expressions;

namespace AnimeTracker.Abstractions.Interfaces.Adapters;

public interface IHangfireJobAdapter
{
	/// <summary>Registers or updates a recurring job.</summary>
	Task Schedule(string id, Expression<Func<Task>> methodCall, string cron);

	void Clear(string id);
}
