using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace AnimeTracker.Api.Abstractions.Interfaces.Adapters;

public interface IHangfireJobAdapter
{
	/// <summary>
	/// Schedule a recurring job
	/// </summary>
	/// <param name="id"></param>
	/// <param name="methodCall"></param>
	/// <param name="cron"></param>
	/// <param name="delay"></param>
	/// <returns></returns>
	Task Schedule(string id, Expression<Func<Task>> methodCall, string cron, TimeSpan? delay = null);

	void Clear(string id);
}