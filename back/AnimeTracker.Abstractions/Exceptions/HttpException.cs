using System.Net;
using Elyspio.Utils.Telemetry.Technical.Helpers;

namespace AnimeTracker.Api.Abstractions.Exceptions;

/// <summary>
///     Defines a specialized exception type for HTTP errors.
/// </summary>
public class HttpException : Exception
{
	/// <summary>
	///     Initializes a new instance of the HttpException class with a specific HTTP status code, error message and a
	///     reference to the inner exception causing this exception.
	/// </summary>
	/// <param name="code">The HTTP status code.</param>
	/// <param name="message">The error message. This message is used for the Exception.Message property.</param>
	/// <param name="innerException">
	///     The exception that is the cause of the current exception, or a null reference if no inner
	///     exception is specified.
	/// </param>
	public HttpException(HttpStatusCode code, string? message, Exception? innerException) : base(message, innerException)
	{
		Code = code;
	}

	/// <summary>
	///     Initializes a new instance of the HttpException class with a specific HTTP status code and error message.
	/// </summary>
	/// <param name="code">The HTTP status code.</param>
	/// <param name="message">The error message. This message is used for the Exception.Message property.</param>
	public HttpException(HttpStatusCode code, string? message) : base(message)
	{
		Code = code;
	}

	/// <summary>
	///     Gets the HTTP status code associated with this exception.
	/// </summary>
	public HttpStatusCode Code { get; }


	/// <summary>
	///     This represents a HTTP 404 (Not Found) exception specifically for entities or resources of type <typeparamref name="T" />.
	///     It is used when the specified entity or resource cannot be found.
	/// </summary>
	/// <typeparam name="T">The type of the entity or resource which could not be found.</typeparam>
	public sealed class NotFound<T>(Guid id) : HttpException(HttpStatusCode.NotFound, $"Could not find {typeof(T).Name} with {Log.F(id)}");
}