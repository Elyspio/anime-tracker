using MongoDB.Bson;

namespace Example.Api.Abstractions.Common.Extensions;

/// <summary>
///     BsonExtension is a static class that provides extension methods for ObjectId and Guid types.
/// </summary>
public static class BsonExtension
{
	/// <summary>
	///     Transforms an ObjectId into a Guid
	/// </summary>
	/// <param name="oid">The ObjectId to be converted.</param>
	/// <returns>Guid representation of the ObjectId.</returns>
	public static Guid AsGuid(this ObjectId oid)
	{
		var bytes = oid.ToByteArray().Concat(new byte[]
		{
			5, 5, 5, 5
		}).ToArray();
		var gid = new Guid(bytes);
		return gid;
	}

	/// <summary>
	///     Transforms a Guid into an ObjectId. Used to convert a Guid that was once an ObjectId
	/// </summary>
	/// <param name="gid">The Guid to be converted.</param>
	/// <returns>ObjectId representation of the Guid.</returns>
	public static ObjectId AsObjectId(this Guid gid)
	{
		var bytes = gid.ToByteArray().Take(12).ToArray();
		var oid = new ObjectId(bytes);
		return oid;
	}
}