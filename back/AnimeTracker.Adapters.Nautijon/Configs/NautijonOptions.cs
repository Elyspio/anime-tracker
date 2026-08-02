using System.ComponentModel.DataAnnotations;

namespace AnimeTracker.Adapters.Nautijon.Configs;

/// <summary>Scraping settings. Validated at startup — a missing solver means every scrape 403s.</summary>
public sealed class NautijonOptions
{
	public const string SectionName = "Nautijon";

	/// <summary>
	///     FlareSolverr endpoint. Nautiljon sits behind Cloudflare, so every request is proxied
	///     through a solver that holds the clearance cookie. Aspire runs one locally; deployments
	///     point at a shared instance.
	/// </summary>
	[Required]
	public string FlareSolverrUrl { get; set; } = "";

	/// <summary>Solver round-trip budget in milliseconds — a challenge takes tens of seconds.</summary>
	public int SolverTimeoutMs { get; set; } = 60_000;
}
