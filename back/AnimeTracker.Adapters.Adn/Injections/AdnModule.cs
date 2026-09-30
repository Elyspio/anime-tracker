using System.Net;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Adapters.Adn.Adapters;
using AnimeTracker.Adapters.Adn.Configs;
using AnimeTracker.Adapters.Adn.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Adapters.Adn.Injections;

public static class AdnModule
{
	/// <summary>The egress proxy every dub platform is reached through, shared with the other platforms.</summary>
	public const string ProxyKey = "Dub:Proxy";

	public static IServiceCollection AddAdnAdapter(this IServiceCollection services, IConfiguration config)
	{
		// Never from the local address: without the proxy, ADN is simply not asked.
		if (config[ProxyKey] is not { Length: > 0 } proxy) return services;

		services.AddOptions<AdnOptions>()
			.Bind(config.GetSection(AdnOptions.SectionName))
			.ValidateDataAnnotations()
			.Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Adn:BaseUrl must be an absolute URL.")
			.ValidateOnStart();

		services.AddHttpClient(AdnClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30))
			.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
			{
				Proxy = new WebProxy(proxy),
				UseProxy = true,
				UseCookies = false
			});

		services.AddSingleton<AdnClient>();
		services.AddSingleton<IDubPlatformAdapter, AdnAdapter>();

		return services;
	}
}
