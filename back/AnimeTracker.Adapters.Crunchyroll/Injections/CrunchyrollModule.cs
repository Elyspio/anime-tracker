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
		// Never from the local address: without the gateway or the proxy, Crunchyroll is not asked at all
		// and the dub sync has one platform fewer. Getting the network's address banned is the thing to avoid.
		var gateway = config[$"{CrunchyrollOptions.SectionName}:{nameof(CrunchyrollOptions.Gateway)}"];
		var proxy = config[ProxyKey];
		if (string.IsNullOrWhiteSpace(gateway) && string.IsNullOrWhiteSpace(proxy)) return services;

		services.AddOptions<CrunchyrollOptions>()
			.Bind(config.GetSection(CrunchyrollOptions.SectionName))
			.ValidateDataAnnotations()
			.Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Crunchyroll:BaseUrl must be an absolute URL.")
			.Validate(options => Uri.TryCreate(options.ApiRoot, UriKind.Absolute, out _), "Crunchyroll:Gateway must be an absolute URL.")
			.ValidateOnStart();

		// No resilience pipeline: a retried request is another one Cloudflare counts, and a failure is
		// already handled by keeping the previous measurement until the next night.
		var usesGateway = !string.IsNullOrWhiteSpace(gateway);
		services.AddHttpClient(CrunchyrollClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30))
			.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
			{
				// The gateway is on the LAN or in the cluster and opens the TLS connection itself: reached
				// directly. Without it, straight to Crunchyroll through the egress proxy.
				Proxy = usesGateway ? null : new WebProxy(proxy),
				UseProxy = !usesGateway,
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
