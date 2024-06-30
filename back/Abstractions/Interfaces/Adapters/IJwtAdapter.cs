using System.IdentityModel.Tokens.Jwt;

namespace Example.Api.Abstractions.Interfaces.Adapters;

public interface IJwtAdapter
{
	bool ValidateJwt(string? token, out JwtSecurityToken? validatedToken);
}