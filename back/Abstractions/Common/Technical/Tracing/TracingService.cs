using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Example.Api.Abstractions.Common.Helpers;
using Example.Api.Abstractions.Common.Technical.Tracing.Base;
using Microsoft.Extensions.Logging;

namespace Example.Api.Abstractions.Common.Technical.Tracing;

/// <summary>
///     Tracing context for Services and Adapters
/// </summary>
public class TracingService : ITracingContext
{
	private static readonly ConcurrentDictionary<string, ActivitySource> Sources = new();
	private readonly ILogger _logger;
	private readonly string _sourceName;


	/// <summary>
	///     Default constructor
	/// </summary>
	/// <param name="logger"></param>
	protected TracingService(ILogger logger)
	{
		_logger = logger;
		_sourceName = GetType().Name;
		TracingContext.AddSource(_sourceName);
	}


	private ActivitySource ActivitySource => TracingContext.GetActivitySource(_sourceName);


	/// <summary>
	///     Create a logger instance for a specific call
	/// </summary>
	/// <param name="arguments"></param>
	/// <param name="method">name of the method (auto)</param>
	/// <param name="fullFilePath">filename of the method (auto)</param>
	/// <param name="autoExit">Pass false to handle <see cref="Log.LoggerInstance.Exit" /> yourself</param>
	/// <returns></returns>
	protected Log.LoggerInstance LogService(string arguments = "", [CallerMemberName] string method = "", [CallerFilePath] string fullFilePath = "", bool autoExit = true)
	{
		method = TracingContext.GetMethodName(method);

		var className = Log.GetClassNameFromFilepath(fullFilePath);

		var activity = ActivitySource.CreateActivity(className, method, arguments);

		return _logger.Enter(arguments, LogLevel.Debug, activity, method, autoExit, className);
	}
}