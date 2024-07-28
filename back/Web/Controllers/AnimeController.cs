using Elyspio.Utils.Telemetry.Tracing.Elements;
using AnimeTracker.Api.Abstractions.Interfaces.Services;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Abstractions.Models.Transports;
using Microsoft.AspNetCore.Mvc;

namespace AnimeTracker.Api.Web.Controllers;

[Route("api/animes")]
[ApiController]
public class AnimeController(IAnimeService animeService, ILogger<AnimeController> logger) : TracingController(logger)
{

	[HttpGet]
	public async Task<IReadOnlyCollection<Anime>> GetAnimes()
	{
		using var _ = LogController();

		return await animeService.GetAll();
	}


	[HttpPost("refresh/all")]
	public async Task RefreshAll(AnimeDate date)
	{
		using var _ = LogController();

		await animeService.RefreshAll(date);
	}
}