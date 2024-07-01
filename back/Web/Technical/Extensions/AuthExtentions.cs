using Example.Api.Adapters.Rest.AuthenticationApi;

namespace AnimeTracker.Api.Web.Technical.Extensions;

public static class AuthExtentions
{
	public static User? GetUser(this HttpRequest request)
	{
		return (User?)request.HttpContext.Items["user"];
	}

	public static string? GetUsername(this HttpRequest request)
	{
		return GetUser(request)?.Username;
	}

	public static string? GetToken(this HttpRequest request)
	{
		return request.Headers.Authorization;
	}
}