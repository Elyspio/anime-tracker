using System.ComponentModel.DataAnnotations;

namespace AnimeTracker.Adapters.Adn.Configs;

/// <summary>
///     ADN (Animation Digital Network) settings. The gateway is the one the website calls, and needs
///     no account for the catalogue. Where requests leave from is <c>Dub:Proxy</c>, shared by every
///     platform.
/// </summary>
public sealed class AdnOptions
{
	public const string SectionName = "Adn";

	[Required]
	public string BaseUrl { get; set; } = "https://gw.api.animationdigitalnetwork.com";

	/// <summary>
	///     The catalogue asked for. ADN runs one per territory (<c>fr</c>, <c>de</c>…) and says so in a
	///     header rather than by the caller's address.
	/// </summary>
	[Required]
	public string Distribution { get; set; } = "fr";

	/// <summary>Pause between two requests, for the same reason as Crunchyroll's: never get the egress banned.</summary>
	[Range(0, 10_000)]
	public int RequestDelayMs { get; set; } = 350;

	[Required]
	public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
}
