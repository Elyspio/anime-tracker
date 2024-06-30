using Example.Api.Abstractions.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Example.Api.Web.Technical.Filters;

/// <inheritdoc />
/// , Intercepts
/// <see cref="HttpException" />
/// to returns HTTP codes from exceptions
public sealed class HttpExceptionFilter : ExceptionFilterAttribute
{
	/// <inheritdoc />
	public override void OnException(ExceptionContext context)
	{
		if (context.Exception is HttpException ex)
			context.Result = new ObjectResult(ex.ToString())
			{
				StatusCode = (int)ex.Code,
				Value = ex.ToString()
			};
		else
			context.Result = new ObjectResult(context.Exception.ToString())
			{
				StatusCode = 500,
				Value = context.Exception.ToString()
			};

		Console.WriteLine(context.Exception);

		base.OnException(context);
	}
}