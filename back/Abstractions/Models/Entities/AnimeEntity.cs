﻿using AnimeTracker.Api.Abstractions.Interfaces.Business;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AnimeTracker.Api.Abstractions.Models.Entities;

public class AnimeEntity : AnimeBase, IEntity
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; }
}