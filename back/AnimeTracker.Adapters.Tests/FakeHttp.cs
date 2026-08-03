using System.Net;
using System.Text;

namespace AnimeTracker.Adapters.Tests;

/// <summary>One request captured on its way to the wire.</summary>
public sealed record CapturedRequest(HttpMethod Method, string Url, string Body);

/// <summary>
///     Records every request and answers from a canned routing table, so the adapter can be tested
///     against recorded replies without reaching AniList.
/// </summary>
public sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
	public List<CapturedRequest> Requests { get; } = [];

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);

		// AbsoluteUri, not ToString(): it is the escaped form that actually goes on the wire.
		Requests.Add(new CapturedRequest(request.Method, request.RequestUri?.AbsoluteUri ?? "", body));

		return responder(request);
	}

	public static HttpResponseMessage Json(string payload, HttpStatusCode status = HttpStatusCode.OK)
	{
		return new HttpResponseMessage(status)
		{
			Content = new StringContent(payload, Encoding.UTF8, "application/json")
		};
	}

	/// <summary>A page carrying no media and declaring itself the last one.</summary>
	public static HttpResponseMessage EmptyPage()
	{
		return Json("""{"data":{"Page":{"pageInfo":{"hasNextPage":false},"media":[]}}}""");
	}

	/// <summary>
	///     GraphQL reports failure inside a 200, so this is what a rejected query actually looks
	///     like on the wire.
	/// </summary>
	public static HttpResponseMessage GraphQlError(string message)
	{
		return Json($$"""{"errors":[{"message":{{System.Text.Json.JsonSerializer.Serialize(message)}}}],"data":null}""");
	}
}

/// <summary>Hands the client the fake handler through the factory it expects.</summary>
public sealed class FakeHttpClientFactory(HttpMessageHandler handler, Uri? baseAddress = null) : IHttpClientFactory
{
	public HttpClient CreateClient(string name)
	{
		return new HttpClient(handler, false) { BaseAddress = baseAddress };
	}
}
