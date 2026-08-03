using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace AnimeTracker.Web.Auth;

public static class AuthModule
{
	public const string AdminPolicy = "Admin";

	/// <summary>Keycloak nests realm roles under this claim rather than emitting plain role claims.</summary>
	private const string RealmAccessClaim = "realm_access";

	/// <summary>Client roles live here, keyed by client id, in the same nested shape.</summary>
	private const string ResourceAccessClaim = "resource_access";

	public static IServiceCollection AddAppAuth(this IServiceCollection services, IConfiguration config)
	{
		var auth = new AuthOptions();
		config.GetSection(AuthOptions.SectionName).Bind(auth);

		services.AddOptions<AuthOptions>()
			.Bind(config.GetSection(AuthOptions.SectionName))
			.ValidateDataAnnotations()
			.Validate(options => IsHttpAuthority(options.Authority), "Auth:Authority must be an absolute HTTP(S) URL.")
			.ValidateOnStart();

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.Authority = auth.Authority;
				options.Audience = auth.Audience;
				// Local development runs Keycloak over http; deployed authorities are https.
				options.RequireHttpsMetadata = auth.Authority.StartsWith("https", StringComparison.OrdinalIgnoreCase);
				options.TokenValidationParameters.NameClaimType = "name";
				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = context =>
					{
						FlattenKeycloakRoles(context.Principal, auth.Audience);
						return Task.CompletedTask;
					}
				};
			});

		// Unlike the reference application there is no default policy: browsing a season is
		// anonymous, and every mutating endpoint opts in with [Authorize(AuthModule.AdminPolicy)].
		services.AddAuthorizationBuilder()
			.AddPolicy(AdminPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(auth.AdminRole));

		return services;
	}

	/// <summary>
	///     Lifts Keycloak's realm_access.roles and resource_access.[client].roles arrays into standard
	///     role claims, so RequireRole and User.IsInRole work without every call site parsing JSON.
	///     Both shapes are read because the same role can be granted at either level depending on how
	///     the realm was set up, and a token only ever carries it in one of them.
	/// </summary>
	private static void FlattenKeycloakRoles(ClaimsPrincipal? principal, string clientId)
	{
		if (principal?.Identity is not ClaimsIdentity identity) return;

		AddRoles(identity, principal.FindFirst(RealmAccessClaim)?.Value, null);
		AddRoles(identity, principal.FindFirst(ResourceAccessClaim)?.Value, clientId);
	}

	/// <summary>
	///     Reads a roles array out of a nested Keycloak claim. When <paramref name="clientId" /> is set
	///     the array is looked up one level deeper, under that client — roles granted to other clients
	///     in the same token are none of this API's business.
	/// </summary>
	private static void AddRoles(ClaimsIdentity identity, string? claim, string? clientId)
	{
		if (string.IsNullOrWhiteSpace(claim)) return;

		try
		{
			using var document = JsonDocument.Parse(claim);
			var container = document.RootElement;

			if (clientId is not null)
			{
				if (!container.TryGetProperty(clientId, out var client) || client.ValueKind != JsonValueKind.Object) return;
				container = client;
			}

			if (!container.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array) return;

			foreach (var role in roles.EnumerateArray())
			{
				var value = role.GetString();
				if (!string.IsNullOrWhiteSpace(value)) identity.AddClaim(new Claim(identity.RoleClaimType, value));
			}
		}
		catch (JsonException)
		{
			// A malformed claim means no roles, which the policy then rejects. Nothing to log:
			// the token came from outside and its shape is not ours to trust.
		}
	}

	private static bool IsHttpAuthority(string authority)
	{
		return Uri.TryCreate(authority, UriKind.Absolute, out var uri)
		       && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}
}
