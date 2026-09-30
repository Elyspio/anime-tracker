using AnimeTracker.Abstractions.Models.Base.Dub;

namespace AnimeTracker.Abstractions.Exceptions;

/// <summary>
///     The platform cannot be read right now: an anti-bot page, a rate limit, an outage, the egress
///     proxy down. Nothing learned from such a reply is true of the catalogue, so the sync stops asking
///     this platform and keeps every measurement it already had — unknown is not "no dub".
/// </summary>
public sealed class DubPlatformUnavailableException(DubPlatform platform, string message, Exception? innerException = null)
	: Exception(message, innerException)
{
	public DubPlatform Platform { get; } = platform;
}
