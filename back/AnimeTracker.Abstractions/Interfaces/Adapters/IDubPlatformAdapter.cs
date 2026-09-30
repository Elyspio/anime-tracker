using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Models.Base.Dub;

namespace AnimeTracker.Abstractions.Interfaces.Adapters;

/// <summary>
///     One streaming platform, read for its French audio. It knows the platform's schema and nothing
///     of AniList: deciding which series is which anime is the caller's job. Every network failure is
///     reported as <see cref="DubPlatformUnavailableException" />, so that no transport error can ever
///     read as an empty catalogue.
/// </summary>
public interface IDubPlatformAdapter
{
	DubPlatform Platform { get; }

	/// <summary>Whether the address is on this platform's own domain. No request.</summary>
	bool Owns(string url);

	/// <summary>The series id when the address is one of this platform's series pages, null otherwise. No request.</summary>
	string? ParseSeriesId(string url);

	/// <summary>Series whose title the platform's own search associates with the query.</summary>
	Task<IReadOnlyList<PlatformSeriesSummary>> Search(string query, CancellationToken cancellationToken = default);

	/// <summary>The series and its seasons, or null when the platform does not know the id.</summary>
	Task<PlatformSeries?> GetSeries(string seriesId, CancellationToken cancellationToken = default);

	/// <summary>The episodes of a season that <see cref="GetSeries" /> returned without them.</summary>
	Task<IReadOnlyList<PlatformEpisode>> GetEpisodes(string seriesId, string seasonId, CancellationToken cancellationToken = default);
}
