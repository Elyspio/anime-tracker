using AnimeTracker.Abstractions.Models.Base.Studios;

namespace AnimeTracker.Abstractions.Models.Transports;

/// <summary>
///     A studio's filmography, newest first: the works nobody has dated yet, then by start date. Music
///     videos are left out.
/// </summary>
public class StudioFilmography : StudioFilmographyBase
{
	/// <summary>When the source was last asked — the filmography is as old as this says.</summary>
	public required DateTimeOffset FetchedAt { get; init; }
}
