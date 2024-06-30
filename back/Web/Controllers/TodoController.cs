using Example.Api.Abstractions.Common.Helpers;
using Example.Api.Abstractions.Common.Technical.Tracing;
using Example.Api.Abstractions.Interfaces.Services;
using Example.Api.Abstractions.Models.Transports;
using Example.Api.Adapters.Rest.AuthenticationApi;
using Example.Api.Web.Technical.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Web.Controllers;

[Route("api/todo")]
[ApiController]
public class TodoController(ITodoService todoService, ILogger<TodoController> logger) : TracingController(logger)
{
	[HttpGet]
	[ProducesResponseType(typeof(List<Todo>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll()
	{
		using var _ = LogController();
		return Ok(await todoService.GetAll());
	}

	[HttpPut("{id:guid}/toggle")]
	[ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
	public async Task<IActionResult> Check(Guid id)
	{
		using var _ = LogController($"{Log.F(id)}");
		return Ok(await todoService.Check(id));
	}


	[Authorize(AuthenticationRoles.Admin)]
	[HttpPost]
	[ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
	public async Task<IActionResult> Add([FromBody] string label)
	{
		using var _ = LogController($"{Log.F(label)}");
		return Ok(await todoService.Add(label));
	}

	[Authorize(AuthenticationRoles.User)]
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(Guid id)
	{
		using var _ = LogController($"{Log.F(id)}");
		await todoService.Delete(id);
		return NoContent();
	}
}