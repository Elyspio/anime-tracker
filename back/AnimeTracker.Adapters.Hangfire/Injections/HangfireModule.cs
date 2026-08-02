using AnimeTracker.Abstractions.Interfaces.Adapters;
using AnimeTracker.Adapters.Hangfire.Adapters;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.Hangfire.Injections;

public static class HangfireModule
{
	public static IServiceCollection AddHangfireJobs(this IServiceCollection services, IConfiguration config)
	{
		var connectionString = config.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
		var url = new MongoUrlBuilder(connectionString);
		var client = new MongoClient(url.ToMongoUrl());

		services.AddHangfire(configuration => configuration
			.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
			.UseSimpleAssemblyNameTypeSerializer()
			.UseRecommendedSerializerSettings()
			.UseMongoStorage(client, url.DatabaseName, new MongoStorageOptions
			{
				MigrationOptions = new MongoMigrationOptions
				{
					MigrationStrategy = new MigrateMongoMigrationStrategy(),
					BackupStrategy = new CollectionMongoBackupStrategy()
				},
				Prefix = "hangfire",
				CheckConnection = true
			}));

		// The season refresh walks Nautiljon one page at a time behind a single Cloudflare solver.
		// A second worker would double the request rate without finishing any sooner.
		services.AddHangfireServer(options =>
		{
			options.ServerName = "anime-tracker";
			options.WorkerCount = 1;
		});

		services.AddSingleton<IHangfireJobAdapter, HangfireJobAdapter>();

		return services;
	}
}
