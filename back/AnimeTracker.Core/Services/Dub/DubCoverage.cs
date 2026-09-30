using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Core.Services.Dub;

/// <summary>
///     Whether the French dub has caught up, platform by platform. Pure and computed on every read,
///     like the binge prediction: it depends on today's date, and a stored verdict would be wrong the
///     morning the next episode airs in Japan.
/// </summary>
public static class DubCoverage
{
	/// <summary>
	///     Every matched platform, best first: complete, then up to date, then the most French episodes.
	///     A platform matched with no French episode is still returned — its page is where the anime is
	///     — and a platform never matched is not: that dub is unknown, not missing.
	/// </summary>
	public static DubAvailability[] Evaluate(IReadOnlyCollection<Episode> schedule, int? totalEpisodes,
		IEnumerable<DubMatchBase> matches, DateOnly today)
	{
		var released = schedule.Where(episode => episode.ReleaseDate <= today).Select(episode => episode.Number).ToHashSet();

		return matches
			.Where(match => match is { Status: DubMatchStatus.Matched, Url: not null })
			.Select(match => Evaluate(match, released, totalEpisodes))
			.OrderByDescending(dub => dub.Complete)
			.ThenByDescending(dub => dub.UpToDate)
			.ThenByDescending(dub => dub.FrenchEpisodes)
			.ThenBy(dub => dub.Platform)
			.ToArray();
	}

	private static DubAvailability Evaluate(DubMatchBase match, HashSet<int> released, int? totalEpisodes)
	{
		var french = match.FrenchEpisodes.ToHashSet();

		return new DubAvailability
		{
			Platform = match.Platform,
			Url = match.Url!,
			FrenchEpisodes = totalEpisodes is { } total ? french.Count(number => number >= 1 && number <= total) : french.Count,
			TotalEpisodes = totalEpisodes,
			// Sets, never counts: French audio on 1, 2 and 4 is not "up to date" after three episodes.
			// Nothing aired yet would make "every aired episode is dubbed" vacuously true.
			UpToDate = released.Count > 0 && released.IsSubsetOf(french),
			// On the announced total, not on the prediction's status: a known total with no published
			// slot is UnknownEnd there, and can still be dubbed from the first episode to the last.
			Complete = totalEpisodes is { } announced && announced > 0 && Enumerable.Range(1, announced).All(french.Contains),
			CheckedAt = match.CheckedAt
		};
	}
}
