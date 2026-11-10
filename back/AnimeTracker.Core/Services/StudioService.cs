using System.Collections.Concurrent;
using System.Net;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Assemblers;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services;

public class StudioService(
	IStudioFilmographyRepository studioRepository,
	IAnimeRepository animeRepository,
	IAnimeSourceAdapter sourceAdapter,
	TimeProvider timeProvider,
	ILogger<StudioService> logger
) : TracingService(logger), IStudioService
{
	/// <summary>
	///     One fetch per studio at a time. The first read of a studio is anonymous and costs the source a
	///     request per fifty works; ten visitors landing on the same link must not cost ten times that.
	/// </summary>
	private readonly ConcurrentDictionary<int, SemaphoreSlim> _fetches = new();

	public async Task<StudioFilmography> Get(int studioId, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(studioId)}");

		var stored = await studioRepository.Get(studioId, cancellationToken);

		return stored is not null ? StudioAssembler.Convert(stored) : await Fetch(studioId, false, cancellationToken);
	}

	public async Task<StudioFilmography> Refresh(int studioId, CancellationToken cancellationToken = default)
	{
		using var _ = LogService($"{Log.F(studioId)}");

		return await Fetch(studioId, true, cancellationToken);
	}

	private async Task<StudioFilmography> Fetch(int studioId, bool replace, CancellationToken cancellationToken)
	{
		await EnsureKnown(studioId, cancellationToken);

		var gate = _fetches.GetOrAdd(studioId, _ => new SemaphoreSlim(1, 1));

		await gate.WaitAsync(cancellationToken);

		try
		{
			// Whoever held the gate may have just stored what this call came for.
			if (!replace && await studioRepository.Get(studioId, cancellationToken) is { } stored) return StudioAssembler.Convert(stored);

			var filmography = await FromSource(studioId, cancellationToken);

			var saved = await studioRepository.Save(filmography, timeProvider.GetUtcNow(), cancellationToken);

			return StudioAssembler.Convert(saved);
		}
		finally
		{
			gate.Release();
		}
	}

	/// <summary>
	///     Only a studio some stored anime credits — or one already fetched — is ever asked of the
	///     source. The read is anonymous: without this, walking the ids would spend the source's rate
	///     limit, which the nightly refresh shares, and fill the store with studios nobody linked to.
	/// </summary>
	private async Task EnsureKnown(int studioId, CancellationToken cancellationToken)
	{
		if (await studioRepository.Get(studioId, cancellationToken) is not null) return;
		if (await animeRepository.HasStudio(studioId, cancellationToken)) return;

		throw NotFound(studioId);
	}

	private async Task<StudioFilmographyBase> FromSource(int studioId, CancellationToken cancellationToken)
	{
		try
		{
			return await sourceAdapter.GetStudio(studioId, cancellationToken) ?? throw NotFound(studioId);
		}
		catch (HttpRequestException exception)
		{
			// Served anonymously like a refresh run's error: the message, never the stack trace.
			throw new HttpException(HttpStatusCode.BadGateway, $"The studio could not be fetched from AniList: {exception.Message}", exception);
		}
	}

	private static HttpException NotFound(int studioId)
	{
		return new HttpException(HttpStatusCode.NotFound, $"No studio is known with id {studioId}.");
	}
}
