using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Models.Base.Dub;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AnimeTracker.Abstractions.Models.Entities;

public class DubOverrideEntity : DubOverrideBase, IEntity
{
	// Left unset when saving, so a replacement never carries an _id: the stored document keeps its
	// own, and an upsert gets a fresh one. An empty ObjectId written as _id would collide instead.
	[BsonId]
	[BsonIgnoreIfDefault]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; }
}
