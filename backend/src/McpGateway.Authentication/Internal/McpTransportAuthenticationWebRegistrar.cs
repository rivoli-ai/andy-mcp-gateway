using McpGateway.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace McpGateway.Authentication.Internal;

internal static class McpTransportAuthenticationWebRegistrar
{
    public static void AddAuthenticationAndAuthorization(
        IServiceCollection services,
        ResolvedMcpTransportAuthentication state)
    {
        AddJwtAndMcpAuthentication(services, state);
        AddAuthorizationPolicies(services, state);
    }

    /// <summary>Registers the personal API key scheme so <c>X-MCP-Key</c> is honoured on MCP routes.</summary>
    private static void AddApiKeyScheme(AuthenticationBuilder authBuilder)
    {
        authBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            McpTransportAuthenticationSchemes.ApiKey,
            displayName: "MCP Gateway API Key",
            configureOptions: _ => { });
    }

    private static void AddJwtAndMcpAuthentication(IServiceCollection services, ResolvedMcpTransportAuthentication state)
    {
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = state.UseMcpOAuthChallenge
                ? McpAuthenticationDefaults.AuthenticationScheme
                : JwtBearerDefaults.AuthenticationScheme;
        });

        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(state.SymmetricJwtKey),
                ValidateIssuer = true,
                ValidIssuer = state.GatewayJwtIssuer,
                ValidateAudience = true,
                ValidAudience = state.GatewayJwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
            JwtBearerEntraMcpChallengeExtensions.AttachMcpResourceMetadataChallenge(options);
        });

        if (state.RegistersEntraJwtBearerForMcp)
        {
            authBuilder.AddJwtBearer(McpTransportAuthenticationSchemes.EntraAccessToken, options =>
            {
                options.Authority = state.EntraAuthority;
                options.MetadataAddress = $"{state.EntraAuthority}/.well-known/openid-configuration";
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = state.EntraValidIssuers,
                    ValidateAudience = state.AudiencesForAzure.Count > 0,
                    ValidAudiences = state.AudiencesForAzure,
                    ValidateLifetime = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetService<ILoggerFactory>()
                            ?.CreateLogger("McpGateway.Authentication.EntraJwtBearer");
                        logger?.LogWarning(ctx.Exception,
                            "Entra JWT rejected on MCP transport (check aud/issuer and Mcp:OAuth:ValidAudiences).");
                        return Task.CompletedTask;
                    }
                };
                JwtBearerEntraMcpChallengeExtensions.AttachMcpResourceMetadataChallenge(options);
            });
        }

        AddApiKeyScheme(authBuilder);

        if (state.UseMcpOAuthChallenge)
        {
            var authorizationServersInMetadata = state.ProxyEntraAuthorizationServerMetadata
                ? new List<string> { state.McpPublicBase! }
                : new List<string> { state.AuthorizationServerIssuer! };

            var scopesForProtectedResourceMetadata = state.ProxyEntraAuthorizationServerMetadata
                && !string.IsNullOrWhiteSpace(state.AzureClientId)
                ? McpOAuthScopeHelper.ForEntraProtectedResourceMetadata(state.McpOAuth.ScopesSupported, state.AzureClientId)
                : state.McpOAuth.ScopesSupported;

            // Force the `resource` field to come from Mcp:OAuth:PublicBaseUrl (https)
            // instead of being inferred from HttpContext.Request — that inference is
            // unreliable behind a TLS-terminating reverse proxy (nginx → http upstream).
            // Cline validates that the metadata `resource` exactly matches the URL it
            // dialed; without this it sees `http://…` and rejects the connection.
            var explicitResource = !string.IsNullOrWhiteSpace(state.McpPublicBase)
                ? state.McpPublicBase
                : null;

            authBuilder.AddMcp(options =>
            {
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = explicitResource,
                    AuthorizationServers = authorizationServersInMetadata,
                    ScopesSupported = scopesForProtectedResourceMetadata
                };
            });

            if (state.ProxyEntraAuthorizationServerMetadata)
            {
                Console.WriteLine(
                    $"[AUTH] Entra OAuth discovery workaround: authorization_servers → {state.McpPublicBase}; GET /.well-known/oauth-authorization-server proxies OpenID configuration from Entra.");
            }
        }
    }

    private static void AddAuthorizationPolicies(IServiceCollection services, ResolvedMcpTransportAuthentication state)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(McpTransportAuthorizationPolicy.Name, policy =>
            {
                // X-MCP-Key is always accepted on MCP transport routes — it's the OAuth2-bypass path
                // for clients that don't speak OIDC. The handler returns NoResult when the header is
                // absent, so the JWT/Entra schemes still get a chance to evaluate the request normally.
                var schemes = new List<string> { McpTransportAuthenticationSchemes.ApiKey };
                if (state.RegistersEntraJwtBearerForMcp)
                    schemes.Add(McpTransportAuthenticationSchemes.EntraAccessToken);
                schemes.Add(JwtBearerDefaults.AuthenticationScheme);

                policy.AddAuthenticationSchemes(schemes.ToArray());
                policy.RequireAuthenticatedUser();
            });
        });
    }
}
