using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnimeTracker.Adapters.Nautijon.Configs;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimeTracker.Adapters.Nautijon.FlareSolverr;

/// <summary>
///     Fetches a page through a FlareSolverr instance and hands back the HTML it rendered.
///     <para>
///         The obvious alternative — FlareSolverrSharp's ClearanceHandler, which solves once and
///         then replays requests itself using the returned cookies — does not work against the
///         Cloudflare configuration Nautiljon runs: the challenge is solved but no reusable
///         <c>cf_clearance</c> cookie comes back, and every request dies on "the cookies provided
///         by FlareSolverr are not valid". Reading the body the solver already rendered sidesteps
///         the whole question of whether clearance can be transplanted into another client.
///     </para>
/// </summary>
public class FlareSolverrClient(IHttpClientFactory httpClientFactory, IOptions<NautijonOptions> options, ILogger<FlareSolverrClient> logger)
	: TracingAdapter(logger)
{
	public const string ClientName = "FlareSolverr";

	/// <summary>How long to wait out a rate limit before asking again, and how many times to bother.</summary>
	private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromSeconds(30);

	private const int MaxRateLimitRetries = 3;

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	public async Task<string> GetHtml(string url, CancellationToken cancellationToken = default)
	{
		using var trace = LogAdapter($"{Log.F(url)}");

		for (var attempt = 1; ; attempt++)
		{
			var solution = await Solve(url, cancellationToken);

			if (solution.Status != (int)HttpStatusCode.TooManyRequests) return Ensure(url, solution);

			if (attempt > MaxRateLimitRetries)
				throw new HttpRequestException($"Nautiljon kept rate limiting {url} after {MaxRateLimitRetries} attempts.");

			// Backing off here rather than in a delegating handler: the solver always answers 200,
			// so the page's own status is the only place a rate limit is visible.
			_logger.LogWarning("Rate limited fetching {Url}, retrying in {Backoff}", url, RateLimitBackoff);
			await Task.Delay(RateLimitBackoff, cancellationToken);
		}
	}

	private async Task<Solution> Solve(string url, CancellationToken cancellationToken)
	{
		using var client = httpClientFactory.CreateClient(ClientName);

		var response = await client.PostAsJsonAsync("v1",
			new Command("request.get", url, options.Value.SolverTimeoutMs), Json, cancellationToken);

		response.EnsureSuccessStatusCode();

		var body = await response.Content.ReadFromJsonAsync<SolverResponse>(Json, cancellationToken);

		if (body is null || !string.Equals(body.Status, "ok", StringComparison.OrdinalIgnoreCase) || body.Solution is null)
			throw new HttpRequestException($"FlareSolverr could not fetch {url}: {body?.Message ?? "empty response"}");

		return body.Solution;
	}

	private static string Ensure(string url, Solution solution)
	{
		if (solution.Status is >= 200 and < 300) return solution.Response;

		throw new HttpRequestException($"Nautiljon answered {solution.Status} for {url}.", null, (HttpStatusCode)solution.Status);
	}

	private sealed record Command(
		[property: JsonPropertyName("cmd")] string Cmd,
		[property: JsonPropertyName("url")] string Url,
		[property: JsonPropertyName("maxTimeout")] int MaxTimeout);

	private sealed record SolverResponse(string? Status, string? Message, Solution? Solution);

	/// <summary>The rendered page. <see cref="Status" /> is the site's status code, not the solver's.</summary>
	private sealed record Solution(int Status, string Response);
}
