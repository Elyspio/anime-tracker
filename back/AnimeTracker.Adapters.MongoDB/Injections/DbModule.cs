using AnimeTracker.Abstractions.Interfaces.Repositories;
using AnimeTracker.Adapters.MongoDB.Repositories;
using AnimeTracker.Adapters.MongoDB.Technical;
using Elyspio.Utils.Telemetry.MongoDB.Business;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace AnimeTracker.Adapters.MongoDB.Injections;

public static class DbModule
{
	public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
	{
		MongoMappings.Register();

		var connectionString = config.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
		var url = MongoUrl.Create(connectionString);
		var databaseName = !string.IsNullOrWhiteSpace(url.DatabaseName)
			? url.DatabaseName
			: config["Mongo:Database"] ?? "anime-tracker";

		services.AddSingleton<IMongoClient>(_ =>
		{
			var settings = MongoClientSettings.FromUrl(url);
			settings.ClusterConfigurator = builder => builder.Subscribe(new MongoDbActivityEventSubscriber());
			return new MongoClient(settings);
		});

		services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

		services.AddSingleton<IAnimeRepository, AnimeRepository>();
		services.AddSingleton<IRefreshRunRepository, RefreshRunRepository>();

		return services;
	}
}
