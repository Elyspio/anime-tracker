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
	/// <summary>Every anime of a season, soonest bingeable first. Defaults to the current season.</summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<IReadOnlyCollection<Anime>> GetAnimes([FromQuery] int? year, [FromQuery] AnimeSeason? season, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(year)} {Log.F(season)}");

		return await animeService.GetBySeason(Resolve(year, season), cancellationToken);
	}

	/// <summary>
	///     Queues a season re-scrape and returns straight away. The walk itself takes tens of
	///     minutes at the pace Nautiljon is polled, so it runs on the scheduler, not in the request.
	/// </summary>
	[HttpPost("refresh")]
	[Authorize(AuthModule.AdminPolicy)]
	[ProducesResponseType(StatusCodes.Status202Accepted)]
	public IActionResult RefreshAll([FromQuery] int? year, [FromQuery] AnimeSeason? season)
	{
		using var trace = LogController($"{Log.F(year)} {Log.F(season)}");

		var date = Resolve(year, season);

		return Accepted(new RefreshQueued(animeService.QueueRefresh(date), date));
	}

	private AnimeDate Resolve(int? year, AnimeSeason? season)
	{
		var current = AnimeDate.Current(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

		return new AnimeDate(year ?? current.Year, season ?? current.Season);
	}
}
