using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Transports;

/// <summary>Acknowledgement that a season refresh was queued. Its progress is the scheduler's.</summary>
public sealed record RefreshQueued(string JobId, AnimeDate Date);
