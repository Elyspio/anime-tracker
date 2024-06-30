using System.IdentityModel.Tokens.Jwt;
using Example.Api.Abstractions.Common.Technical.Tracing;
using Example.Api.Abstractions.Interfaces.Adapters;
using Example.Api.Abstractions.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Example.Api.Core.Services;

internal class AuthenticationService : TracingService, IAuthenticationService
{
	private readonly IJwtAdapter _jwtAdapter;

	public AuthenticationService(IJwtAdapter jwtAdapter, ILogger<AuthenticationService> logger) : base(logger)
	{
		_jwtAdapter = jwtAdapter;
	}


	public bool ValidateJwt(string? token, out JwtSecurityToken? validatedToken)
	{
		using var _ = LogService();

		return _jwtAdapter.ValidateJwt(token, out validatedToken);
	}
}