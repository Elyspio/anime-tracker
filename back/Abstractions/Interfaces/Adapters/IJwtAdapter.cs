using System.IdentityModel.Tokens.Jwt;

namespace AnimeTracker.Api.Abstractions.Interfaces.Adapters;

public interface IJwtAdapter
{
	bool ValidateJwt(string? token, out JwtSecurityToken? validatedToken);
}