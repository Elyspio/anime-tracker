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

	/// <summary>Re-scrapes a season. Slow by design — it walks Nautiljon one page at a time.</summary>
	[HttpPost("refresh")]
	[Authorize(AuthModule.AdminPolicy)]
	public async Task RefreshAll([FromQuery] int? year, [FromQuery] AnimeSeason? season, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(year)} {Log.F(season)}");

		await animeService.RefreshAll(Resolve(year, season), cancellationToken);
	}

	private AnimeDate Resolve(int? year, AnimeSeason? season)
	{
		var current = AnimeDate.Current(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

		return new AnimeDate(year ?? current.Year, season ?? current.Season);
	}
}
