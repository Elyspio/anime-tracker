namespace AnimeTracker.Api.Abstractions.Models.Base;

public class TodoBase
{
	public required string Label { get; init; }

	public required string User { get; init; }

	public required bool Checked { get; set; }
}