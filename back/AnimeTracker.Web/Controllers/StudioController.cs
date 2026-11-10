using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Web.Auth;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnimeTracker.Web.Controllers;

/// <summary>A studio's filmography: every work it was the main studio of, newest first.</summary>
[Route("api/studios")]
[ApiController]
public class StudioController(IStudioService studioService, ILogger<StudioController> logger) : TracingController(logger)
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
	public async Task<StudioFilmography> GetStudio(int studioId, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(studioId)}");

		return await studioService.Get(studioId, cancellationToken);
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
	public async Task<StudioFilmography> RefreshStudio(int studioId, CancellationToken cancellationToken)
	{
		using var trace = LogController($"{Log.F(studioId)}");

		return await studioService.Refresh(studioId, cancellationToken);
	}
}
