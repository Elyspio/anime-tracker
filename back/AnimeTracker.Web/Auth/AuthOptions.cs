using System.ComponentModel.DataAnnotations;

namespace AnimeTracker.Web.Auth;

/// <summary>OIDC bearer validation settings. Validated at startup, never defaulted silently.</summary>
public sealed class AuthOptions
{
	public const string SectionName = "Auth";

	/// <summary>Realm issuer, e.g. https://auth.elyspio.fr/realms/anime-tracker.</summary>
	[Required]
	public string Authority { get; set; } = "";

	/// <summary>Expected audience — the Keycloak client id.</summary>
	[Required]
	public string Audience { get; set; } = "";

	/// <summary>
	///     Role required to mutate anything, granted either on the realm or on <see cref="Audience" />.
	///     Reading a season is anonymous: the data is a
	///     public broadcast schedule, and the only privileged action is spending someone else's
	///     scraping budget.
	/// </summary>
	[Required]
	public string AdminRole { get; set; } = "anime-tracker-admin";
}
