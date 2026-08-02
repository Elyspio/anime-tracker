namespace AnimeTracker.Api.Abstractions.Models.Base.Anime;

public class AnimeLight
{
	public required AnimeDate Date { get; set; }
	public required double? Score { get; set; }
	public required string Title { get; set; }
	public required string Studio { get; set; }
	public required string Description { get; set; }
	public required int Popularity { get; set; }
	public required string ImageUrl { get; set; }
	public required string Url { get; set; }
	public required int? EpisodesCount { get; set; }
}