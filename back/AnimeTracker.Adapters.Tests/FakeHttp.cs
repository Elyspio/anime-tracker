using System.Net;
using System.Text;

namespace AnimeTracker.Adapters.Tests;

/// <summary>One request captured on its way to the wire.</summary>
public sealed record CapturedRequest(HttpMethod Method, string Url);

/// <summary>
///     Records every request and answers from a canned routing table, so the adapter can be tested
///     against recorded pages without reaching Nautiljon or a FlareSolverr instance.
/// </summary>
public sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
	public List<CapturedRequest> Requests { get; } = [];

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// AbsoluteUri, not ToString(): it is the escaped form that actually goes on the wire.
		Requests.Add(new CapturedRequest(request.Method, request.RequestUri?.AbsoluteUri ?? ""));

		return Task.FromResult(responder(request));
	}

	public static HttpResponseMessage Html(string html, HttpStatusCode status = HttpStatusCode.OK)
	{
		return new HttpResponseMessage(status) { Content = new StringContent(html, Encoding.UTF8, "text/html") };
	}
}

/// <summary>Hands the adapter the fake handler through the factory it expects.</summary>
public sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
	public HttpClient CreateClient(string name)
	{
		return new HttpClient(handler, false);
	}
}
