using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using AnimeTracker.Api.Abstractions.Interfaces.Services;
using AnimeTracker.Api.Abstractions.Models.Transports;
using AnimeTracker.Api.Web.Technical.Filters;
using Example.Api.Adapters.Rest.AuthenticationApi;
using Microsoft.AspNetCore.Mvc;

namespace AnimeTracker.Api.Web.Controllers;

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