using AnimeTracker.Api.Adapters.Mongo.Repositories.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using AnimeTracker.Api.Abstractions.Common.Extensions;
using AnimeTracker.Api.Abstractions.Interfaces.Repositories;
using AnimeTracker.Api.Abstractions.Models.Base;
using AnimeTracker.Api.Abstractions.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace AnimeTracker.Api.Adapters.Mongo.Repositories;

internal class TodoRepository(IConfiguration configuration, ILogger<BaseRepository<TodoEntity>> logger) : CrudRepository<TodoEntity, TodoBase>(configuration, logger),
	ITodoRepository
{
	public async Task<TodoEntity> Add(string label, string user)
	{
		using var logger = LogAdapter($"{Log.F(user)} {Log.F(label)}", autoExit: false);

		var entity = new TodoEntity
		{
			Checked = false,
			Label = label,
			User = user
		};
		await EntityCollection.InsertOneAsync(entity);

		logger.Exit($"{Log.F(entity.Id)}");

		return entity;
	}

	public async Task<List<TodoEntity>> GetAll(string user)
	{
		using var logger = LogAdapter($"{Log.F(user)}");

		return await EntityCollection.AsQueryable().Where(x => x.User == user).ToListAsync();
	}

	public async Task<TodoEntity> Check(Guid id, string user)
	{
		using var logger = LogAdapter($"{Log.F(id)} {Log.F(user)}");

		var entity = await EntityCollection.AsQueryable().FirstAsync(todo => todo.Id == id.AsObjectId());

		entity.Checked = !entity.Checked;

		await EntityCollection.FindOneAndReplaceAsync(e => e.Id == id.AsObjectId(), entity);

		return entity;
	}

	public async Task Delete(Guid id, string user)
	{
		using var logger = LogAdapter($"{Log.F(id)} {Log.F(user)}");

		await EntityCollection.FindOneAndDeleteAsync(todo => todo.User == user && todo.Id == id.AsObjectId());
	}

	public async Task<bool> Own(ObjectId id, string user)
	{
		using var logger = LogAdapter($"{Log.F(id)} {Log.F(user)}");

		var todo = await GetById(id);

		return todo?.User == user;
	}
}