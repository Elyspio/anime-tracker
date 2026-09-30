using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Web.Auth;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimeTracker.Web.Controllers;

/// <summary>The French dub matches: what an admin looks at, and how they correct it.</summary>
[Route("api/dubs")]
[ApiController]
public class DubController(IDubService dubService, TimeProvider timeProvider, ILogger<DubController> logger) : TracingController(logger)
{
	/// <summary>
	///     The season's matches worth a look. Anonymous like the season itself: which platform series an
	///     anime was matched to is a fact about a public schedule.
	/// </summary>
	[HttpGet("cases")]
	[AllowAnonymous]
	public async Task<IReadOnlyCollection<DubCase>> GetCases([FromQuery] int? year, [FromQuery] AnimeSeason? season, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(year)} {Log.F(season)}");

		var current = AnimeDate.Current(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

		return await dubService.GetCases(new AnimeDate(year ?? current.Year, season ?? current.Season), cancellationToken);
	}

	/// <summary>
	///     Pins an anime to a platform series, blocks the platform for it, or hands it back to the
	///     automatic match — then applies that to the anime straight away.
	/// </summary>
	[HttpPut("{sourceId:int}/{platform}")]
	[Authorize(AuthModule.AdminPolicy)]
	[ProducesResponseType<DubOverrideResult>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<DubOverrideResult> SetOverride(int sourceId, DubPlatform platform, [FromBody] DubOverrideRequest request,
		CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(sourceId)} {Log.F(platform)} {Log.F(request.Mode)}");

		return await dubService.SetOverride(sourceId, platform, request, cancellationToken);
	}
}
