using AnimeTracker.Abstractions.Interfaces.Business;
using MongoDB.Bson;

namespace AnimeTracker.Abstractions.Interfaces.Repositories;

/// <summary>Generic CRUD repository for entities implementing <see cref="IEntity" />.</summary>
/// <typeparam name="TEntity">Stored entity.</typeparam>
/// <typeparam name="TBase">Type used to create or update <typeparamref name="TEntity" />.</typeparam>
public interface ICrudRepository<TEntity, in TBase> where TEntity : IEntity
{
	Task<TEntity[]> Add(IReadOnlyCollection<TBase> bases, CancellationToken cancellationToken = default);

	Task<TEntity> Replace(ObjectId id, TBase @base, CancellationToken cancellationToken = default);

	Task<List<TEntity>> GetAll(CancellationToken cancellationToken = default);

	Task Delete(ObjectId id, CancellationToken cancellationToken = default);

	Task<TEntity?> GetById(ObjectId id, CancellationToken cancellationToken = default);
}
