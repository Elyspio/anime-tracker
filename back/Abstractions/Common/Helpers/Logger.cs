using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Example.Api.Abstractions.Common.Helpers;

/// <summary>
///     Provides utility methods for logging.
/// </summary>
public static class Log
{
	/// <summary>
	///     Custom JSON serializer options with enum as string.
	/// </summary>
	private static readonly JsonSerializerOptions Options = new()
	{
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};

	/// <summary>
	///     Formats a named value as a JSON string.
	/// </summary>
	public static string F(object? value, [CallerArgumentExpression("value")] string name = "")
	{
		return $"{name}={JsonSerializer.Serialize(value, Options)}";
	}

	/// <summary>
	///     Creates a new instance of the LoggerInstance class for method logging.
	/// </summary>
	public static LoggerInstance Enter(this ILogger logger, string arguments = "", LogLevel level = LogLevel.Debug, Activity? activity = null,
		[CallerMemberName] string method = "", bool autoExit = true, string className = "")
	{
		return new LoggerInstance(logger, method, arguments, level, activity, autoExit, className);
	}

	/// <summary>
	///     Extracts class name from the provided filepath.
	/// </summary>
	public static string GetClassNameFromFilepath(string fullFilePath)
	{
		// On récupère le nom du fichier
		var filePath = fullFilePath[fullFilePath.LastIndexOf(Path.DirectorySeparatorChar)..];

		// On supprime le premier / et l'extension
		return filePath[1..^3];
	}


	/// <summary>
	///     Encapsulates a logging session, providing methods to record specific events and messages.
	/// </summary>
	public sealed class LoggerInstance : IDisposable
	{
		private readonly string? _arguments;
		private readonly bool _autoExit;
		private readonly string _className;
		private readonly LogLevel _level;
		private readonly ILogger _logger;
		private readonly string _method;
		private readonly long _startedAt = Stopwatch.GetTimestamp();
		private readonly string? _traceId;

		/// <summary>
		///     Creates a new instance of the LoggerInstance class.
		/// </summary>
		/// <param name="logger">The logger to be used.</param>
		/// <param name="method">The method to be logged.</param>
		/// <param name="arguments">The arguments to be logged.</param>
		/// <param name="level">The level of log to be captured.</param>
		/// <param name="activity">Optional activity to correlate logger messages.</param>
		/// <param name="autoExit">True if the log must automatically exit when disposed.</param>
		/// <param name="className">The class name where the logger is used.</param>
		public LoggerInstance(ILogger logger, string method, string? arguments, LogLevel level, Activity? activity = null, bool autoExit = true, string className = "")
		{
			_level = level;
			Activity = activity;
			_autoExit = autoExit;
			_method = method;
			_logger = logger;
			_traceId = activity?.RootId ?? activity?.Id;
			_className = className;

			if (!string.IsNullOrWhiteSpace(_arguments)) _arguments = arguments;

			Enter();
		}

		/// <summary>
		///     Current activity
		/// </summary>
		public Activity? Activity { get; }


		/// <summary>
		///     Releases associated resources and call <see cref="Exit" /> if <see cref="_autoExit" /> is enabled
		/// </summary>
		public void Dispose()
		{
			if (_autoExit) Exit();
			Activity?.Dispose();
			GC.SuppressFinalize(this);
		}


		/// <summary>
		///     Logs an "entering method" message.
		/// </summary>
		private void Enter()
		{
			if (!_logger.IsEnabled(_level)) return;
			var sb = new StringBuilder($"{_traceId} {_className}.{_method} -- Enter");
			if (!string.IsNullOrWhiteSpace(_arguments)) sb.Append($" -- {_arguments}");

			_logger.Log(_level, sb.ToString());
		}


		/// <summary>
		///     Logs an "exiting method" message.
		/// </summary>
		public void Exit(string? content = null)
		{
			if (!_logger.IsEnabled(_level)) return;
			var sb = new StringBuilder($"{_traceId} {_className}.{_method} -- Exit");
			if (_arguments?.Length > 0) sb.Append($" -- {_arguments}");

			if (!string.IsNullOrWhiteSpace(content)) sb.Append($" -- {content}");

			sb.Append($" -- {Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds}ms");

			_logger.Log(_level, sb.ToString());
		}

		/// <summary>
		///     Logs an error message.
		/// </summary>
		public void Error(string content)
		{
			var sb = new StringBuilder($"{_traceId} {_className}.{_method} -- {content}");
			_logger.LogError(sb.ToString());
		}

		/// <summary>
		///     Logs a warning message.
		/// </summary>
		public void Warn(string content)
		{
			var sb = new StringBuilder($"{_traceId} {_className}.{_method} -- {content}");
			_logger.LogWarning(sb.ToString());
		}


		/// <summary>
		///     Logs a debug message.
		/// </summary>
		public void Debug(string s)
		{
			if (!_logger.IsEnabled(LogLevel.Debug)) return;
			var sb = new StringBuilder($"{_traceId} {_className}.{_method}");
			if (_arguments?.Length > 0) sb.Append($" -- {_arguments}");
			sb.Append($" -- {s}");

			_logger.LogDebug(sb.ToString());
		}
	}
}