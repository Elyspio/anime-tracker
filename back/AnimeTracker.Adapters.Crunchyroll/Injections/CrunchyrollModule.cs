using System.Net;
using System.Net.Security;
using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Adapters.Crunchyroll.Adapters;
using AnimeTracker.Adapters.Crunchyroll.Configs;
using AnimeTracker.Adapters.Crunchyroll.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Adapters.Crunchyroll.Injections;

public static class CrunchyrollModule
{
	/// <summary>The egress proxy every dub platform is reached through, shared with the other platforms.</summary>
	public const string ProxyKey = "Dub:Proxy";

	public static IServiceCollection AddCrunchyrollAdapter(this IServiceCollection services, IConfiguration config)
	{
		// Never from the local address: without the proxy, Crunchyroll is not asked at all and the dub
		// sync has one platform fewer. Getting the network's address banned is the thing to avoid.
		if (config[ProxyKey] is not { Length: > 0 } proxy) return services;

		services.AddOptions<CrunchyrollOptions>()
			.Bind(config.GetSection(CrunchyrollOptions.SectionName))
			.ValidateDataAnnotations()
			.Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Crunchyroll:BaseUrl must be an absolute URL.")
			.ValidateOnStart();

		// No resilience pipeline: a retried request is another one Cloudflare counts, and a failure is
		// already handled by keeping the previous measurement until the next night.
		services.AddHttpClient(CrunchyrollClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30))
			.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
			{
				Proxy = new WebProxy(proxy),
				UseProxy = true,
				// Cloudflare challenges a resumed TLS session: the first connection of the process goes
				// through and every later one is refused, the moment the handler pool opens a second
				// connection. A full handshake every time was measured to pass, five connections out of five.
				SslOptions = new SslClientAuthenticationOptions { AllowTlsResume = false },
				// Stateless: a cookie set on a challenged reply must not follow the next request around.
				UseCookies = false
			});

		services.AddSingleton<CrunchyrollClient>();
		services.AddSingleton<IDubPlatformAdapter, CrunchyrollAdapter>();

		return services;
	}
}
