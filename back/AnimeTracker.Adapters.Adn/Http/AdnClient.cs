using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Adapters.Adn.Configs;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimeTracker.Adapters.Adn.Http;

/// <summary>
///     Calls ADN's public gateway. No token and no anti-bot to satisfy, but the same discipline as
///     Crunchyroll: one request at a time, spaced out, and anything that is not an answer about the
///     catalogue — a refusal, an outage, the proxy down — reported as the platform being unavailable.
/// </summary>
internal class AdnClient(
	IHttpClientFactory httpClientFactory,
	IOptions<AdnOptions> options,
	TimeProvider timeProvider,
	ILogger<AdnClient> logger
) : TracingAdapter(logger)
{
	public const string ClientName = "Adn";

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	private readonly SemaphoreSlim _pace = new(1, 1);

	private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

	/// <summary>The decoded reply, or null when ADN does not know the resource.</summary>
	public async Task<T?> Get<T>(string path, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default)
		where T : class
	{
		using var trace = LogAdapter($"{Log.F(path)}");

		var builder = new UriBuilder(new Uri(new Uri(options.Value.BaseUrl), path))
		{
			Query = string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))
		};

		using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
		request.Headers.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);
		request.Headers.TryAddWithoutValidation("Accept", "application/json");
		request.Headers.TryAddWithoutValidation("X-Target-Distribution", options.Value.Distribution);

		using var response = await Send(request, cancellationToken);

		if (response.StatusCode == HttpStatusCode.NotFound) return null;

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
			?? throw new HttpRequestException($"ADN returned an empty reply for {path}.");
	}

	private async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		await _pace.WaitAsync(cancellationToken);

		HttpResponseMessage response;

		try
		{
			var wait = _lastRequest + TimeSpan.FromMilliseconds(options.Value.RequestDelayMs) - timeProvider.GetUtcNow();
			if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);

			using var client = httpClientFactory.CreateClient(ClientName);
			response = await client.SendAsync(request, cancellationToken);
		}
		catch (HttpRequestException exception)
		{
			throw new DubPlatformUnavailableException(DubPlatform.Adn, $"ADN could not be reached: {exception.Message}", exception);
		}
		catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
		{
			throw new DubPlatformUnavailableException(DubPlatform.Adn, "ADN did not answer in time.", exception);
		}
		finally
		{
			_lastRequest = timeProvider.GetUtcNow();
			_pace.Release();
		}

		var refusal = response.StatusCode switch
		{
			// ADN answers a territory it does not serve with 403; that is no statement about a show.
			HttpStatusCode.Forbidden => "ADN refused the request (403).",
			HttpStatusCode.TooManyRequests => "ADN is rate limiting the requests (429).",
			>= HttpStatusCode.InternalServerError => $"ADN is failing ({(int)response.StatusCode}).",
			_ => null
		};

		if (refusal is null) return response;

		response.Dispose();
		throw new DubPlatformUnavailableException(DubPlatform.Adn, refusal);
	}
}
