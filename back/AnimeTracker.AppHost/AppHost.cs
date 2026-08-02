// Aspire AppHost — MongoDB + Keycloak + FlareSolverr + the API + the Vite front, for local development.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddLogging(x => x.AddSimpleConsole(l => l.SingleLine = true));

var mongo = builder.AddMongoDB("mongo").WithImage("mongo:8.0.4").WithDataVolume();
var mongodb = mongo.AddDatabase("anime-tracker");

// Keycloak on a pinned port so the issuer URL is identical for the browser and the API.
// The realm seeds the admin role, an "admin" user holding it, and a "user" that does not —
// the second one is how the access-denied path stays exercised.
var keycloak = builder.AddKeycloak("keycloak", 8080)
	.WithDataVolume()
	.WithRealmImport("./Realms");

var authority = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/anime-tracker");
const string clientId = "anime-tracker";

// Nautiljon sits behind Cloudflare: every scrape goes through this solver, which holds the
// clearance cookie. Nothing works without it, locally or in production.
var flareSolverr = builder.AddContainer("flaresolverr", "ghcr.io/flaresolverr/flaresolverr", "latest")
	.WithHttpEndpoint(targetPort: 8191, name: "http")
	.WithEnvironment("LOG_LEVEL", "info");

var api = builder.AddProject<AnimeTracker_Web>("api")
	.WithReference(mongodb, "MongoDB")
	.WaitFor(mongodb)
	.WaitFor(keycloak)
	.WithEnvironment("Nautijon__FlareSolverrUrl", flareSolverr.GetEndpoint("http"))
	.WithEnvironment("Auth__Authority", authority)
	.WithEnvironment("Auth__Audience", clientId)
	.WithEnvironment("Auth__AdminRole", "anime-tracker-admin");

// Vite dev server. Pinned to 5173 and un-proxied: a stable origin is what makes the OIDC
// redirect URIs in the realm valid, and it keeps the HMR websocket working.
builder.AddViteApp("front", "../../front")
	.WithPnpm()
	.WithReference(api)
	.WithEnvironment("VITE_API_TARGET", api.GetEndpoint("https"))
	.WithEnvironment("VITE_OIDC_AUTHORITY", authority)
	.WithEnvironment("VITE_OIDC_CLIENT_ID", clientId)
	.WithEndpoint("http", endpoint =>
	{
		endpoint.Port = 5173;
		endpoint.TargetPort = 5173;
		endpoint.IsProxied = false;
	})
	.WaitFor(api);

builder.Build().Run();
