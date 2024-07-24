using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using AnimeTracker.Api.Abstractions.Interfaces.Repositories;
using AnimeTracker.Api.Abstractions.Interfaces.Services;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Abstractions.Models.Entities;
using AnimeTracker.Api.Abstractions.Models.Transports;
using AnimeTracker.Api.Core.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Api.Core.Services;

public class AnimeService : TracingService, IAnimeService
{
	private readonly AnimeAssembler _animeAssembler = new();
	private readonly INautijonAdapter _nautijonAdapter;
	private readonly IAnimeRepository _animeRepository;
	private readonly IMassTransitJobAdapter _massTransitJobAdapter;

	public AnimeService(IAnimeRepository animeRepository, ILogger<AnimeService> logger, INautijonAdapter nautijonAdapter, IMassTransitJobAdapter massTransitJobAdapter) : base(logger)
	{
		_animeRepository = animeRepository;
		_nautijonAdapter = nautijonAdapter;
		_massTransitJobAdapter = massTransitJobAdapter;
	}


	public async Task<IReadOnlyCollection<Anime>> GetAll()
	{
		using var _ = LogService();

		var animes = await _animeRepository.GetAll();

		return _animeAssembler.Convert(animes);
	}

	public async Task Refresh(string animeUrl)
	{
		using var _ = LogService($"{Log.F(animeUrl)}");

		var episodes = await _nautijonAdapter.GetAnimeEpisodes(animeUrl);

		var entity = await _animeRepository.UpdateEpisodes(animeUrl, episodes);
	}

	public async Task RefreshAll(AnimeDate date)
	{
		using var logger = LogService(Log.F(date));

		var animes = await _nautijonAdapter.GetAnimes(date);

		await _animeRepository.Refresh(date, animes);

		foreach (var anime in animes)
		{
			_ = _massTransitJobAdapter.SendAnimeRefreshMessage(anime.Url);
		}

	}
}