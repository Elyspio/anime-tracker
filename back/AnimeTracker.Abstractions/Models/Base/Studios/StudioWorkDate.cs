namespace AnimeTracker.Abstractions.Models.Base.Studios;

/// <summary>
///     When a work started, as far as the source knows. A work announced for "2027" has a year and
///     nothing else, so the month and the day are each allowed to be missing — rounding them to
///     January the 1st would invent a date nobody published.
/// </summary>
public record StudioWorkDate(int Year, int? Month, int? Day);
