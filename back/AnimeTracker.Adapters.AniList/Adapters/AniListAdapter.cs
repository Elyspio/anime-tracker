using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Abstractions.Models.Base.Anime;
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
}
