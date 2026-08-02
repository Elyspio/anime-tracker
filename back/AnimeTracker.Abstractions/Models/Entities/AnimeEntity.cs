using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Models.Base.Anime;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AnimeTracker.Abstractions.Models.Entities;

public class AnimeEntity : AnimeBase, IEntity
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; }
}