using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;

namespace AnimeTracker.Core.Services.Dub;

/// <summary>A season whose episodes have been read.</summary>
public sealed record LoadedSeason(PlatformSeason Season, IReadOnlyList<PlatformEpisode> Episodes);

/// <summary>A platform season lined up with an anime, its episodes renumbered the way AniList numbers them.</summary>
/// <param name="Available">AniList episode numbers the platform has.</param>
/// <param name="French">AniList episode numbers the platform has with French audio.</param>
public sealed record DubAlignment(string SeasonId, IReadOnlyCollection<int> Available, IReadOnlyCollection<int> French);

/// <summary>
///     Which platform episode is which AniList episode. A platform groups a franchise its own way — one
///     season per cour, two cours in one season, numbering that restarts or carries on (13, 14… for a
///     second season) — so neither the season count nor the episode count says which season an anime
///     is. The date does: the platform episode that aired with the anime's first episode anchors the
///     season, and the offset between the two numbers renumbers the rest. Anything that does not line
///     up on exactly one season is left unknown rather than guessed.
/// </summary>
public static class DubAligner
{
	/// <summary>
	///     Days either side of the anime's first broadcast within which a platform episode is the same
	///     one. Weekly slots keep the neighbouring episodes seven days away, and a platform that releases
	///     a day late — ADN posts after the Japanese slot — still falls inside.
	/// </summary>
	public const int ToleranceDays = 3;

	/// <summary>The anime's first episode: what a platform season is lined up on.</summary>
	public static Episode? Anchor(IReadOnlyCollection<Episode> schedule)
	{
		return schedule.MinBy(episode => episode.Number);
	}

	/// <summary>
	///     Whether the season started by the end of the anchor's window. Seasons follow each other, so
	///     every older one ended before this one began and cannot hold the anchor: reading stops here.
	/// </summary>
	public static bool StartsBy(IReadOnlyList<PlatformEpisode> episodes, DateOnly anchorDate)
	{
		var first = episodes.Select(episode => episode.AirDate).Where(date => date is not null).Min();

		return first is { } start && start <= anchorDate.AddDays(ToleranceDays);
	}

	public static DubAlignment? Align(Episode anchor, IReadOnlyList<LoadedSeason> seasons)
	{
		// A double premiere puts episodes 1 and 2 on the same day: the lower number is the anchor.
		var hits = seasons
			.Select(season => (season, hit: season.Episodes
				.Where(episode => IsWhole(episode.Number) && (IsNear(episode.AirDate, anchor.ReleaseDate) || IsNear(episode.ReleaseDate, anchor.ReleaseDate)))
				.MinBy(episode => episode.Number)))
			.Where(pair => pair.hit is not null)
			.ToArray();

		// None: the anime is not on this series yet. Several: two seasons claim the same day — a
		// separate dub season, say — and picking one would be a guess.
		if (hits.Length != 1) return null;

		var (aligned, anchorEpisode) = hits[0];
		var offset = (int)anchorEpisode!.Number!.Value - anchor.Number;

		var episodes = aligned.Episodes
			.Where(episode => IsWhole(episode.Number))
			.Select(episode => (number: (int)episode.Number!.Value - offset, episode.French))
			// Below the anchor belongs to the previous cour when the platform keeps counting.
			.Where(episode => episode.number >= anchor.Number)
			.ToArray();

		// Two platform episodes on one AniList number: a split episode or a re-upload. Which of them
		// the dub is on cannot be told, so the whole season stays unknown.
		if (episodes.GroupBy(episode => episode.number).Any(group => group.Count() > 1)) return null;

		return new DubAlignment(
			aligned.Season.Id,
			episodes.Select(episode => episode.number).Order().ToArray(),
			episodes.Where(episode => episode.French).Select(episode => episode.number).Order().ToArray());
	}

	/// <summary>A recap slotted as 6.5, or a special with no number, is no AniList episode.</summary>
	private static bool IsWhole(double? number)
	{
		return number is { } value && value >= 0 && Math.Abs(value - Math.Round(value)) < 1e-9;
	}

	private static bool IsNear(DateOnly? date, DateOnly anchor)
	{
		return date is { } day && Math.Abs(day.DayNumber - anchor.DayNumber) <= ToleranceDays;
	}
}
