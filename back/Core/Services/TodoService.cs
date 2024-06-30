using Example.Api.Abstractions.Common.Helpers;
using Example.Api.Abstractions.Common.Technical.Tracing;
using Example.Api.Abstractions.Interfaces.Repositories;
using Example.Api.Abstractions.Interfaces.Services;
using Example.Api.Abstractions.Models.Transports;
using Example.Api.Core.Assemblers;
using Microsoft.Extensions.Logging;

namespace Example.Api.Core.Services;

public class TodoService(ITodoRepository todoRepository, ILogger<TodoService> logger) : TracingService(logger), ITodoService
{
	private const string defaultUser = "public";
	private readonly TodoAssembler todoAssembler = new();

	public async Task<Todo> Add(string label)
	{
		using var _ = LogService(Log.F(label));

		var entity = await todoRepository.Add(label, defaultUser);
		var data = todoAssembler.Convert(entity);

		return data;
	}

	public async Task<Todo> AddForUser(string label, string user)
	{
		using var _ = LogService($"{Log.F(user)} {Log.F(label)}");

		var entity = await todoRepository.Add(label, user);
		var data = todoAssembler.Convert(entity);

		return data;
	}

	public async Task<List<Todo>> GetAllForUser(string user)
	{
		using var _ = LogService($"{Log.F(user)}");

		var entities = await todoRepository.GetAll(user);
		var data = todoAssembler.Convert(entities);

		return data;
	}

	public async Task<List<Todo>> GetAll()
	{
		using var _ = LogService();

		var entities = await todoRepository.GetAll(defaultUser);
		var data = todoAssembler.Convert(entities);

		return data;
	}

	public async Task<Todo> Check(Guid id)
	{
		using var _ = LogService(Log.F(id));

		var entity = await todoRepository.Check(id, defaultUser);
		var data = todoAssembler.Convert(entity);

		return data;
	}

	public async Task<Todo> CheckForUser(Guid id, string user)
	{
		using var _ = LogService($"{Log.F(user)} {Log.F(id)}");

		var entity = await todoRepository.Check(id, user);
		var data = todoAssembler.Convert(entity);

		return data;
	}

	public async Task Delete(Guid id)
	{
		using var _ = LogService(Log.F(id));

		await todoRepository.Delete(id, defaultUser);
	}

	public async Task DeleteForUser(Guid id, string user)
	{
		using var _ = LogService(Log.F(id));

		await todoRepository.Delete(id, user);
	}
}