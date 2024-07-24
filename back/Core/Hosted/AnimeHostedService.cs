using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using AnimeTracker.Api.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Api.Core.Hosted;

public class AnimeHostedService: TracingService, IHostedService
{
	private readonly IMassTransitJobAdapter _massTransitJobAdapter;
	private readonly IHangfireJobAdapter _hangfireJobAdapter;
	private readonly IAnimeRepository _animeRepository;

	private const string JobId = "anime-episodes-refresh";

	public AnimeHostedService(ILogger<AnimeHostedService> logger, IMassTransitJobAdapter massTransitJobAdapter, IAnimeRepository animeRepository, IHangfireJobAdapter hangfireJobAdapter) : base(logger)
	{
		_massTransitJobAdapter = massTransitJobAdapter;
		_animeRepository = animeRepository;
		_hangfireJobAdapter = hangfireJobAdapter;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var _ = LogService();

		await _hangfireJobAdapter.Schedule(JobId, () => RefreshAnimeEpisodes(), "0 12 * * *");
	}

	// ReSharper disable once MemberCanBePrivate.Global Called by Hangfire
	public async Task RefreshAnimeEpisodes()
	{
		using var _ = LogService();

		var animes = await _animeRepository.GetAll();

		await Task.WhenAll(animes.Select(async anime =>
		{
			await Task.Delay(1000 * Random.Shared.Next(1, 10));
			await _massTransitJobAdapter.SendAnimeRefreshMessage(anime.Url);
		}));
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_hangfireJobAdapter.Clear(JobId);
		return Task.CompletedTask;
	}
}