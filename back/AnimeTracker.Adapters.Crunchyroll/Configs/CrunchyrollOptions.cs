using System.ComponentModel.DataAnnotations;

namespace AnimeTracker.Adapters.Crunchyroll.Configs;

/// <summary>
///     Crunchyroll settings. The API is the one the website calls, read with the anonymous token any
///     visitor gets: there are no credentials to supply. Where requests leave from is not configured
///     here but under <c>Dub:Proxy</c>, shared by every platform.
/// </summary>
public sealed class CrunchyrollOptions
{
	public const string SectionName = "Crunchyroll";

	[Required]
	public string BaseUrl { get; set; } = "https://www.crunchyroll.com";

	/// <summary>
	///     The language of titles and descriptions. It changes nothing about audio: every episode lists
	///     all its dubs whatever the locale.
	/// </summary>
	[Required]
	public string Locale { get; set; } = "fr-FR";

	/// <summary>
	///     Pause between two requests. A season sync is a few hundred of them, and the point of the
	///     egress proxy is to never get an address banned; spacing them out is part of that.
	/// </summary>
	[Range(0, 10_000)]
	public int RequestDelayMs { get; set; } = 350;

	/// <summary>
	///     The browser the client says it is. Cloudflare answers the default .NET user agent with a
	///     challenge and lets this one through — measured, not assumed; see the spike report.
	/// </summary>
	[Required]
	public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
}
