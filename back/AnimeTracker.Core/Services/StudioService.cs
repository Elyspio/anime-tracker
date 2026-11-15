using Elyspio.Utils.Results;
using AnimeTracker.Abstractions.Errors;
using Elyspio.Utils.Telemetry;
using System.Collections.Concurrent;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Abstractions.Models.Transports;
using AnimeTracker.Core.Assemblers;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services;

public class StudioService(
	IStudioFilmographyRepository studioRepository,
	IAnimeRepository animeRepository,
	IAnimeSourceAdapter sourceAdapter,
	TimeProvider timeProvider,
	ILogger<StudioService> logger
) : IStudioService
{
	/// <summary>
	///     One fetch per studio at a time. The first read of a studio is anonymous and costs the source a
	///     request per fifty works; ten visitors landing on the same link must not cost ten times that.
	/// </summary>
	private readonly ConcurrentDictionary<int, SemaphoreSlim> _fetches = new();

	public async Task<Result<StudioFilmography>> Get(int studioId, CancellationToken cancellationToken = default)
	{
		using var trace = logger.Trace($"{Log.F(studioId)}");

		var stored = await studioRepository.Get(studioId, cancellationToken);
		if (stored is not null) return StudioAssembler.Convert(stored);

		return trace.Return(await Fetch(studioId, false, cancellationToken));
	}

	public async Task<Result<StudioFilmography>> GetByAnime(int sourceId, CancellationToken cancellationToken = default)
	{
		using var trace = logger.Trace($"{Log.F(sourceId)}");

		// Only a stored anime: like a studio, an id nobody stored is never asked of the source.
		var anime = await animeRepository.GetBySourceId(sourceId, cancellationToken);
		if (anime is null) return trace.Fail(new ResourceNotFound($"No anime is stored with id {sourceId}."));

		if (anime.StudioId is { } known) return trace.Return(await Get(known, cancellationToken));

		if (!(await MainStudio(sourceId, cancellationToken)).TryGet(out var studio, out var error)) return trace.Fail(error);
		if (studio is null) return trace.Fail(new ResourceNotFound($"AniList credits no main studio for {anime.Title}."));

		// Kept on the anime: the next click costs the source nothing, and the studio is now one a
		// stored anime credits, which is what lets its filmography be fetched.
		await animeRepository.SetStudio(sourceId, studio, cancellationToken);

		return trace.Return(await Get(studio.Id, cancellationToken));
	}

	public async Task<Result<StudioFilmography>> Refresh(int studioId, CancellationToken cancellationToken = default)
	{
		using var trace = logger.Trace($"{Log.F(studioId)}");

		return trace.Return(await Fetch(studioId, true, cancellationToken));
	}

	private async Task<Result<StudioFilmography>> Fetch(int studioId, bool replace, CancellationToken cancellationToken)
	{
		if (!await IsKnown(studioId, cancellationToken)) return NotFound(studioId);

		var gate = _fetches.GetOrAdd(studioId, _ => new SemaphoreSlim(1, 1));

		await gate.WaitAsync(cancellationToken);

		try
		{
			// Whoever held the gate may have just stored what this call came for.
			if (!replace && await studioRepository.Get(studioId, cancellationToken) is { } stored) return StudioAssembler.Convert(stored);

			if (!(await FromSource(studioId, cancellationToken)).TryGet(out var filmography, out var error)) return error;

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
	private async Task<bool> IsKnown(int studioId, CancellationToken cancellationToken)
	{
		return await studioRepository.Get(studioId, cancellationToken) is not null || await animeRepository.HasStudio(studioId, cancellationToken);
	}

	private async Task<Result<StudioFilmographyBase>> FromSource(int studioId, CancellationToken cancellationToken)
	{
		try
		{
			return await sourceAdapter.GetStudio(studioId, cancellationToken) is { } filmography ? filmography : NotFound(studioId);
		}
		catch (HttpRequestException exception)
		{
			// Served anonymously like a refresh run's error: the message, never the stack trace.
			return Unavailable(exception);
		}
	}

	private async Task<Result<StudioReference?>> MainStudio(int sourceId, CancellationToken cancellationToken)
	{
		try
		{
			return await sourceAdapter.GetMainStudio(sourceId, cancellationToken);
		}
		catch (HttpRequestException exception)
		{
			return Unavailable(exception);
		}
	}

	private SourceUnavailable Unavailable(HttpRequestException exception)
	{
		logger.LogWarning(exception, "The studio could not be fetched from AniList");

		return new SourceUnavailable($"The studio could not be fetched from AniList: {exception.Message}");
	}

	private static ResourceNotFound NotFound(int studioId)
	{
		return new ResourceNotFound($"No studio is known with id {studioId}.");
	}
}
