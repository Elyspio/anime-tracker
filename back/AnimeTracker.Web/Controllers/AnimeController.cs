using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Web.Auth;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimeTracker.Web.Controllers;

[Route("api/animes")]
[ApiController]
public class AnimeController(IAnimeService animeService, TimeProvider timeProvider, ILogger<AnimeController> logger)
	: TracingController(logger)
{
	/// <summary>
	///     Enough to see the last few nights and why they stopped, not an audit log. Every successful refresh
	///     is followed by a dub sync, so this is about fifteen of each.
	/// </summary>
	private const int RecentRunsLimit = 30;

	/// <summary>Every anime of a season, soonest bingeable first. Defaults to the current season.</summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<IReadOnlyCollection<Anime>> GetAnimes([FromQuery] int? year, [FromQuery] AnimeSeason? season, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(year)} {Log.F(season)}");

		return await animeService.GetBySeason(Resolve(year, season), cancellationToken);
	}

	/// <summary>
	///     Queues a season refresh and returns straight away. The fetch takes about a second, but it goes
	///     through the scheduler like the nightly job, and a successful one queues the season's dub sync.
	/// </summary>
	[HttpPost("refresh")]
	[Authorize(AuthModule.AdminPolicy)]
	[ProducesResponseType<RefreshRun>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<RefreshRun>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> RefreshAll([FromQuery] int? year, [FromQuery] AnimeSeason? season, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(year)} {Log.F(season)}");

		// A season already being refreshed is answered with that run rather than refreshed twice.
		var result = await animeService.QueueRefresh(Resolve(year, season), cancellationToken);

		return result.AlreadyRunning ? Conflict(result.Run) : Accepted(result.Run);
	}

	/// <summary>
	///     Recent refresh runs, newest first. Anonymous like the season itself: this says how far
	///     along a public scraping schedule is, and it explains a half-filled grid to whoever is
	///     looking at one.
	/// </summary>
	[HttpGet("refreshes")]
	[AllowAnonymous]
	public async Task<IReadOnlyCollection<RefreshRun>> GetRefreshes(CancellationToken cancellationToken)
	{
		using var trace = LogController();

		return await animeService.GetRecentRuns(RecentRunsLimit, cancellationToken);
	}

	private AnimeDate Resolve(int? year, AnimeSeason? season)
	{
		var current = AnimeDate.Current(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

		return new AnimeDate(year ?? current.Year, season ?? current.Season);
	}
}
