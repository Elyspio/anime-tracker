using AnimeTracker.Abstractions.Interfaces.Adapters;
using System.Net;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Studios;
using AnimeTracker.Adapters.AniList.Assemblers;
using AnimeTracker.Adapters.AniList.Configs;
using AnimeTracker.Adapters.AniList.GraphQL;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimeTracker.Adapters.AniList.Adapters;

/// <summary>The only project that knows AniList's schema exists.</summary>
internal class AniListAdapter(
	AniListClient client,
	MediaAssembler assembler,
	IOptions<AniListOptions> options,
	ILogger<AniListAdapter> logger
) : TracingAdapter(logger), IAnimeSourceAdapter
{
	public async Task<AnimeBase[]> GetSeason(AnimeDate date, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(date)}");

		var animes = new List<AnimeBase>();

		for (var page = 1; page <= options.Value.MaxPages; page++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var variables = new
			{
				season = SeasonQuery.SeasonName(date.Season),
				year = date.Year,
				page,
				perPage = options.Value.PageSize
			};

			var data = await client.Query<SeasonData>(SeasonQuery.Document, variables, cancellationToken);

			var media = data.Page?.Media ?? [];

			animes.AddRange(media.Select(item => assembler.Convert(date, item)));

			// Paging follows hasNextPage and nothing else: pageInfo.total is capped at 5000 for every
			// query, so treating it as a count would page far past the end of the season.
			if (data.Page?.PageInfo?.HasNextPage != true) return animes.ToArray();
		}

		_logger.LogWarning("Stopped paging {Date} after {MaxPages} pages", date, options.Value.MaxPages);

		return animes.ToArray();
	}

	public async Task<StudioFilmographyBase?> GetStudio(int studioId, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(studioId)}");

		StudioDetails? studio = null;
		var works = new List<StudioWork>();

		for (var page = 1; page <= options.Value.MaxPages; page++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var variables = new { id = studioId, page, perPage = options.Value.PageSize };

			StudioData data;

			try
			{
				data = await client.Query<StudioData>(StudioQuery.Document, variables, cancellationToken);
			}
			// Unlike every other failure, an unknown id is not reported inside a 200: the whole reply is a 404.
			catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
			{
				return null;
			}

			if (data.Studio is null) return null;

			studio ??= data.Studio;

			// A studio's media is not filtered by type upstream, and a few studios are credited on manga.
			works.AddRange((data.Studio.Media?.Nodes ?? [])
				.Where(media => media.Type is null || string.Equals(media.Type, "ANIME", StringComparison.OrdinalIgnoreCase))
				.Select(assembler.ConvertWork));

			if (data.Studio.Media?.PageInfo?.HasNextPage != true) break;

			if (page == options.Value.MaxPages) _logger.LogWarning("Stopped paging studio {StudioId} after {MaxPages} pages", studioId, page);
		}

		return new StudioFilmographyBase
		{
			StudioId = studio!.Id,
			Name = studio.Name?.Trim() ?? "",
			Url = studio.SiteUrl ?? "",
			// A work that moves across a page boundary between two requests comes back twice.
			Works = works.DistinctBy(work => work.SourceId).ToArray()
		};
	}
}
