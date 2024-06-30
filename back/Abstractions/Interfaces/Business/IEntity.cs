using MongoDB.Bson;

namespace Example.Api.Abstractions.Interfaces.Business;

/// <summary>
///     IEntity interface that represents an entity with a unique identifier.
/// </summary>
public interface IEntity
{
	/// <summary>
	///     The ObjectId unique identifier of an entity.
	/// </summary>
	public ObjectId Id { get; set; }
}