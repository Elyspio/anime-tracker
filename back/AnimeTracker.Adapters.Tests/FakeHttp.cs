using System.Net;
using System.Text;
using System.Text.Json;

namespace AnimeTracker.Adapters.Tests;

/// <summary>One request captured on its way to the wire.</summary>
public sealed record CapturedRequest(HttpMethod Method, string Url, string Body);

/// <summary>
///     Records every request and answers from a canned routing table, so the adapter can be tested
///     against recorded pages without reaching Nautiljon or a FlareSolverr instance.
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

	/// <summary>A FlareSolverr envelope carrying a rendered page.</summary>
	public static HttpResponseMessage Solved(string html, int siteStatus = 200)
	{
		return Envelope(new
		{
			status = "ok",
			message = "",
			solution = new { status = siteStatus, response = html }
		});
	}

	/// <summary>A FlareSolverr envelope reporting that it could not fetch anything.</summary>
	public static HttpResponseMessage SolverError(string message)
	{
		return Envelope(new { status = "error", message, solution = (object?)null });
	}

	private static HttpResponseMessage Envelope(object payload)
	{
		return new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
		};
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
