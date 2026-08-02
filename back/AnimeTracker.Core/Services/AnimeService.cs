using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services;

public class AnimeService(
	IAnimeRepository animeRepository,
	INautijonAdapter nautijonAdapter,
	IHangfireJobAdapter hangfireJobAdapter,
	TimeProvider timeProvider,
	ILogger<AnimeService> logger
) : TracingService(logger), IAnimeService
{
	/// <summary>
	///     Nautiljon is scraped through a Cloudflare solver that answers one request at a time.
	///     Refreshing a season is therefore a sequential walk, spaced out so a full pass looks like
	///     someone browsing rather than a crawler.
	/// </summary>
	private static readonly TimeSpan DelayBetweenAnimes = TimeSpan.FromSeconds(2);

	public async Task<IReadOnlyCollection<Anime>> GetBySeason(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(date)}");

		var entities = await animeRepository.GetBySeason(date, cancellationToken);

		var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

		return entities
			.Select(entity => AnimeAssembler.Convert(entity, today))
			// Soonest bingeable first; anything without an estimated end sinks to the bottom.
			.OrderBy(anime => anime.Binge.BingeableAt ?? DateOnly.MaxValue)
			.ThenByDescending(anime => anime.Popularity)
			.ToArray();
	}

	public string QueueRefresh(AnimeDate date)
	{
		using var _ = LogService($"{Log.F(date)}");

		return hangfireJobAdapter.Enqueue<AnimeRefreshJob>(job => job.RefreshSeason(date.Year, date.Season));
	}

	public async Task RefreshAll(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(date)}");

		var animes = await nautijonAdapter.GetAnimes(date, cancellationToken);

		await animeRepository.Refresh(date, animes, cancellationToken);

		foreach (var anime in animes)
		{
			cancellationToken.ThrowIfCancellationRequested();

			await Refresh(anime.Url, cancellationToken);
			await Task.Delay(DelayBetweenAnimes, timeProvider, cancellationToken);
		}
	}

	public async Task Refresh(string animeUrl, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(animeUrl)}");

		var episodes = await nautijonAdapter.GetAnimeEpisodes(animeUrl, cancellationToken);

		await animeRepository.UpdateEpisodes(animeUrl, episodes, cancellationToken);
	}
}
