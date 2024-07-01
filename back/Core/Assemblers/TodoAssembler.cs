using AnimeTracker.Api.Abstractions.Common.Assemblers;
using AnimeTracker.Api.Abstractions.Common.Extensions;
using AnimeTracker.Api.Abstractions.Models.Entities;
using AnimeTracker.Api.Abstractions.Models.Transports;

namespace AnimeTracker.Api.Core.Assemblers;

public class TodoAssembler : BaseAssembler<Todo, TodoEntity>
{
	public override Todo Convert(TodoEntity obj)
	{
		return new Todo
		{
			Checked = obj.Checked,
			Id = obj.Id.AsGuid(),
			Label = obj.Label,
			User = obj.User
		};
	}

	public override TodoEntity Convert(Todo obj)
	{
		return new TodoEntity
		{
			Checked = obj.Checked,
			Id = obj.Id.AsObjectId(),
			Label = obj.Label,
			User = obj.User
		};
	}
}