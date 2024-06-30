using System.Collections.Concurrent;
using System.Diagnostics;

namespace Example.Api.Abstractions.Common.Technical.Tracing.Base;

/// <summary>
///     Tracing context for Services and Adapters
/// </summary>
public static class TracingContext
{
	private static readonly ConcurrentDictionary<string, ActivitySource> Sources = new();


	/// <summary>
	///     Create a new activity from classname, method and arguments
	/// </summary>
	/// <param name="source"></param>
	/// <param name="class"></param>
	/// <param name="method"></param>
	/// <param name="arguments"></param>
	/// <returns></returns>
	public static Activity? CreateActivity(this ActivitySource source, string @class, string method, string? arguments)
	{
		var activity = source.StartActivity($"{@class}.{method}");

		if (activity == null) return activity;

		activity.AddTag("class", @class);
		activity.AddTag("method", method);

		if (!string.IsNullOrWhiteSpace(arguments))
			activity.AddTag("arguments", arguments);

		return activity;
	}


	/// <summary>
	///     Get <see cref="ActivitySource" /> from name
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static ActivitySource GetActivitySource(string name)
	{
		return Sources[name];
	}

	/// <summary>
	///     Create a new <see cref="ActivitySource" />
	/// </summary>
	/// <param name="name"></param>
	public static void AddSource(string name)
	{
		if (!Sources.ContainsKey(name)) Sources.TryAdd(name, new ActivitySource(name));
	}

	/// <summary>
	///     Get a human friendly version of a method name
	/// </summary>
	/// <param name="rawMethodName"></param>
	/// <returns></returns>
	public static string GetMethodName(string rawMethodName)
	{
		if (rawMethodName == ".ctor") rawMethodName = "Constructor";
		return rawMethodName;
	}
}