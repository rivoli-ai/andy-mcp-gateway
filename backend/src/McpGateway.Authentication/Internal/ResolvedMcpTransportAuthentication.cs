using System.Text;
using McpGateway.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace McpGateway.Authentication.Internal;

/// <summary>Resolved configuration for MCP transport authentication (gateway JWT + optional Entra + MCP OAuth challenge).</summary>
internal sealed class ResolvedMcpTransportAuthentication
{
    public byte[] SymmetricJwtKey { get; private init; } = Array.Empty<byte>();
    public string GatewayJwtIssuer { get; private init; } = null!;
    public string GatewayJwtAudience { get; private init; } = null!;
    public McpGatewayOAuthOptions McpOAuth { get; private init; } = null!;
    public IConfigurationSection AzureAdSection { get; private init; } = null!;
    public string? AzureTenantId { get; private init; }
    public string? AzureClientId { get; private init; }
    public bool RegistersEntraJwtBearerForMcp { get; private init; }
    public List<string> AudiencesForAzure { get; private init; } = [];
    public string? AuthorizationServerIssuer { get; private init; }
    public bool UseMcpOAuthChallenge { get; private init; }
    public bool ProxyEntraAuthorizationServerMetadata { get; private init; }
    public string? McpPublicBase { get; private init; }
    public string? EntraIssuerForOidcProxy { get; private init; }
    public string EntraAuthority { get; private init; } = null!;
    public List<string> EntraValidIssuers { get; private init; } = [];

    public bool MapsEntraOAuthDiscoveryEndpoints =>
        !string.IsNullOrWhiteSpace(EntraIssuerForOidcProxy) && !string.IsNullOrWhiteSpace(McpPublicBase);

    internal static ResolvedMcpTransportAuthentication Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var secretKey = configuration["JWT:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("JWT:SecretKey must be configured.");
            secretKey = "dev-secret-key-min-32-characters-long-for-security";
        }

        var mcpOAuth = configuration.GetSection(McpGatewayOAuthOptions.SectionName).Get<McpGatewayOAuthOptions>()
            ?? new McpGatewayOAuthOptions();

        var azureSection = configuration.GetSection($"{AuthProvidersOptions.SectionName}:AzureAd");
        var azureTenantId = azureSection["TenantId"];
        var azureEnabled = azureSection.GetValue("Enabled", true);
        var azureClientId = azureSection["ClientId"];
        var azureAuthority = azureSection["Authority"]?.Trim();

        var registerEntraJwt = mcpOAuth.Enabled && azureEnabled && IsConfiguredAzureTenant(azureTenantId);

        var audiencesForAzure = new List<string>();
        if (mcpOAuth.ValidAudiences is { Length: > 0 } configuredAudiences)
            audiencesForAzure.AddRange(configuredAudiences.Where(a => !string.IsNullOrWhiteSpace(a)));
        if (registerEntraJwt && audiencesForAzure.Count == 0 && !string.IsNullOrWhiteSpace(azureClientId))
        {
            audiencesForAzure.Add(azureClientId!);
            audiencesForAzure.Add($"api://{azureClientId}");
        }

        if (registerEntraJwt && !string.IsNullOrWhiteSpace(mcpOAuth.ProtectedResourceIdentifier))
        {
            var pr = mcpOAuth.ProtectedResourceIdentifier.Trim();
            if (!audiencesForAzure.Contains(pr, StringComparer.OrdinalIgnoreCase))
                audiencesForAzure.Add(pr);
        }

        var effectiveRegisterEntra = registerEntraJwt;
        if (registerEntraJwt && audiencesForAzure.Count == 0)
        {
            Console.WriteLine(
                "[AUTH] Azure AD tenant is configured but no MCP token audiences (ClientId or Mcp:OAuth:ValidAudiences); Entra JWT bearer disabled for MCP transport.");
            effectiveRegisterEntra = false;
        }

        var authorizationServerIssuer = ResolveAuthorizationServerIssuer(
            mcpOAuth, effectiveRegisterEntra, azureTenantId, azureAuthority);

