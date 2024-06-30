using System.IdentityModel.Tokens.Jwt;

namespace Example.Api.Abstractions.Interfaces.Services;

public interface IAuthenticationService
{
	bool ValidateJwt(string? token, out JwtSecurityToken? validatedToken);
}