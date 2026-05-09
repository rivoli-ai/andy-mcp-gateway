namespace McpGateway.Auth.Options;

public class AuthProvidersOptions
{
    public const string SectionName = "AuthProviders";

    public Dictionary<string, ProviderConfig> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<KeyValuePair<string, ProviderConfig>> GetEnabledProviders()
        => Providers.Where(kv => kv.Value.Enabled);
}

public class ProviderConfig
{
    public bool Enabled { get; set; }
    public string? Type { get; set; } = "FrontendOidc";

    // OIDC
    public string? Authority { get; set; }
    public string? ClientId { get; set; }
    public string? SpaClientId { get; set; }
    public string? Scopes { get; set; }
    public string? TenantId { get; set; }
    public string? ProfileEndpoint { get; set; }
}

