using System.Net;

namespace AnimeTracker.Api.Adapters.Rest.Utils.Clients;

public sealed class ClientSideRateLimitedHandler: DelegatingHandler
{

	private const int MaxRetries = 30;

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var retries = 0;
		while (retries++ < MaxRetries)
		{
			var resp = await base.SendAsync(request, cancellationToken);

			if (resp.StatusCode != HttpStatusCode.TooManyRequests) return resp;

			var delay = GetRetryAfter();
			await Task.Delay(delay, cancellationToken);
		}

		return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
	}

	private int GetRetryAfter()
	{
		return Random.Shared.Next(1000, 10_000);
	}

}