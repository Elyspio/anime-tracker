namespace AnimeTracker.Abstractions.Models.Base.Anime;

/// <summary>
///     An anime as the source publishes it. There is no lighter variant: the season query returns
///     every field at once, so nothing is ever half-populated waiting for a second fetch.
/// </summary>
public class AnimeBase
{
	/// <summary>
	///     The source's own identifier, and this anime's identity everywhere. Stable across renames
	///     and re-scrapes, unlike a title or a URL.
	/// </summary>
	public required int SourceId { get; set; }

	public required AnimeDate Date { get; set; }

	public required string Title { get; set; }

	/// <summary>
	///     Every other name the source knows the anime by — English, native, community synonyms — for
	///     search only. Not required: documents stored before the field existed read back empty.
	/// </summary>
	public IReadOnlyCollection<string> AlternativeTitles { get; set; } = [];

	public required string Description { get; set; }

	public required string Studio { get; set; }

	public required string ImageUrl { get; set; }

	/// <summary>The anime's page on the source, for the card to link out to.</summary>
	public required string Url { get; set; }

	public required AnimeFormat Format { get; set; }

	/// <summary>Filtered out of the grid unless explicitly asked for.</summary>
	public required bool IsAdult { get; set; }

	/// <summary>Average grade out of 10. Null until somebody has graded it.</summary>
	public required double? Score { get; set; }

	/// <summary>Members who have the anime in a list, whether or not they graded it.</summary>
	public required int Popularity { get; set; }

	/// <summary>How many members graded it. This is what says whether <see cref="Score" /> means anything.</summary>
	public required int? VotesCount { get; set; }

	/// <summary>Total episodes announced. Null when the source does not say, which forces UnknownEnd.</summary>
	public required int? EpisodesCount { get; set; }

	public required IReadOnlyCollection<string> Genres { get; set; }

	/// <summary>The airing schedule: aired and scheduled episodes together, ordered by number.</summary>
	public required IReadOnlyCollection<Episode> Episodes { get; set; }
}
