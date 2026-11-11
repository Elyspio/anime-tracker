namespace AnimeTracker.Abstractions.Models.Base.Studios;

/// <summary>A studio and everything it was the main studio of, as the source publishes it.</summary>
public class StudioFilmographyBase
{
	/// <summary>The AniList id of the studio, and its identity everywhere. Never its name.</summary>
	public required int StudioId { get; set; }

	public required string Name { get; set; }

	/// <summary>The studio's page on the source.</summary>
	public required string Url { get; set; }

	public required IReadOnlyCollection<StudioWork> Works { get; set; }
}
