namespace AnimeTracker.Api.Abstractions.Models.Base.Anime;

public class AnimeBase : AnimeLight
{
	public required IReadOnlyCollection<AnimeTag>  Tags { get; set; }
	public required IReadOnlyCollection<Episode> Episodes { get; set; }
}