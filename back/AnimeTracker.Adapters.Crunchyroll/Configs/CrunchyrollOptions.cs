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

	/// <summary>The public site: where series pages are, and where requests go when there is no gateway.</summary>
	[Required]
	public string BaseUrl { get; set; } = "https://www.crunchyroll.com";

	/// <summary>
	///     The gateway in the qBittorrent pod, spoken to in plain HTTP; nginx there opens the TLS connection
	///     to Crunchyroll from the VPN exit. Preferred to <c>Dub:Proxy</c> when set: Cloudflare judges the
	///     client by its TLS handshake, and .NET's is always challenged on Windows and challenged on Linux
	///     as soon as a session is resumed — nginx's passes. Blank: straight to <see cref="BaseUrl" />
	///     through the proxy.
	/// </summary>
	public string Gateway { get; set; } = "";

	/// <summary>Where requests actually go.</summary>
	public string ApiRoot => string.IsNullOrWhiteSpace(Gateway) ? BaseUrl : Gateway;

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
