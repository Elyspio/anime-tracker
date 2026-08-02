using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Adapters.Nautijon.Adapters;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using AnimeTracker.Adapters.Nautijon.Configs;
using AnimeTracker.Adapters.Nautijon.FlareSolverr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Adapters.Nautijon.Injections;

public static class NautijonModule
{
	public static IServiceCollection AddNautijonAdapter(this IServiceCollection services, IConfiguration config)
	{
		services.AddOptions<NautijonOptions>()
			.Bind(config.GetSection(NautijonOptions.SectionName))
			.ValidateDataAnnotations()
			.Validate(options => Uri.TryCreate(options.FlareSolverrUrl, UriKind.Absolute, out _),
				"Nautijon:FlareSolverrUrl must be an absolute URL.")
			.ValidateOnStart();

		var options = new NautijonOptions();
		config.GetSection(NautijonOptions.SectionName).Bind(options);

		services.AddHttpClient(FlareSolverrClient.ClientName, client =>
		{
			// Trailing slash: the request path is relative, and BaseAddress drops the last segment
			// without one.
			client.BaseAddress = new Uri(options.FlareSolverrUrl.TrimEnd('/') + "/");

			// Must outlast the solver's own budget, otherwise a challenge is abandoned moments
			// before it would have been solved.
			client.Timeout = TimeSpan.FromMilliseconds(options.SolverTimeoutMs) + TimeSpan.FromSeconds(30);
		});
		// Deliberately no resilience pipeline: its 30s attempt timeout is shorter than a Cloudflare
		// challenge takes to solve, and its retries would hammer the site this scraper depends on.
		// Rate limiting is handled inside FlareSolverrClient, which can see the page's own status.

		services.AddSingleton<AnimeTileAssembler>();
		services.AddSingleton<AnimeEpisodesAssembler>();
		services.AddSingleton<FlareSolverrClient>();
		services.AddSingleton<INautijonAdapter, NautijonAdapter>();

		return services;
	}
}
