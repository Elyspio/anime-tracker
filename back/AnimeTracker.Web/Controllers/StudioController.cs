using Elyspio.Utils.Results.AspNetCore;
using Elyspio.Utils.Telemetry;
using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimeTracker.Web.Controllers;

/// <summary>A studio's filmography: every work it was the main studio of, newest first.</summary>
[Route("api/studios")]
[ApiController]
public class StudioController(IStudioService studioService, ILogger<StudioController> logger) : ControllerBase
{
	/// <summary>
	///     Anonymous like the season it is reached from: a filmography is a public fact. The first read of
	///     a studio fetches it from the source, so it is only served for a studio a stored anime credits.
	/// </summary>
	[HttpGet("{studioId:int}")]
	[AllowAnonymous]
	[ProducesResponseType<StudioFilmography>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
	public async Task<ActionResult<StudioFilmography>> GetStudio(int studioId, CancellationToken cancellationToken)
	{
		using var trace = logger.Trace($"{Log.F(studioId)}");

		return (await studioService.Get(studioId, cancellationToken)).ToActionResult();
	}

	/// <summary>
	///     The filmography of the studio a stored anime credits. This is the way in for an anime stored
	///     before studios had an id: its studio is asked of the source on its own, then kept, so the
	///     season does not have to be refreshed first. Anonymous, and bounded by the stored animes.
	/// </summary>
	[HttpGet("~/api/animes/{sourceId:int}/studio")]
	[AllowAnonymous]
	[ProducesResponseType<StudioFilmography>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
	public async Task<ActionResult<StudioFilmography>> GetStudioOfAnime(int sourceId, CancellationToken cancellationToken)
	{
		using var trace = logger.Trace($"{Log.F(sourceId)}");

		return (await studioService.GetByAnime(sourceId, cancellationToken)).ToActionResult();
	}

	/// <summary>
	///     Fetches the filmography again and answers with it. Synchronous, unlike a season refresh: it is
	///     the same few requests the first read already made inline, and nothing else follows from it.
	/// </summary>
	[HttpPost("{studioId:int}/refresh")]
	[Authorize(AuthModule.AdminPolicy)]
	[ProducesResponseType<StudioFilmography>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
	public async Task<ActionResult<StudioFilmography>> RefreshStudio(int studioId, CancellationToken cancellationToken)
	{
		using var trace = logger.Trace($"{Log.F(studioId)}");

		return (await studioService.Refresh(studioId, cancellationToken)).ToActionResult();
	}
}
