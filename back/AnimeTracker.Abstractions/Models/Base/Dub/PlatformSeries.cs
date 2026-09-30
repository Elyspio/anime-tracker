namespace AnimeTracker.Abstractions.Models.Base.Dub;

/// <summary>A search hit: enough to decide whether the series is worth opening.</summary>
/// <param name="AlternativeTitles">Any other title the platform gives it, such as ADN's romaji original title.</param>
public sealed record PlatformSeriesSummary(string Id, string Title, IReadOnlyCollection<string> AlternativeTitles);

/// <summary>A series as the platform groups it: often every season of a franchise under one id.</summary>
/// <param name="Url">The series page, for a reader to open.</param>
/// <param name="Seasons">In any order; <see cref="PlatformSeason.Order" /> says which came first.</param>
public sealed record PlatformSeries(string Id, string Title, string Url, IReadOnlyList<PlatformSeason> Seasons);

/// <param name="Order">Chronological position within the series, 1 for the first season.</param>
/// <param name="Episodes">
///     Null when the platform lists episodes separately; the caller then asks for them, one season at
///     a time, and only for the seasons it needs.
/// </param>
public sealed record PlatformSeason(string Id, int Order, IReadOnlyList<PlatformEpisode>? Episodes);

/// <param name="Number">
///     The platform's episode number within the season. Fractional for a recap slotted between two
///     episodes, null for a special — neither is ever aligned with an AniList episode.
/// </param>
/// <param name="AirDate">The day it was first released, when the platform says.</param>
/// <param name="French">Whether it can be watched with French audio.</param>
/// <param name="ReleaseDate">
///     The day the platform put it online, when it says. A second chance at lining it up: Crunchyroll's
///     broadcast date is sometimes plainly wrong (a first episode dated a year early), its release date
///     much less often.
/// </param>
public sealed record PlatformEpisode(double? Number, DateOnly? AirDate, bool French, DateOnly? ReleaseDate = null);
