namespace AnimeTracker.Api.Abstractions.Models.Base.Anime;

public sealed record Episode
{
	public int Number { get; set; }
	public required string Title { get; set; }
	public required string Url { get; set; }
}