using System.Text.Json.Serialization;
using AnimeTracker.Adapters.Hangfire.Adapters;
using AnimeTracker.Adapters.Hangfire.Injections;
using AnimeTracker.Adapters.MongoDB.Injections;
using AnimeTracker.Adapters.MongoDB.Technical;
using AnimeTracker.Adapters.Nautijon.Adapters;
using AnimeTracker.Adapters.Nautijon.Injections;
using AnimeTracker.Core.Injections;
using AnimeTracker.Core.Services;
using AnimeTracker.Web.Auth;
using AnimeTracker.Web.Filters;
using AnimeTracker.Web.Hosting;
using Elyspio.Utils.Telemetry.Technical.Extensions;
using Elyspio.Utils.Telemetry.Tracing.Builder;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Environment-specific configuration: a mounted secret in deployment, a gitignored file locally.
builder.Configuration.AddJsonFile("appsettings.docker.json", true, true);
builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);

builder.Host.UseSerilogWithTelemetry();

// Telemetry is owned by Elyspio.Utils.Telemetry: it registers every TracingX activity source found
// in the product assemblies. Under Aspire, OTEL_EXPORTER_OTLP_ENDPOINT takes precedence over
// OpenTelemetry:CollectorUri, so the same build works locally and in the cluster.
if (builder.Configuration.IsTelemetryEnabled(out var telemetryOptions))
{
	var telemetry = new AppOpenTelemetryBuilder<Program>(telemetryOptions!, builder.Configuration);
	telemetry.AddAssembly<AnimeService>();
	telemetry.AddAssembly<NautijonAdapter>();
	telemetry.AddAssembly<EnumAsStringSerializationProvider>();
	telemetry.AddAssembly<HangfireJobAdapter>();
	telemetry.Build(builder.Services);
	builder.Services.AddOpenTelemetryJsonConfiguration(builder.Configuration);
}

builder.AddHostingDefaults();

builder.Services.AddSingleton(TimeProvider.System);

// One Add* per project, wired here and nowhere else.
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddNautijonAdapter(builder.Configuration);
builder.Services.AddHangfireJobs(builder.Configuration);
builder.Services.AddCore();
builder.Services.AddAppAuth(builder.Configuration);

// Enums as names, not integers: the frontend compares against "Summer", "BingeableNow".
builder.Services.AddControllers(options => options.Filters.Add<HttpExceptionFilter>())
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

app.UseOpenTelemetryJsonConfiguration();

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
