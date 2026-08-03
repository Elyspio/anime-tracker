using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace AnimeTracker.Adapters.MongoDB.Technical;

/// <summary>
///     BSON conventions for the whole process. Registered once, before any collection is resolved:
///     enums are stored as names so a reordered enum cannot silently reinterpret stored documents,
///     and Guids get an explicit representation the driver no longer assumes for them.
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

		// Driver 3.x has no default Guid representation: writing one without saying which layout to
		// use throws rather than guessing, because the legacy layouts each byte-order the value
		// differently and picking wrong is silent corruption. Standard is RFC 4122 / subtype 4 —
		// the interoperable one, and the only sensible choice for a store with no legacy documents.
		BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
		BsonSerializer.RegisterSerializer(new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));
	}
}
