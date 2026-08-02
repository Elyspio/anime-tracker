using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Core.Services;

/// <summary>
///     Answers the only question this product exists to answer: when will every episode be out?
///     Pure and static — no tracing, no clock of its own.
/// </summary>
public static class BingePredictor
{
	/// <summary>
	///     Used when the cadence cannot be measured (fewer than two release dates). Simulcast anime
	///     are weekly by default, so this is the least surprising guess to make explicit.
	/// </summary>
	private static readonly TimeSpan FallbackCadence = TimeSpan.FromDays(7);

	public static BingePrediction Predict(IReadOnlyCollection<Episode> episodes, int? totalEpisodes, DateOnly today)
	{
		var releaseDates = episodes
			.Select(episode => episode.ReleaseDate)
			.OfType<DateOnly>()
			.Where(date => date <= today)
			.Order()
			.ToArray();

		var released = releaseDates.Length;

		// Nothing announced: refuse to invent an end date rather than show a number that is wrong.
		if (totalEpisodes is not { } total)
		{
			return new BingePrediction
			{
				Status = BingeStatus.UnknownEnd,
				BingeableAt = null,
				ReleasedEpisodes = released,
				TotalEpisodes = null
			};
		}

		if (released >= total)
		{
			return new BingePrediction
			{
				Status = BingeStatus.BingeableNow,
				BingeableAt = releaseDates.Length > 0 ? releaseDates[^1] : today,
				ReleasedEpisodes = released,
				TotalEpisodes = total
			};
		}

		// No episode out yet — the first one has not aired, so there is nothing to extrapolate from.
		if (released == 0)
		{
			var upcoming = episodes.Select(episode => episode.ReleaseDate).OfType<DateOnly>().Order().ToArray();

			return new BingePrediction
			{
				Status = upcoming.Length > 0 ? BingeStatus.Estimated : BingeStatus.UnknownEnd,
				BingeableAt = upcoming.Length > 0 ? upcoming[0].AddDays(FallbackCadence.Days * (total - 1)) : null,
				ReleasedEpisodes = 0,
				TotalEpisodes = total
			};
		}

		var cadence = MedianCadence(releaseDates);
		var remaining = total - released;

		return new BingePrediction
		{
			Status = BingeStatus.Estimated,
			BingeableAt = releaseDates[^1].AddDays(cadence.Days * remaining),
			ReleasedEpisodes = released,
			TotalEpisodes = total
		};
	}

	/// <summary>
	///     Median, not mean: a single pause week or a recap break would drag an average and push the
	///     estimate out by more than the schedule actually slipped. Distinct dates only, so a double
	///     episode aired on one day does not register as a zero-day gap.
	/// </summary>
	private static TimeSpan MedianCadence(IReadOnlyList<DateOnly> releaseDates)
	{
		var distinct = releaseDates.Distinct().Order().ToArray();

		if (distinct.Length < 2) return FallbackCadence;

		var gaps = distinct
			.Zip(distinct.Skip(1), (previous, next) => next.DayNumber - previous.DayNumber)
			.Order()
			.ToArray();

		var middle = gaps.Length / 2;
		var days = gaps.Length % 2 == 1
			? gaps[middle]
			: (gaps[middle - 1] + gaps[middle]) / 2;

		return days > 0 ? TimeSpan.FromDays(days) : FallbackCadence;
	}
}
