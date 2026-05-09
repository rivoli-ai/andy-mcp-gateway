using McpGateway.Application.Auth;
using Microsoft.Extensions.Options;

namespace McpGateway.Infrastructure.Auth;

/// <summary>
/// Resolves named auth providers from <see cref="AuthProvidersOptions"/> and instantiates
/// the matching <see cref="IAuthProvider"/> for each enabled entry.
/// </summary>
public sealed class AuthProviderRegistry
{
    private readonly Dictionary<string, IAuthProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public AuthProviderRegistry(IOptions<AuthProvidersOptions> options)
    {
        Options = options.Value ?? new AuthProvidersOptions();

        foreach (var (name, config) in Options.GetEnabledProviders())
        {
            var type = (config.Type ?? "FrontendOidc").Trim();
            if (type.Equals("FrontendOidc", StringComparison.OrdinalIgnoreCase))
                _providers[name] = new OidcAuthProvider(name, config);
        }
    }

    public AuthProvidersOptions Options { get; }

    public bool TryGetProvider(string name, out IAuthProvider? provider) =>
        _providers.TryGetValue(name, out provider);
}