        var useMcpOAuthChallenge = mcpOAuth.Enabled && !string.IsNullOrWhiteSpace(authorizationServerIssuer);

        var proxyEntraAuthorizationServerMetadata = mcpOAuth.ProxyEntraAuthorizationServerMetadata
            && authorizationServerIssuer?.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) == true;

        string? mcpPublicBase = null;
        string? entraIssuerForOidcProxy = null;
        if (useMcpOAuthChallenge)
        {
            mcpPublicBase = (mcpOAuth.PublicBaseUrl ?? "").Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(mcpPublicBase))
            {
                mcpPublicBase = "http://localhost:5080";
                Console.WriteLine(
                    "[AUTH] Mcp:OAuth:PublicBaseUrl is empty; using http://localhost:5080 for protected-resource metadata (set Mcp:OAuth:PublicBaseUrl behind reverse proxies).");
            }

            if (proxyEntraAuthorizationServerMetadata)
                entraIssuerForOidcProxy = authorizationServerIssuer;
        }

        var entraAuthority = "";
        var entraValidIssuers = new List<string>();
        if (effectiveRegisterEntra && !string.IsNullOrWhiteSpace(azureTenantId))
        {
            var tenantId = azureTenantId.Trim();
            entraAuthority = string.IsNullOrWhiteSpace(azureAuthority)
                ? $"https://login.microsoftonline.com/{tenantId}/v2.0"
                : azureAuthority.Trim().TrimEnd('/');
            entraValidIssuers =
            [
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/",
                entraAuthority
            ];
            entraValidIssuers = entraValidIssuers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new ResolvedMcpTransportAuthentication
        {
            SymmetricJwtKey = Encoding.ASCII.GetBytes(secretKey),
            GatewayJwtIssuer = configuration["JWT:Issuer"] ?? "McpGateway",
            GatewayJwtAudience = configuration["JWT:Audience"] ?? "McpGateway",
            McpOAuth = mcpOAuth,
            AzureAdSection = azureSection,
            AzureTenantId = azureTenantId,
            AzureClientId = azureClientId,
            RegistersEntraJwtBearerForMcp = effectiveRegisterEntra,
            AudiencesForAzure = audiencesForAzure,
            AuthorizationServerIssuer = authorizationServerIssuer,
            UseMcpOAuthChallenge = useMcpOAuthChallenge,
            ProxyEntraAuthorizationServerMetadata = proxyEntraAuthorizationServerMetadata,
            McpPublicBase = mcpPublicBase,
            EntraIssuerForOidcProxy = entraIssuerForOidcProxy,
            EntraAuthority = entraAuthority,
            EntraValidIssuers = entraValidIssuers
        };
    }

    private static string? ResolveAuthorizationServerIssuer(
        McpGatewayOAuthOptions mcpOAuth,
        bool registersEntraJwt,
        string? azureTenantId,
        string? azureAuthority)
    {
        if (!string.IsNullOrWhiteSpace(mcpOAuth.AuthorizationServer)
            && Uri.TryCreate(mcpOAuth.AuthorizationServer, UriKind.Absolute, out var explicitIssuer))
            return explicitIssuer.ToString().TrimEnd('/');

        if (!registersEntraJwt || string.IsNullOrWhiteSpace(azureTenantId))
            return null;

        if (!string.IsNullOrWhiteSpace(azureAuthority) && Uri.TryCreate(azureAuthority, UriKind.Absolute, out var fromConfig))
            return fromConfig.ToString().TrimEnd('/');

        return $"https://login.microsoftonline.com/{azureTenantId}/v2.0";
    }

    private static bool IsConfiguredAzureTenant(string? tenantId) =>
        !string.IsNullOrWhiteSpace(tenantId)
        && !tenantId.Equals("YOUR_TENANT_ID", StringComparison.OrdinalIgnoreCase)
        && Guid.TryParse(tenantId, out _);
}
