namespace AnimeTracker.Api.Abstractions.Models.Base.Anime;

public class Anime
{
	public required AnimeDate Date { get; set; }
	public required string Title { get; set; }
	public required string Studio { get; set; }
	public required string Description { get; set; }
	public required string ImageUrl { get; set; }
	public required string Url { get; set; }
	public required IReadOnlyCollection<AnimeTag>  Tags { get; set; }
	public required IReadOnlyCollection<Episode> Episodes { get; set; }
	public required int? EpisodesCount { get; set; }
}