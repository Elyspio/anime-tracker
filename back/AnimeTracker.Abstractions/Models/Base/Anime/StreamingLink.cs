namespace AnimeTracker.Abstractions.Models.Base.Anime;

/// <summary>
///     Where an anime can be watched, as the source lists it. The link is the source's own, not one
///     the product resolved, so it points at whatever page the source's contributors chose.
/// </summary>
/// <param name="Site">The platform's name as the source spells it — "Crunchyroll", "Netflix".</param>
/// <param name="Url">An absolute http(s) link to a page of the platform, never its bare home page.</param>
public sealed record StreamingLink(string Site, string Url);
