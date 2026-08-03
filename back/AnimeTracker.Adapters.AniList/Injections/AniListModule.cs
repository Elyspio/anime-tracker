using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Adapters.AniList.Adapters;
using AnimeTracker.Adapters.AniList.Assemblers;
using AnimeTracker.Adapters.AniList.Configs;
using AnimeTracker.Adapters.AniList.GraphQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Adapters.AniList.Injections;

public static class AniListModule
{
	public static IServiceCollection AddAniListAdapter(this IServiceCollection services, IConfiguration config)
	{
		services.AddOptions<AniListOptions>()
			.Bind(config.GetSection(AniListOptions.SectionName))
			.ValidateDataAnnotations()
			.Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _),
				"AniList:Endpoint must be an absolute URL.")
			.ValidateOnStart();

		// A season is two requests against a public API that answers in under a second. No resilience
		// pipeline: its retries would spend the 30-per-minute budget the adapter already respects by
		// reading Retry-After when it is told to slow down.
		services.AddHttpClient(AniListClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30));

		services.AddSingleton<MediaAssembler>();
		services.AddSingleton<AniListClient>();
		services.AddSingleton<IAnimeSourceAdapter, AniListAdapter>();

		return services;
	}
}
