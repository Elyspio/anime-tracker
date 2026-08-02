using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services;

/// <summary>
///     The scheduler's entry point. It exists as its own type because Hangfire resolves the target
///     of a job from the container, and a hosted service is not a good target.
/// </summary>
public class AnimeRefreshJob(IAnimeService animeService, TimeProvider timeProvider, ILogger<AnimeRefreshJob> logger)
	: TracingService(logger)
{
	public const string RecurringJobId = "anime-season-refresh";

	// ReSharper disable once MemberCanBePrivate.Global — called by Hangfire through an expression.
	public async Task RefreshCurrentSeason()
	{
		using var trace = LogService();

		var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

		await animeService.RefreshAll(AnimeDate.Current(today));
	}

	/// <summary>
	///     Takes the season apart rather than an <see cref="AnimeDate" />: Hangfire serialises job
	///     arguments to its storage, and primitives survive a schema change that a record would not.
	/// </summary>
	// ReSharper disable once MemberCanBePrivate.Global — called by Hangfire through an expression.
	public async Task RefreshSeason(int year, AnimeSeason season)
	{
		using var trace = LogService($"{Log.F(year)} {Log.F(season)}");

		await animeService.RefreshAll(new AnimeDate(year, season));
	}
}
