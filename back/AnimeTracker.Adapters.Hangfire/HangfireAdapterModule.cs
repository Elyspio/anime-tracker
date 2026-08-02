using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Adapters.Hangfire.Adapters;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace AnimeTracker.Api.Adapters.Hangfire;

public class HangfireAdapterModule: IDotnetModule
{
	public void Load(IServiceCollection services, IConfiguration configuration)
	{
		var mongoUrlBuilder = new MongoUrlBuilder(configuration["Database"]);
		var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

		services.AddHangfire(c => c
			.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
			.UseSimpleAssemblyNameTypeSerializer()
			.UseRecommendedSerializerSettings()
			.UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, new MongoStorageOptions
			{
				MigrationOptions = new MongoMigrationOptions
				{
					MigrationStrategy = new MigrateMongoMigrationStrategy(),
					BackupStrategy = new CollectionMongoBackupStrategy(),
				},
				Prefix = "hangfire",
				CheckConnection = true
			})
		);

		services.AddHangfireServer(serverOptions =>
		{
			serverOptions.ServerName = "anime-tracker";
		});

		services.Scan(scan => scan
			.FromAssemblyOf<HangfireAdapterModule>()
			.AddClasses(classes => classes.InNamespaceOf<HangfireHangfireJobAdapter>())
			.AsImplementedInterfaces()
			.WithSingletonLifetime()
		);
	}
}