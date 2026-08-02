using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Services;
using Xunit;

namespace AnimeTracker.Core.Tests;

public class BingePredictorTests
{
	private static readonly DateOnly Today = new(2026, 2, 1);

	/// <summary>Episodes released weekly starting <paramref name="start" />, one per week.</summary>
	private static Episode[] Weekly(DateOnly start, int count, int intervalDays = 7)
	{
		return Enumerable.Range(0, count)
			.Select(index => new Episode
			{
				Number = index + 1,
				Title = $"Episode {index + 1}",
				Url = null,
				ReleaseDate = start.AddDays(index * intervalDays)
			})
			.ToArray();
	}

	[Fact]
	public void Extrapolates_a_regular_weekly_cadence()
	{
		// 4 of 12 out, last one on 2026-01-25 — eight weeks of episodes left.
		var episodes = Weekly(new DateOnly(2026, 1, 4), 4);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(BingeStatus.Estimated, prediction.Status);
		Assert.Equal(4, prediction.ReleasedEpisodes);
		Assert.Equal(new DateOnly(2026, 1, 25).AddDays(7 * 8), prediction.BingeableAt);
	}

	[Fact]
	public void A_single_pause_week_does_not_shift_the_estimate()
	{
		// Weekly, except the fourth episode slipped by a week. The median gap is still 7 days,
		// which is the whole point of using a median rather than an average.
		var episodes = new[]
		{
			new DateOnly(2026, 1, 4),
			new DateOnly(2026, 1, 11),
			new DateOnly(2026, 1, 18),
			new DateOnly(2026, 2, 1)
		}.Select((date, index) => new Episode { Number = index + 1, Title = $"E{index + 1}", Url = null, ReleaseDate = date }).ToArray();

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(new DateOnly(2026, 2, 1).AddDays(7 * 8), prediction.BingeableAt);
	}

	[Fact]
	public void Two_episodes_aired_the_same_day_do_not_collapse_the_cadence()
	{
		// A double premiere would register as a zero-day gap if distinct dates were not used,
		// and the whole season would be predicted as already finished.
		var episodes = new[]
		{
			new DateOnly(2026, 1, 4),
			new DateOnly(2026, 1, 4),
			new DateOnly(2026, 1, 11),
			new DateOnly(2026, 1, 18)
		}.Select((date, index) => new Episode { Number = index + 1, Title = $"E{index + 1}", Url = null, ReleaseDate = date }).ToArray();

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(BingeStatus.Estimated, prediction.Status);
		Assert.Equal(4, prediction.ReleasedEpisodes);
		Assert.Equal(new DateOnly(2026, 1, 18).AddDays(7 * 8), prediction.BingeableAt);
	}

	[Fact]
	public void An_unannounced_episode_count_yields_no_date_at_all()
	{
		var prediction = BingePredictor.Predict(Weekly(new DateOnly(2026, 1, 4), 4), null, Today);

		Assert.Equal(BingeStatus.UnknownEnd, prediction.Status);
		Assert.Null(prediction.BingeableAt);
		Assert.Null(prediction.TotalEpisodes);
		Assert.Equal(4, prediction.ReleasedEpisodes);
	}

	[Fact]
	public void A_finished_season_is_bingeable_now()
	{
		var episodes = Weekly(new DateOnly(2025, 10, 5), 12);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(BingeStatus.BingeableNow, prediction.Status);
		Assert.Equal(new DateOnly(2025, 10, 5).AddDays(7 * 11), prediction.BingeableAt);
	}

	[Fact]
	public void More_episodes_than_announced_still_counts_as_finished()
	{
		// Nautiljon sometimes lists specials beyond the announced count; that must not read as
		// "still airing" forever.
		var episodes = Weekly(new DateOnly(2025, 10, 5), 13);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(BingeStatus.BingeableNow, prediction.Status);
	}

	[Fact]
	public void Before_the_premiere_the_estimate_starts_from_the_announced_first_airing()
	{
		var episodes = Weekly(new DateOnly(2026, 4, 5), 12);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(BingeStatus.Estimated, prediction.Status);
		Assert.Equal(0, prediction.ReleasedEpisodes);
		Assert.Equal(new DateOnly(2026, 4, 5).AddDays(7 * 11), prediction.BingeableAt);
	}

	[Fact]
	public void An_announced_season_with_no_dates_at_all_has_an_unknown_end()
	{
		var episodes = Enumerable.Range(1, 3)
			.Select(number => new Episode { Number = number, Title = $"E{number}", Url = null, ReleaseDate = null })
			.ToArray();

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(BingeStatus.UnknownEnd, prediction.Status);
		Assert.Null(prediction.BingeableAt);
	}

	[Fact]
	public void A_lone_released_episode_falls_back_to_a_weekly_cadence()
	{
		var episodes = Weekly(new DateOnly(2026, 1, 25), 1);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(new DateOnly(2026, 1, 25).AddDays(7 * 11), prediction.BingeableAt);
	}

	[Fact]
	public void A_biweekly_cadence_is_measured_rather_than_assumed()
	{
		var episodes = Weekly(new DateOnly(2025, 12, 7), 4, 14);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		Assert.Equal(new DateOnly(2026, 1, 18).AddDays(14 * 8), prediction.BingeableAt);
	}

	[Fact]
	public void Episodes_dated_in_the_future_are_not_counted_as_released()
	{
		// The stored list mixes aired and scheduled episodes; only the aired ones may drive the
		// cadence, otherwise a fully scheduled season would read as already bingeable.
		var episodes = Weekly(new DateOnly(2026, 1, 4), 12);

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		// Five weekly slots have passed on 2026-02-01; the seven scheduled ones must not count.
		Assert.Equal(BingeStatus.Estimated, prediction.Status);
		Assert.Equal(5, prediction.ReleasedEpisodes);
		Assert.Equal(new DateOnly(2026, 2, 1).AddDays(7 * 7), prediction.BingeableAt);
	}
}
