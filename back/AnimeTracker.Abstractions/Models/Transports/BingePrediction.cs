namespace AnimeTracker.Abstractions.Models.Transports;

public enum BingeStatus
{
	/// <summary>Every announced episode has been released — watch it now.</summary>
	BingeableNow,

	/// <summary>The end date is extrapolated from the observed release cadence.</summary>
	Estimated,

	/// <summary>The total episode count was never announced, so no end date is claimed.</summary>
	UnknownEnd
}

/// <summary>
///     When an anime becomes bingeable. Derived from the episode list on every read and never
///     stored: it is a function of the episodes plus today's date, and a stored copy would rot
///     between two scrapes.
/// </summary>
public sealed record BingePrediction
{
	public required BingeStatus Status { get; init; }

	/// <summary>Estimated release date of the last episode. Null when <see cref="Status" /> is UnknownEnd.</summary>
	public required DateOnly? BingeableAt { get; init; }

	public required int ReleasedEpisodes { get; init; }

	public required int? TotalEpisodes { get; init; }
}
