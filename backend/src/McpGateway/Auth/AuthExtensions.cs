using McpGateway.Auth.Options;

namespace McpGateway.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthProviders(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind dictionary-at-root (DevPilot style):
        // "AuthProviders": { "AzureAd": { ... }, "Duende": { ... } }
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

