using Elyspio.Utils.Results;

namespace AnimeTracker.Abstractions.Errors;

/// <summary>The request cannot be applied as sent (400).</summary>
public sealed record RequestInvalid(string Message) : Invalid("request.invalid", Message);

/// <summary>Nothing is stored or configured under this identifier (404).</summary>
public sealed record ResourceNotFound(string Message) : NotFound("resource.not-found", Message);

/// <summary>The source (AniList) did not answer correctly (502). The message is the source's, never a stack trace.</summary>
public sealed record SourceUnavailable(string Message) : BadGateway("source.unavailable", Message);
