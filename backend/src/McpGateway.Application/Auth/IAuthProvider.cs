using System.Security.Claims;

namespace McpGateway.Application.Auth;

public interface IAuthProvider
{
    string Name { get; }
    string Type { get; }

    Task<ClaimsPrincipal> ValidateTokenAsync(string token, CancellationToken ct);
}
