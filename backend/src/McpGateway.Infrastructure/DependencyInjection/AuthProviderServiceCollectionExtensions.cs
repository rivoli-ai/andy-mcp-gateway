using McpGateway.Application.Auth;
using McpGateway.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpGateway.Infrastructure;

public static class AuthProviderServiceCollectionExtensions
{
    /// <summary>Binds <c>AuthProviders</c> and registers OIDC validation + gateway JWT minting for SPA login.</summary>
    public static IServiceCollection AddAuthProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthProvidersOptions>(options =>
        {
            var section = configuration.GetSection(AuthProvidersOptions.SectionName);
            var providers = section.Get<Dictionary<string, ProviderConfig>>() ?? new Dictionary<string, ProviderConfig>();
            options.Providers = new Dictionary<string, ProviderConfig>(providers, StringComparer.OrdinalIgnoreCase);
        });
        services.AddSingleton<AuthProviderRegistry>();
        services.AddSingleton<AuthenticationService>();
        services.AddHttpClient();
        return services;
    }
}
