using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace McpGateway.Application.Auth;

/// <summary>Issues short-lived gateway JWTs after external OIDC validation (SPA login flow).</summary>
public sealed class AuthenticationService
{
    private readonly IConfiguration _configuration;

    public AuthenticationService(IConfiguration configuration) =>
        _configuration = configuration;

    public string GenerateToken(string subjectId, string email, string? name = null)
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

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
