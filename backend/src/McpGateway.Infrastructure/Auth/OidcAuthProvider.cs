using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using McpGateway.Application.Auth;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace McpGateway.Infrastructure.Auth;

/// <summary>
/// Generic OIDC provider for frontend-delegated flows (Azure AD, Duende, etc.).
/// The frontend obtains a token via OIDC; the backend validates the token using OIDC discovery.
/// </summary>
public sealed class OidcAuthProvider : IAuthProvider
{
    private readonly string _name;
    private readonly ProviderConfig _config;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configManager;
    private readonly string[] _validAudiences;

    public string Name => _name;
    public string Type => "FrontendOidc";

    public OidcAuthProvider(string name, ProviderConfig config)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        var authority = config.Authority ?? throw new InvalidOperationException($"Authority is required for OIDC provider '{name}'.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
            throw new InvalidOperationException($"Authority must be an absolute URI for OIDC provider '{name}'.");

        var metadataAddress = new Uri(authorityUri, ".well-known/openid-configuration").ToString();
        var authorityIsHttps = string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

        if (!authorityIsHttps)
        {
            using var httpForDiscovery = new HttpClient();
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpForDiscovery) { RequireHttps = false });
        }
        else
        {
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever());
        }

        var audiences = new List<string>();
        if (!string.IsNullOrEmpty(config.ClientId)) audiences.Add(config.ClientId);
        if (!string.IsNullOrEmpty(config.SpaClientId)) audiences.Add(config.SpaClientId);
        _validAudiences = audiences.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<ClaimsPrincipal> ValidateTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));

        var oidcConfig = await _configManager.GetConfigurationAsync(ct).ConfigureAwait(false);
        var handler = new JwtSecurityTokenHandler();

        var validIssuers = BuildValidIssuers(oidcConfig, _config);
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = validIssuers.Count > 0,
            ValidIssuers = validIssuers,
            ValidateAudience = _validAudiences.Length > 0,
            ValidAudiences = _validAudiences,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            IssuerSigningKeys = oidcConfig.SigningKeys,
            ValidateIssuerSigningKey = true,
        };

        return handler.ValidateToken(token, parameters, out _);
    }

    private static List<string> BuildValidIssuers(OpenIdConnectConfiguration oidcConfig, ProviderConfig config)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(oidcConfig.Issuer))
            set.Add(oidcConfig.Issuer);

        var tenantId = !string.IsNullOrWhiteSpace(config.TenantId)
            ? config.TenantId.Trim()
            : TryGetAzureAdTenantGuidFromAuthority(config.Authority);

        if (!string.IsNullOrEmpty(tenantId)
            && !tenantId.Equals("common", StringComparison.OrdinalIgnoreCase)
            && !tenantId.Equals("organizations", StringComparison.OrdinalIgnoreCase)
            && !tenantId.Equals("consumers", StringComparison.OrdinalIgnoreCase))
        {
            set.Add($"https://login.microsoftonline.com/{tenantId}/v2.0");
            set.Add($"https://login.microsoftonline.com/{tenantId}/");
            set.Add($"https://sts.windows.net/{tenantId}/");
        }

        return set.ToList();
    }

    private static string? TryGetAzureAdTenantGuidFromAuthority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority) || !Uri.TryCreate(authority, UriKind.Absolute, out var uri))
            return null;
        if (!uri.Host.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        return Guid.TryParse(parts[0], out _) ? parts[0] : null;
    }
}
