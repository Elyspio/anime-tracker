using System.Diagnostics;
using Example.Api.Abstractions.Common.Helpers;
using Example.Api.Abstractions.Common.Technical.Tracing;
using Example.Api.Abstractions.Interfaces.Services;
using Example.Api.Adapters.Rest.AuthenticationApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Example.Api.Web.Technical.Filters;

/// <summary>
///     Add an authorization filter on <see cref="AuthenticationRoles" />
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthorizeAttribute : TypeFilterAttribute
{
	/// <inheritdoc />
	public AuthorizeAttribute(AuthenticationRoles role) : base(typeof(CustomAuthorizeFilter))
	{
		Arguments = new object[]
		{
			role
		};
	}
}

/// Implementation of
/// <see cref="AuthorizeAttribute" />
/// with dependency injection
public sealed class CustomAuthorizeFilter(AuthenticationRoles role, ILogger<CustomAuthorizeFilter> logger) : TracingAttribute, IAuthorizationFilter
{
	/// <inheritdoc />
	public override ILogger Logger { get; set; } = logger;

	/// <inheritdoc />
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var bearer = context.HttpContext.Request.Headers.Authorization.ToString();


		using var logger = LogAttribute($"path={context.HttpContext.Request.Path} {Log.F(bearer.Length)}");

		var svc = context.HttpContext.RequestServices;
		var tokenService = svc.GetRequiredService<IAuthenticationService>();

		// skip authorization if action is decorated with [AllowAnonymous] attribute
		var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
		if (allowAnonymous)
			return;


		if (!tokenService.ValidateJwt(bearer, out var token))
		{
			context.Result = new JsonResult(new
			{
				status = "Unauthorized"
			})
			{
				StatusCode = StatusCodes.Status401Unauthorized
			};
			logger.Activity?.SetStatus(ActivityStatusCode.Error);
			logger.Activity?.SetCustomProperty("result", context.Result);
			return;
		}


		var userStr = token!.Payload["data"].ToString()!;

		var user = JsonConvert.DeserializeObject<User>(userStr)!;

		context.HttpContext.Items["user"] = user;

		if (user.Authorizations.Authentication?.Roles.Contains(role) == true) return;

		context.Result = new JsonResult(new
		{
			status = "Forbidden",
			missingRole = role
		})
		{
			StatusCode = StatusCodes.Status403Forbidden
		};
		logger.Activity?.SetStatus(ActivityStatusCode.Error);
		logger.Activity?.SetCustomProperty("result", context.Result);
	}
}