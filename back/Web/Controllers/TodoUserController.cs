using Example.Api.Abstractions.Common.Helpers;
using Example.Api.Abstractions.Common.Technical.Tracing;
using Example.Api.Abstractions.Interfaces.Services;
using Example.Api.Abstractions.Models.Transports;
using Example.Api.Adapters.Rest.AuthenticationApi;
using Example.Api.Web.Technical.Extensions;
using Example.Api.Web.Technical.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Web.Controllers;

[Route("api/todo/user")]
[ApiController]
[Authorize(AuthenticationRoles.User)]
public class TodoUserController(ITodoService todoService, ILogger<TodoUserController> logger) : TracingController(logger)
{
	private string Username => Request.GetUsername();


	[HttpDelete("{id:guid}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
	public async Task<IActionResult> DeleteForUser(Guid id)
	{
		using var _ = LogController($"{Log.F(id)} {Log.F(Username)}");
		await todoService.DeleteForUser(id, Username);
		return NoContent();
	}

	[HttpPost]
	[ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
	public async Task<IActionResult> AddForUser([FromBody] string label)
	{
		using var _ = LogController($"{Log.F(label)} {Log.F(Username)}");
		var todo = await todoService.AddForUser(label, Username);
		return Created($"/{todo.Id}", todo);
	}


	[HttpGet]
	[ProducesResponseType(typeof(List<Todo>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAllForUser()
	{
		using var _ = LogController($"{Log.F(Username)}");
		return Ok(await todoService.GetAllForUser(Username));
	}


	[HttpPut("{id:guid}/toggle")]
	[ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
	public async Task<IActionResult> CheckForUser(Guid id)
	{
		using var _ = LogController($"{Log.F(id)} {Log.F(Username)}");
		return Ok(await todoService.CheckForUser(id, Username));
	}
}