using System.Text.Json.Serialization;
using AnimeTracker.Adapters.Hangfire.Adapters;
using AnimeTracker.Adapters.Hangfire.Injections;
using AnimeTracker.Adapters.MongoDB.Injections;
using AnimeTracker.Adapters.MongoDB.Technical;
using AnimeTracker.Adapters.AniList.Configs;
using AnimeTracker.Adapters.AniList.Injections;
using AnimeTracker.Adapters.Adn.Configs;
using AnimeTracker.Adapters.Adn.Injections;
using AnimeTracker.Adapters.Crunchyroll.Configs;
using AnimeTracker.Adapters.Crunchyroll.Injections;
using AnimeTracker.Core.Injections;
using AnimeTracker.Core.Services;
using AnimeTracker.Web.Auth;
using AnimeTracker.Web.Hosting;
using Elyspio.Utils.Telemetry;
using Elyspio.Utils.Telemetry.MongoDB.Extensions;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Environment-specific configuration: a mounted secret in deployment, a gitignored file locally.
builder.Configuration.AddJsonFile("appsettings.docker.json", true, true);
builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);

// Telemetry is owned by Elyspio.Utils.Telemetry: Serilog on the console, plus traces, metrics and logs over OTLP
// when OTEL_EXPORTER_OTLP_ENDPOINT is set (Aspire locally, the chart in the cluster), so the same build works in both.
builder.AddElyspioTelemetry(options => options.Tracing = tracing => tracing.AddAppMongoInstrumentation());

builder.AddHostingDefaults();

builder.Services.AddSingleton(TimeProvider.System);

// One Add* per project, wired here and nowhere else.
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddAniListAdapter(builder.Configuration);
builder.Services.AddCrunchyrollAdapter(builder.Configuration);
builder.Services.AddAdnAdapter(builder.Configuration);
builder.Services.AddHangfireJobs(builder.Configuration);
builder.Services.AddCore();
builder.Services.AddAppAuth(builder.Configuration);

// Enums as names, not integers: the frontend compares against "Summer", "BingeableNow".
builder.Services.AddControllers()
	.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — the SPA is same-origin in production; only the Vite dev server needs an exception.
const string corsPolicy = "spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var origins = allowedOrigins.Append("http://localhost:5173").Distinct().ToArray();
builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
	policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();


app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

// The dashboard is browser-navigated, so a bearer token cannot reach it. Development keeps it
// open on the loopback interface; deployments put it behind the ingress, never on the public host.
if (app.Environment.IsDevelopment()) app.MapHangfireDashboard("/hangfire").AllowAnonymous();

// SPA fallback — any non-API route serves the React app from wwwroot.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();
