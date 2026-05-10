using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace McpGateway.Application.Auth;

/// <summary>Issues short-lived gateway JWTs after external OIDC validation (SPA login flow).</summary>
public sealed class AuthenticationService
{
    /// <summary>Role granted to operators allowed to manage API keys / privileged settings.</summary>
    public const string AdminRole = "admin";

    private readonly IConfiguration _configuration;

    public AuthenticationService(IConfiguration configuration) =>
        _configuration = configuration;

    /// <summary>
    /// Issues a gateway JWT. <paramref name="roles"/> is forwarded verbatim from the
    /// upstream IdP (Azure App Roles in the <c>roles</c> claim) so authorization policies
    /// like <c>[Authorize(Roles = "admin")]</c> can read them off the principal directly.
    /// </summary>
    public string GenerateToken(string subjectId, string email, string? name = null, IEnumerable<string>? roles = null)
    {
        var secretKey = _configuration["JWT:SecretKey"] ?? "dev-secret-key-min-32-characters-long-for-security";
        var issuer = _configuration["JWT:Issuer"] ?? "McpGateway";
        var audience = _configuration["JWT:Audience"] ?? "McpGateway";

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId),
            new(JwtRegisteredClaimNames.Email, email),
        };
        if (!string.IsNullOrWhiteSpace(name))
            claims.Add(new Claim("name", name));

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role))
                    continue;
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
