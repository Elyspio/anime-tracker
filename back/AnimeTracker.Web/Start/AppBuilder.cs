using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Adapters.Hangfire;
using AnimeTracker.Api.Adapters.MassTransit;
using AnimeTracker.Api.Adapters.Mongo.Injections;
using AnimeTracker.Api.Adapters.Rest.Injections;
using AnimeTracker.Api.Core;
using AnimeTracker.Api.Web.Technical.Extensions;

namespace AnimeTracker.Api.Web.Start;

/// <summary>
///     Application builder
/// </summary>
public sealed class AppBuilder
{
	/// <summary>
	///     Create builder from command args
	/// </summary>
	/// <param name="args"></param>
	public AppBuilder(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration.AddJsonFile("appsettings.docker.json", true, true);

		builder.Services.AddModule<CoreModule>(builder.Configuration);

		builder.Services.AddModule<MongoAdapterModule>(builder.Configuration);
		builder.Services.AddModule<RestAdapterModule>(builder.Configuration);
		builder.Services.AddModule<HangfireAdapterModule>(builder.Configuration);
		builder.Services.AddModule<MassTransitAdapterModule>(builder.Configuration);

		builder.Host.AddLogging();

		builder.Services
			.AddAppControllers()
			.AddAppSignalR()
			.AddAppSwagger()
			.AddAppOpenTelemetry(builder.Configuration);


		if (builder.Environment.IsDevelopment()) builder.Services.SetupDevelopmentCors();

		Application = builder.Build();
	}

	/// <summary>
	///     Built application
	/// </summary>
	public WebApplication Application { get; }
}