using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Adapters.Nautijon.Adapters;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using AnimeTracker.Adapters.Nautijon.Configs;
using AnimeTracker.Adapters.Nautijon.Utils.Clients;
using FlareSolverrSharp;
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

		services.AddTransient(_ => new ClearanceHandler(options.FlareSolverrUrl)
		{
			MaxTimeout = options.SolverTimeoutMs
		});

		services.AddTransient<ClientSideRateLimitedHandler>();

		services.AddHttpClient(NautijonAdapter.ClientName, client =>
			{
				// Nautiljon serves a different page to clients it does not recognise as a browser.
				client.DefaultRequestHeaders.Add("User-Agent",
					"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
			})
			.ConfigurePrimaryHttpMessageHandler<ClearanceHandler>()
			.AddHttpMessageHandler<ClientSideRateLimitedHandler>();

		services.AddSingleton<AnimeTileAssembler>();
		services.AddSingleton<AnimeEpisodesAssembler>();
		services.AddSingleton<INautijonAdapter, NautijonAdapter>();

		return services;
	}
}
