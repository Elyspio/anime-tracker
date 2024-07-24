namespace AnimeTracker.Api.Abstractions.Models.Base.Anime;

public sealed record Episode
{
	public double Number { get; set; }
	public required string Title { get; set; }
	public required string? Url { get; set; }
	public required DateOnly? ReleaseDate { get; set; }
}