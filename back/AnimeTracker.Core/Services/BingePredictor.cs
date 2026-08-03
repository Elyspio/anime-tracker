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
		var dates = episodes.Select(episode => episode.ReleaseDate).Order().ToArray();

		var released = dates.Count(date => date <= today);

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
				BingeableAt = released > 0 ? dates[released - 1] : today,
				ReleasedEpisodes = released,
				TotalEpisodes = total
			};
		}

		// The schedule already reaches the last announced episode: that date is the broadcaster's,
		// so publish it as a fact instead of extrapolating over the top of it.
		if (dates.Length >= total)
		{
			return new BingePrediction
			{
				Status = BingeStatus.Announced,
				BingeableAt = dates[total - 1],
				ReleasedEpisodes = released,
				TotalEpisodes = total
			};
		}

		// A total was announced but not a single slot was: there is no anchor to extrapolate from,
		// and a date built on nothing would be indistinguishable on screen from a measured one.
		if (dates.Length == 0)
		{
			return new BingePrediction
			{
				Status = BingeStatus.UnknownEnd,
				BingeableAt = null,
				ReleasedEpisodes = 0,
				TotalEpisodes = total
			};
		}

		// Partly scheduled: extend the tail at the cadence the published slots show.
		var cadence = MedianCadence(dates);

		return new BingePrediction
		{
			Status = BingeStatus.Estimated,
			BingeableAt = dates[^1].AddDays(cadence.Days * (total - dates.Length)),
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
