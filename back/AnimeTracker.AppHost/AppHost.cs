// Aspire AppHost — MongoDB + Keycloak + the API + the Vite front, for local development.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddLogging(x => x.AddSimpleConsole(l => l.SingleLine = true));

// Fixed development credentials. They are deliberately boring and deliberately committed: this
// MongoDB is a loopback container holding scraped public listings, and a generated password would
// only make `mongosh` harder to reach for. Deployments supply their own connection string.
var mongoUser = builder.AddParameter("mongo-username", "aspire");
var mongoPassword = builder.AddParameter("mongo-password", "aspire", secret: true);

// Port pinned to 27017 so the host reaches the server at the same address the replica set
// advertises for itself. On a random host port the driver would discover "localhost:27017" from
// the set configuration and dial a port nothing is published on.
var mongo = builder.AddMongoDB("mongo", 27017, mongoUser, mongoPassword)
	.WithDockerfile("mongo")
	.WithDataVolume();

var mongodb = mongo.AddDatabase("anime-tracker");

// Keycloak on a pinned port so the issuer URL is identical for the browser and the API.
// The realm seeds the admin role, an "admin" user holding it, and a "user" that does not —
// the second one is how the access-denied path stays exercised.
var keycloak = builder.AddKeycloak("keycloak", 8080)
	.WithDataVolume()
	.WithRealmImport("./Realms");

var authority = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/anime-tracker");
const string clientId = "anime-tracker";

// The egress proxy of the French dub sync, in the cluster's qBittorrent pod behind NordVPN, reached on
// its LAN address. The streaming platforms are only ever asked through it: a sync run from here would
// otherwise hit them from the home network's address, which is the one that must never get banned.
// Blank it to run without a dub sync — the platforms are then not asked at all.
var dubProxy = builder.AddParameter("dub-proxy", "http://10.0.1.123:8888");

// Crunchyroll goes through the gateway beside that proxy instead: plain HTTP from here, TLS opened by
// nginx from the VPN exit. Cloudflare challenges every TLS handshake .NET makes on Windows, so without
// it the Crunchyroll half of a sync never gets through from a development machine.
var crunchyrollGateway = builder.AddParameter("crunchyroll-gateway", "http://10.0.1.123:8889");

// AniList is a public API: besides the proxy, the only external dependency at runtime is an outbound
// HTTPS call the API makes itself.
var api = builder.AddProject<AnimeTracker_Web>("api")
	.WithReference(mongodb, "MongoDB")
	.WaitFor(mongodb)
	.WaitFor(keycloak)
	.WithEnvironment("Auth__Authority", authority)
	.WithEnvironment("Auth__Audience", clientId)
	.WithEnvironment("Auth__AdminRole", "anime-tracker-admin")
	.WithEnvironment("Dub__Proxy", dubProxy)
	.WithEnvironment("Crunchyroll__Gateway", crunchyrollGateway);

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
