namespace McpGateway.Application.Auth;

/// <summary>
/// Configuration root for the SPA-login auth providers exposed at <c>/api/auth/{provider}/*</c>.
/// Bound from the <c>AuthProviders</c> configuration section.
/// </summary>
public class AuthProvidersOptions
{
    public const string SectionName = "AuthProviders";

    public Dictionary<string, ProviderConfig> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<KeyValuePair<string, ProviderConfig>> GetEnabledProviders() =>
        Providers.Where(kv => kv.Value.Enabled);
}

/// <summary>
/// One named auth provider — typically Microsoft Entra ID. Only OIDC frontend-delegated flows
/// are supported today (<c>Type = "FrontendOidc"</c>).
/// </summary>
public class ProviderConfig
{
    public bool Enabled { get; set; }
    public string? Type { get; set; } = "FrontendOidc";

    public string? Authority { get; set; }
    public string? ClientId { get; set; }
    public string? SpaClientId { get; set; }
    public string? Scopes { get; set; }
    public string? TenantId { get; set; }
}
