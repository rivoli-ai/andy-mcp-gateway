using McpGateway.Auth.Options;
using Microsoft.Extensions.Options;

namespace McpGateway.Auth;

public sealed class AuthProviderRegistry
{
    public AuthProvidersOptions Options { get; }
    private readonly Dictionary<string, IAuthProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public AuthProviderRegistry(IOptions<AuthProvidersOptions> options)
    {
        Options = options.Value ?? new AuthProvidersOptions();

        foreach (var (name, cfg) in Options.GetEnabledProviders())
        {
            var type = (cfg.Type ?? "FrontendOidc").Trim();
            if (type.Equals("FrontendOidc", StringComparison.OrdinalIgnoreCase))
            {
                _providers[name] = new OidcAuthProvider(name, cfg);
            }
        }
    }

    public bool TryGetProvider(string name, out IAuthProvider? provider)
        => _providers.TryGetValue(name, out provider);
}

