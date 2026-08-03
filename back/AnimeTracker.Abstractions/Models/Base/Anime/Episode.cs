namespace AnimeTracker.Abstractions.Models.Base.Anime;

/// <summary>
///     One entry of an anime's airing schedule. The list mixes episodes that have aired with
///     episodes that are only scheduled — the source publishes both, and telling them apart is the
///     prediction's job, not the model's.
/// </summary>
/// <param name="Number">Episode number, 1-based.</param>
/// <param name="ReleaseDate">
///     The day it airs. Always known: the schedule is a list of dated slots, so an entry without a
///     date would be an entry the source never published.
/// </param>
public sealed record Episode(int Number, DateOnly ReleaseDate);
