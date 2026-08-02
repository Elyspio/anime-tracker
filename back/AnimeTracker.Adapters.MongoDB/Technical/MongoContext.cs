using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace AnimeTracker.Adapters.MongoDB.Technical;

/// <summary>
///     BSON conventions for the whole process. Registered once, before any collection is resolved:
///     enums are stored as names so a reordered enum cannot silently reinterpret stored documents.
/// </summary>
public static class MongoMappings
{
	private static bool _registered;

	public static void Register()
	{
		if (_registered) return;
		_registered = true;

		ConventionRegistry.Register("EnumStringConvention", new ConventionPack
		{
			new EnumRepresentationConvention(BsonType.String)
		}, _ => true);

		BsonSerializer.RegisterSerializationProvider(new EnumAsStringSerializationProvider());
	}
}
