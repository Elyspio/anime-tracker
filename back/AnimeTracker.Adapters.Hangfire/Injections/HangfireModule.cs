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
				CheckConnection = true,
				// Hangfire.Mongo watches a change stream by default, which only exists on a replica
				// set. Tailing the capped notifications collection is just as immediate and works
				// on a standalone server too, so where this is deployed stays an operational choice
				// rather than something the scheduler dictates.
				CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
			}));

		// A season refresh is idempotent and cheap to trigger again by hand. Hangfire's default of ten
		// automatic attempts would replay a failing job ten times over without anyone asking.
		GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

		// Two workers so a second season does not have to wait out the first. Each refresh is a
		// single API call, so this decides nothing about how hard the source is hit.
		services.AddHangfireServer(options =>
		{
			options.ServerName = "anime-tracker";
			options.WorkerCount = 2;
		});

		services.AddSingleton<IHangfireJobAdapter, HangfireJobAdapter>();

		return services;
	}
}
