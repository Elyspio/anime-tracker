using System.Diagnostics;
using System.Runtime.CompilerServices;
using Example.Api.Abstractions.Common.Helpers;
using Example.Api.Abstractions.Common.Technical.Tracing.Base;
using Microsoft.Extensions.Logging;

namespace Example.Api.Abstractions.Common.Technical.Tracing;

/// <inheritdoc cref="Attribute" />
/// with tracing context
public abstract class TracingAttribute : Attribute, ITracingContext
{
	private readonly string _sourceName;


	/// <inheritdoc />
	protected TracingAttribute()
	{
		_sourceName = GetType().Name;
		TracingContext.AddSource(_sourceName);
	}

	/// <summary>
	///     A logger
	/// </summary>
	public abstract ILogger Logger { get; set; }

	private ActivitySource ActivitySource => TracingContext.GetActivitySource(_sourceName);


	/// <summary>
	///     Start a new Activity for this context
	/// </summary>
	/// <param name="arguments"></param>
	/// <param name="method"></param>
	/// <param name="fullFilePath"></param>
	/// <param name="autoExit"></param>
	/// <returns></returns>
	protected Log.LoggerInstance LogAttribute(string arguments = "", [CallerMemberName] string method = "", [CallerFilePath] string fullFilePath = "", bool autoExit = true)
	{
		method = TracingContext.GetMethodName(method);

		var className = Log.GetClassNameFromFilepath(fullFilePath);
		var activity = ActivitySource.CreateActivity(className, method, arguments);
		return Logger.Enter(arguments, LogLevel.Debug, activity, method, autoExit, className);
	}
}