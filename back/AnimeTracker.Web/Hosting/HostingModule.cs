using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AnimeTracker.Web.Hosting;

/// <summary>
///     Service discovery, HTTP resilience and health endpoints. Telemetry is owned by
///     Elyspio.Utils.Telemetry, wired in Program.cs.
/// </summary>
public static class HostingModule
{
	private const string HealthEndpointPath = "/health";
	private const string AlivenessEndpointPath = "/alive";

	public static WebApplicationBuilder AddHostingDefaults(this WebApplicationBuilder builder)
	{
		builder.Services.AddHealthChecks()
			.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

		builder.Services.AddServiceDiscovery();

		// Service discovery is a safe default; the standard resilience pipeline is not. Its 30s
		// attempt timeout and automatic retries are wrong for the one outbound client this app
		// has: a Cloudflare challenge takes longer than that to solve, and a retried scrape is
		// extra load on a site that tolerates us. Clients that want resilience opt in.
		builder.Services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());

		return builder;
	}

	public static WebApplication MapDefaultEndpoints(this WebApplication app)
	{
		// Anonymous in every environment: Kubernetes probes them and they reveal nothing beyond
		// whether the process is up.
		app.MapHealthChecks(HealthEndpointPath).AllowAnonymous();

		app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
		{
			Predicate = r => r.Tags.Contains("live")
		}).AllowAnonymous();

		return app;
	}
}
