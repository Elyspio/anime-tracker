using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Base.Studios;

/// <summary>
///     One entry of a studio's filmography. Deliberately lighter than <see cref="AnimeBase" />: no
///     airing schedule, so no countdown. The countdown lives on the season the work belongs to, which
///     is what <see cref="Season" /> points at.
/// </summary>
public class StudioWork
{
	/// <summary>The AniList id of the work — the same identity an anime of a season has.</summary>
	public required int SourceId { get; set; }

	public required string Title { get; set; }

	public required string ImageUrl { get; set; }

	/// <summary>The work's page on the source.</summary>
	public required string Url { get; set; }

	public required AnimeFormat Format { get; set; }

	/// <summary>Hidden from the filmography unless explicitly asked for, like in a season.</summary>
	public required bool IsAdult { get; set; }

	/// <summary>Average grade out of 10. Null until somebody has graded it.</summary>
	public required double? Score { get; set; }

	public required int? EpisodesCount { get; set; }

	/// <summary>Null for a work that was announced without a date.</summary>
	public required StudioWorkDate? StartDate { get; set; }

	/// <summary>
	///     The season the work is listed under, when there is one to name: the source's own, or the one
	///     its start month falls in. Null when neither is known.
	/// </summary>
	public required AnimeDate? Season { get; set; }
}
