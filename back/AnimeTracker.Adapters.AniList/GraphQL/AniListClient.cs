using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimeTracker.Adapters.AniList.Configs;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimeTracker.Adapters.AniList.GraphQL;

/// <summary>
///     Posts a GraphQL document to AniList and hands back the typed payload.
///     <para>
///         The API is public, needs no credentials, and allows 30 requests a minute — a full season
///         costs two. The only failure worth handling specially is a 429, which arrives with a
///         Retry-After the server means literally.
///     </para>
/// </summary>
internal class AniListClient(IHttpClientFactory httpClientFactory, IOptions<AniListOptions> options, ILogger<AniListClient> logger)
	: TracingAdapter(logger)
{
	public const string ClientName = "AniList";

	private const int MaxRateLimitRetries = 3;

	private static readonly TimeSpan DefaultBackoff = TimeSpan.FromSeconds(60);

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	public async Task<T> Query<T>(string document, object variables, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter();

		for (var attempt = 1; ; attempt++)
		{
			using var client = httpClientFactory.CreateClient(ClientName);

			var response = await client.PostAsJsonAsync(options.Value.Endpoint,
				new { query = document, variables }, Json, cancellationToken);

			if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt <= MaxRateLimitRetries)
			{
				var wait = response.Headers.RetryAfter?.Delta
					?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow)
					?? DefaultBackoff;

				_logger.LogWarning("AniList rate limited the request, retrying in {Backoff}", wait);
				await Task.Delay(wait > TimeSpan.Zero ? wait : DefaultBackoff, cancellationToken);
				continue;
			}

			response.EnsureSuccessStatusCode();

			var body = await response.Content.ReadFromJsonAsync<GraphQlResponse<T>>(Json, cancellationToken);

			// GraphQL reports failure inside a 200. Reading only the status would hand back an empty
			// season and call it a success.
			if (body?.Errors is { Count: > 0 } errors)
				throw new HttpRequestException($"AniList rejected the query: {string.Join("; ", errors.Select(error => error.Message))}");

			if (body is null || body.Data is null)
				throw new HttpRequestException("AniList returned an empty response.");

			return body.Data;
		}
	}
}
