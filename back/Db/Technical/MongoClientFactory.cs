using Elyspio.Utils.Telemetry.MongoDB.Business;
using MongoDB.Driver;

namespace Example.Api.Adapters.Mongo.Technical;

/// <summary>
///     Manage mongo client
/// </summary>
public static class MongoClientFactory
{
	/// <summary>
	///     Create mongo client with telemetry support
	/// </summary>
	/// <param name="connectionString"></param>
	/// <returns></returns>
	public static (MongoClient Client, MongoUrl Url) Create(string connectionString)
	{
		var mongoUrl = new MongoUrl(connectionString);
		var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
		clientSettings.ClusterConfigurator = cb => cb.Subscribe(new MongoDbActivityEventSubscriber());

		return (new MongoClient(clientSettings), mongoUrl);
	}
}