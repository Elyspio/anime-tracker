namespace AnimeTracker.Abstractions.Models.Transports;

/// <summary>
///     Outcome of asking for a season refresh: either the run this call created, or the one that was
///     already walking that season. Answering with the existing run lets the caller follow it instead
///     of queuing a second identical walk.
/// </summary>
public sealed record RefreshQueueResult(bool AlreadyRunning, RefreshRun Run);
