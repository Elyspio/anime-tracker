namespace AnimeTracker.Api.Abstractions.Interfaces.Business;

/// <summary>
///     ITransport interface that represents a transport entity with a unique identifier.
/// </summary>
public interface ITransport
{
	/// <summary>
	///     The global unique identifier (GUID) of a transport entity. It can only be set during object initialization.
	/// </summary>
	public Guid Id { get; init; }
}