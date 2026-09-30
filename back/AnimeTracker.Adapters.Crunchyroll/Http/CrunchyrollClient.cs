using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnimeTracker.Abstractions.Exceptions;
using AnimeTracker.Abstractions.Models.Base.Dub;
using AnimeTracker.Adapters.Crunchyroll.Configs;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimeTracker.Adapters.Crunchyroll.Http;

/// <summary>
///     Calls the API the Crunchyroll website calls, with the anonymous token any visitor is given.
///     <para>
///         Cloudflare stands in front of it and decides by how the client looks, not by what it asks.
///         Measured from .NET 10 on Linux: HTTP/2 is challenged and HTTP/1.1 is not, the default .NET user
///         agent is challenged and a browser's is not, and a resumed TLS session is challenged (the handler
///         turns resumption off, see the module). From Windows every request is challenged whatever the
///         settings — the TLS handshake gives it away — so the sync only works from a Linux host, which the
///         deployment is. A challenge is reported as the platform being unavailable: the caller keeps what
///         it knew instead of reading a refusal as an empty catalogue.
///     </para>
///     <para>
///         One request at a time, spaced out: a season is a few hundred of them, and getting the egress
///         address banned is the one outcome all of this exists to avoid.
///     </para>
/// </summary>
internal partial class CrunchyrollClient(
	IHttpClientFactory httpClientFactory,
	IOptions<CrunchyrollOptions> options,
	TimeProvider timeProvider,
	ILogger<CrunchyrollClient> logger
) : TracingAdapter(logger)
{
	public const string ClientName = "Crunchyroll";

	/// <summary>A token is renewed this long before it says it expires, so it never lapses mid-request.</summary>
	private static readonly TimeSpan TokenMargin = TimeSpan.FromMinutes(1);

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	private readonly SemaphoreSlim _pace = new(1, 1);

	private readonly SemaphoreSlim _tokenGate = new(1, 1);

	private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

	private string? _token;

	private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

	/// <summary>The decoded reply, or null when Crunchyroll does not know the resource.</summary>
	public async Task<T?> Get<T>(string path, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default)
		where T : class
	{
		using var trace = LogAdapter($"{Log.F(path)}");

		var url = Url(path, query);

		var response = await Send(await Authorized(url, cancellationToken), cancellationToken);

		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			// A token can be revoked before its expiry says so: one fresh one, one retry.
			response.Dispose();
			_token = null;
			response = await Send(await Authorized(url, cancellationToken), cancellationToken);
		}

		using (response)
		{
			if (response.StatusCode == HttpStatusCode.NotFound) return null;

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
				?? throw new HttpRequestException($"Crunchyroll returned an empty reply for {path}.");
		}
	}

	private async Task<HttpRequestMessage> Authorized(Uri url, CancellationToken cancellationToken)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token(cancellationToken));

		return request;
	}

	private async Task<string> Token(CancellationToken cancellationToken)
	{
		if (_token is { } current && timeProvider.GetUtcNow() < _tokenExpiry) return current;

		await _tokenGate.WaitAsync(cancellationToken);

		try
		{
			if (_token is { } renewed && timeProvider.GetUtcNow() < _tokenExpiry) return renewed;

			var clientId = await ClientId(cancellationToken);

			using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.Value.BaseUrl), "/auth/v1/token"))
			{
				Content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["grant_type"] = "client_id",
					["scope"] = "offline_access"
				})
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:")));

			using var response = await Send(request, cancellationToken);
			response.EnsureSuccessStatusCode();

			var token = await response.Content.ReadFromJsonAsync<TokenReply>(Json, cancellationToken);
			if (token?.AccessToken is not { Length: > 0 } accessToken)
				throw new DubPlatformUnavailableException(DubPlatform.Crunchyroll, "Crunchyroll gave no anonymous token.");

			_token = accessToken;
			_tokenExpiry = timeProvider.GetUtcNow() + TimeSpan.FromSeconds(token.ExpiresIn ?? 300) - TokenMargin;

			return accessToken;
		}
		finally
		{
			_tokenGate.Release();
		}
	}

	/// <summary>
	///     The website's own client id, read from the configuration its home page embeds rather than
	///     written here: it has changed before, and the page is always the one that works.
	/// </summary>
	private async Task<string> ClientId(CancellationToken cancellationToken)
	{
		using var response = await Send(new HttpRequestMessage(HttpMethod.Get, new Uri(options.Value.BaseUrl)), cancellationToken);
		response.EnsureSuccessStatusCode();

		var page = await response.Content.ReadAsStringAsync(cancellationToken);

		return ClientIdPattern().Match(page) is { Success: true } match
			? match.Groups["id"].Value
			: throw new DubPlatformUnavailableException(DubPlatform.Crunchyroll, "The Crunchyroll home page no longer carries a client id.");
	}

	/// <summary>
	///     Sends one request, paced, and turns anything that is not an answer about the catalogue — a
	///     challenge, a rate limit, an outage, the proxy down — into <see cref="DubPlatformUnavailableException" />.
	/// </summary>
	private async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		using var owned = request;

		request.Version = HttpVersion.Version11;
		request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
		request.Headers.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);

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
			throw new DubPlatformUnavailableException(DubPlatform.Crunchyroll, $"Crunchyroll could not be reached: {exception.Message}", exception);
		}
		catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
		{
			throw new DubPlatformUnavailableException(DubPlatform.Crunchyroll, "Crunchyroll did not answer in time.", exception);
		}
		finally
		{
			_lastRequest = timeProvider.GetUtcNow();
			_pace.Release();
		}

		if (Refusal(response) is not { } refusal) return response;

		response.Dispose();
		throw new DubPlatformUnavailableException(DubPlatform.Crunchyroll, refusal);
	}

	private static string? Refusal(HttpResponseMessage response)
	{
		if (response.Headers.TryGetValues("cf-mitigated", out var mitigated) && mitigated.Contains("challenge"))
			return "Crunchyroll answered with an anti-bot challenge.";

		return response.StatusCode switch
		{
			// Not "no such series": the API says 404 for that. A 403 is Cloudflare or a region block.
			HttpStatusCode.Forbidden => "Crunchyroll refused the request (403).",
			HttpStatusCode.TooManyRequests => "Crunchyroll is rate limiting the requests (429).",
			>= HttpStatusCode.InternalServerError => $"Crunchyroll is failing ({(int)response.StatusCode}).",
			_ => null
		};
	}

	private Uri Url(string path, IReadOnlyDictionary<string, string> query)
	{
		var builder = new UriBuilder(new Uri(new Uri(options.Value.BaseUrl), path))
		{
			Query = string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))
		};

		return builder.Uri;
	}

	[GeneratedRegex("\"accountAuthClientId\"\\s*:\\s*\"(?<id>[^\"]+)\"")]
	private static partial Regex ClientIdPattern();
}
