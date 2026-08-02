using AnimeTracker.Abstractions.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AnimeTracker.Web.Filters;

/// <summary>Maps domain failures to their status code, so controllers can just throw.</summary>
public sealed class HttpExceptionFilter : IExceptionFilter
{
	public void OnException(ExceptionContext context)
	{
		if (context.Exception is not HttpException exception) return;

		context.Result = new ObjectResult(new ProblemDetails
		{
			Status = (int)exception.Code,
			Title = exception.Message
		})
		{
			StatusCode = (int)exception.Code
		};

		context.ExceptionHandled = true;
	}
}
