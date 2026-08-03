using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Services;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

public class BingePredictorTests
{
	private static readonly DateOnly Today = new(2026, 2, 1);

	/// <summary>Episodes scheduled weekly from <paramref name="start" />, one per week.</summary>
	private static Episode[] Weekly(DateOnly start, int count, int intervalDays = 7)
	{
		return Enumerable.Range(0, count)
			.Select(index => new Episode(index + 1, start.AddDays(index * intervalDays)))
			.ToArray();
	}

	[Fact]
	public void Publishes_the_announced_date_when_the_schedule_reaches_the_end()
	{
		// All twelve slots are published and five have aired: the end date is the broadcaster's, and
		// calling it an estimate would be pretending not to know something we do.
		var prediction = BingePredictor.Predict(Weekly(new DateOnly(2026, 1, 4), 12), 12, Today);

		prediction.Status.ShouldBe(BingeStatus.Announced);
		prediction.ReleasedEpisodes.ShouldBe(5);
		prediction.BingeableAt.ShouldBe(new DateOnly(2026, 1, 4).AddDays(7 * 11));
	}

	[Fact]
	public void Ignores_slots_published_past_the_announced_total()
	{
		// Thirteen slots for a twelve-episode run — the thirteenth is a special, not the finale.
		var prediction = BingePredictor.Predict(Weekly(new DateOnly(2026, 1, 4), 13), 12, Today);

		prediction.Status.ShouldBe(BingeStatus.Announced);
		prediction.BingeableAt.ShouldBe(new DateOnly(2026, 1, 4).AddDays(7 * 11));
	}

	[Fact]
	public void Extrapolates_the_tail_the_schedule_does_not_cover()
	{
		// Thirteen of fourteen slots published, weekly — the fourteenth lands a week after the last.
		var prediction = BingePredictor.Predict(Weekly(new DateOnly(2026, 1, 4), 13), 14, Today);

		prediction.Status.ShouldBe(BingeStatus.Estimated);
		prediction.BingeableAt.ShouldBe(new DateOnly(2026, 1, 4).AddDays(7 * 13));
	}

	[Fact]
	public void A_single_pause_week_does_not_shift_the_estimate()
	{
		// Weekly, except one episode slipped by a week. The median gap is still 7 days, which is the
		// whole point of using a median rather than an average.
		var episodes = new Episode[]
		{
			new(1, new DateOnly(2026, 1, 4)),
			new(2, new DateOnly(2026, 1, 11)),
			new(3, new DateOnly(2026, 1, 18)),
			new(4, new DateOnly(2026, 2, 1))
		};

		var prediction = BingePredictor.Predict(episodes, 12, Today);

		prediction.Status.ShouldBe(BingeStatus.Estimated);
		prediction.BingeableAt.ShouldBe(new DateOnly(2026, 2, 1).AddDays(7 * 8));
	}

	[Fact]
	public void A_double_episode_on_one_day_does_not_collapse_the_cadence()
	{
		// Two episodes share a date; counted as a zero-day gap the whole countdown would vanish.
		var episodes = new Episode[]
		{
			new(1, new DateOnly(2026, 1, 4)),
			new(2, new DateOnly(2026, 1, 4)),
			new(3, new DateOnly(2026, 1, 11)),
			new(4, new DateOnly(2026, 1, 18))
		};

		BingePredictor.Predict(episodes, 12, Today).BingeableAt
			.ShouldBe(new DateOnly(2026, 1, 18).AddDays(7 * 8));
	}

	[Fact]
	public void Every_episode_out_is_bingeable_now()
	{
		var prediction = BingePredictor.Predict(Weekly(new DateOnly(2025, 11, 2), 12), 12, Today);

		prediction.Status.ShouldBe(BingeStatus.BingeableNow);
		prediction.ReleasedEpisodes.ShouldBe(12);
		prediction.BingeableAt.ShouldBe(new DateOnly(2025, 11, 2).AddDays(7 * 11));
	}

	[Fact]
	public void An_unannounced_total_gets_no_date_at_all()
	{
		// Inventing a plausible twelve would make every countdown untrustworthy, because nothing on
		// screen would separate a measurement from a guess.
		var prediction = BingePredictor.Predict(Weekly(new DateOnly(2026, 1, 4), 4), null, Today);

		prediction.Status.ShouldBe(BingeStatus.UnknownEnd);
		prediction.BingeableAt.ShouldBeNull();
		prediction.TotalEpisodes.ShouldBeNull();
		prediction.ReleasedEpisodes.ShouldBe(4);
	}

	[Fact]
	public void An_announced_total_with_no_schedule_still_gets_no_date()
	{
		var prediction = BingePredictor.Predict([], 12, Today);

		prediction.Status.ShouldBe(BingeStatus.UnknownEnd);
		prediction.BingeableAt.ShouldBeNull();
		prediction.TotalEpisodes.ShouldBe(12);
		prediction.ReleasedEpisodes.ShouldBe(0);
	}

	[Fact]
	public void Counts_only_episodes_whose_date_has_passed()
	{
		// 4, 11, 18, 25 January and 1 February are behind or on today; the rest is ahead of it.
		BingePredictor.Predict(Weekly(new DateOnly(2026, 1, 4), 12), 12, Today).ReleasedEpisodes.ShouldBe(5);
	}

	[Fact]
	public void An_episode_airing_today_counts_as_released()
	{
		var prediction = BingePredictor.Predict([new Episode(1, Today)], 1, Today);

		prediction.Status.ShouldBe(BingeStatus.BingeableNow);
		prediction.BingeableAt.ShouldBe(Today);
	}

	[Fact]
	public void A_single_scheduled_episode_falls_back_to_a_weekly_cadence()
	{
		// One slot published out of twelve: no gap to measure, so the weekly default is stated
		// explicitly rather than left to chance.
		var prediction = BingePredictor.Predict([new Episode(1, new DateOnly(2026, 3, 1))], 12, Today);

		prediction.Status.ShouldBe(BingeStatus.Estimated);
		prediction.BingeableAt.ShouldBe(new DateOnly(2026, 3, 1).AddDays(7 * 11));
	}
}
