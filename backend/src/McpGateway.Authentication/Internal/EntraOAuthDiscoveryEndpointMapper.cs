using System.Text.Json;
using System.Text.Json.Nodes;
using McpGateway.Application.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;

namespace McpGateway.Authentication.Internal;

internal static class EntraOAuthDiscoveryEndpointMapper
{
    /// <summary>
    /// Single shared store of pending authorization requests. Static so the same
    /// instance is observed by the <c>/oauth2/authorize</c> and <c>/oauth2/callback</c>
    /// minimal-API handlers, which run as independent delegates and have no other
    /// shared state.
    /// </summary>
    private static readonly PendingMcpAuthorizationStore PendingAuthorizations = new();

    public static void Map(WebApplication app, ResolvedMcpTransportAuthentication state)
    {
        if (!state.MapsEntraOAuthDiscoveryEndpoints)
            return;

        var entraIssuer = state.EntraIssuerForOidcProxy!.TrimEnd('/');
        var registrationEndpoint = $"{state.McpPublicBase}/oauth/register";
        var gatewayAuthorizeUrl = $"{state.McpPublicBase}/oauth2/authorize";
        var gatewayTokenUrl = $"{state.McpPublicBase}/oauth2/token";
        var gatewayCallbackUrl = $"{state.McpPublicBase}/oauth2/callback";

        var (entraAuthorizationEndpoint, entraTokenEndpoint) = ResolveEntraEndpoints(app, entraIssuer);

        var mcpOAuth = state.McpOAuth;
        var azureSection = state.AzureAdSection;

        MapAuthorizationServerMetadata(app, entraIssuer, registrationEndpoint, gatewayAuthorizeUrl, gatewayTokenUrl,
            entraAuthorizationEndpoint, entraTokenEndpoint, mcpOAuth);

        if (mcpOAuth.StripResourceOnAuthorizeRedirect && !string.IsNullOrWhiteSpace(entraAuthorizationEndpoint))
        {
            MapAuthorizeProxy(app, entraAuthorizationEndpoint!, gatewayCallbackUrl);
            MapCallbackProxy(app);
            Console.WriteLine(
                $"[AUTH] OAuth proxy: clients are bridged through {gatewayCallbackUrl}. " +
                $"Register this single redirect URI in Entra (and only this one).");
        }

        if (mcpOAuth.StripResourceOnAuthorizeRedirect && !string.IsNullOrWhiteSpace(entraTokenEndpoint))
        {
            MapTokenProxy(app, entraTokenEndpoint!, gatewayCallbackUrl);
            Console.WriteLine($"[AUTH] Entra token proxy: token_endpoint → {gatewayTokenUrl} (strips resource= on POST to Microsoft).");
        }

        MapDynamicClientRegistrationShim(app, mcpOAuth, azureSection, state.AzureClientId);
        Console.WriteLine($"[AUTH] OAuth DCR shim: registration_endpoint={registrationEndpoint} (RFC 7591 POST returns pre-registered Entra client_id).");
    }

    private static (string? AuthorizationEndpoint, string? TokenEndpoint) ResolveEntraEndpoints(
        WebApplication app, string entraIssuer)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("McpGateway/1.0");
            var oidcJson = http.GetStringAsync($"{entraIssuer}/.well-known/openid-configuration").GetAwaiter().GetResult();
            using var oidcDoc = JsonDocument.Parse(oidcJson);

            string? authorizationEndpoint = null;
            string? tokenEndpoint = null;
            if (oidcDoc.RootElement.TryGetProperty("authorization_endpoint", out var ae))
                authorizationEndpoint = ae.GetString();
            if (oidcDoc.RootElement.TryGetProperty("token_endpoint", out var te))
                tokenEndpoint = te.GetString();
            return (authorizationEndpoint, tokenEndpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] Could not resolve Entra OIDC endpoints (authorize/token passthrough disabled): {ex.Message}");
            return (null, null);
        }
    }

    private static void MapAuthorizationServerMetadata(
        WebApplication app,
        string entraIssuer,
        string registrationEndpoint,
        string gatewayAuthorizeUrl,
        string gatewayTokenUrl,
        string? entraAuthorizationEndpoint,
        string? entraTokenEndpoint,
        McpGatewayOAuthOptions mcpOAuth)
    {
        app.MapGet("/.well-known/oauth-authorization-server", async (HttpContext context, IHttpClientFactory httpFactory) =>
        {
            var oidcUrl = $"{entraIssuer}/.well-known/openid-configuration";
            try
            {
                using var client = httpFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("McpGateway/1.0");
                using var resp = await client.GetAsync(oidcUrl, context.RequestAborted);
                if (!resp.IsSuccessStatusCode)
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    context.Response.StatusCode = (int)resp.StatusCode;
                    await resp.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
                    return;
                }

                var json = await resp.Content.ReadAsStringAsync(context.RequestAborted);
                var node = JsonNode.Parse(json) as JsonObject
                    ?? throw new InvalidOperationException("Entra OIDC metadata is not a JSON object.");

                node["registration_endpoint"] = registrationEndpoint;

                if (mcpOAuth.StripResourceOnAuthorizeRedirect)
                {
                    if (!string.IsNullOrWhiteSpace(entraAuthorizationEndpoint))
                        node["authorization_endpoint"] = gatewayAuthorizeUrl;
                    if (!string.IsNullOrWhiteSpace(entraTokenEndpoint))
                        node["token_endpoint"] = gatewayTokenUrl;
                }

                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync(node.ToJsonString(), context.RequestAborted);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(
                    new { error = "server_error", error_description = $"Failed to load Entra OIDC metadata from {oidcUrl}: {ex.Message}" },
                    context.RequestAborted);
            }
        }).AllowAnonymous();
    }

    /// <summary>
    /// Bridges the MCP client's authorization request to Entra. Entra only honours the
    /// redirect URI registered on the app registration, so the gateway substitutes its
    /// own callback URL and remembers the client's original <c>redirect_uri</c> /
    /// <c>state</c> behind an opaque gateway-issued state token.
    /// </summary>
    private static void MapAuthorizeProxy(WebApplication app, string entraAuthorizeTarget, string gatewayCallbackUrl)
    {
        app.MapGet("/oauth2/authorize", (HttpRequest request) =>
        {
            var clientRedirectUri = request.Query["redirect_uri"].ToString();
            if (string.IsNullOrWhiteSpace(clientRedirectUri))
            {
                return Results.BadRequest(new
                {
                    error = "invalid_request",
                    error_description = "redirect_uri is required."
                });
            }

            var clientState = request.Query["state"].ToString();
            var gatewayState = PendingAuthorizations.Issue(clientRedirectUri, clientState);

            var qb = new QueryBuilder();
            foreach (var kv in request.Query)
            {
                if (IsParameterReplacedByGateway(kv.Key))
                    continue;
                foreach (var value in kv.Value)
                {
                    if (value is null)
                        continue;
                    qb.Add(kv.Key, value);
                }
            }

            qb.Add("redirect_uri", gatewayCallbackUrl);
            qb.Add("state", gatewayState);

            return Results.Redirect(entraAuthorizeTarget + qb.ToString());
        }).AllowAnonymous();
    }

    /// <summary>
    /// Receives the authorization response from Entra and forwards it to the MCP
    /// client's original <c>redirect_uri</c>, restoring the client's original
    /// <c>state</c>. The authorization <c>code</c> from Entra is passed through
    /// unchanged; it will be exchanged at the proxied <c>/oauth2/token</c> endpoint.
    /// </summary>
    private static void MapCallbackProxy(WebApplication app)
    {
        app.MapGet("/oauth2/callback", (HttpRequest request) =>
        {
            var pending = PendingAuthorizations.Redeem(request.Query["state"].ToString());
            if (pending is null)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_state",
                    error_description = "Unknown or expired authorization request."
                });
            }

            var qb = new QueryBuilder();
            foreach (var kv in request.Query)
            {
                if (string.Equals(kv.Key, "state", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var value in kv.Value)
                {
                    if (value is null)
                        continue;
                    qb.Add(kv.Key, value);
                }
            }

            if (!string.IsNullOrEmpty(pending.ClientState))
                qb.Add("state", pending.ClientState);

            return Results.Redirect(pending.ClientRedirectUri + qb.ToString());
        }).AllowAnonymous();
    }

    /// <summary>
    /// Forwards the token exchange to Entra. Entra requires the <c>redirect_uri</c>
    /// presented at the token endpoint to match the one that was sent at the
    /// authorize endpoint, so the gateway always overrides it with its own callback
    /// URL (the same value that <see cref="MapAuthorizeProxy"/> sent to Entra).
    /// </summary>
    private static void MapTokenProxy(WebApplication app, string entraTokenEndpoint, string gatewayCallbackUrl)
    {
        app.MapPost("/oauth2/token", async (HttpContext http, IHttpClientFactory httpFactory) =>
        {
            if (!http.Request.HasFormContentType)
            {
                http.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var key in form.Keys)
            {
                if (IsParameterReplacedByGateway(key))
                    continue;
                var value = form[key].ToString();
                if (value.Length > 0)
                    pairs.Add(new KeyValuePair<string, string>(key, value));
            }

            pairs.Add(new KeyValuePair<string, string>("redirect_uri", gatewayCallbackUrl));

            using var client = httpFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("McpGateway/1.0");
            using var upstream = new FormUrlEncodedContent(pairs);
            using var resp = await client.PostAsync(entraTokenEndpoint, upstream, http.RequestAborted);

            http.Response.StatusCode = (int)resp.StatusCode;
            if (resp.Content.Headers.ContentType is { } ct)
                http.Response.ContentType = ct.ToString();
            await resp.Content.CopyToAsync(http.Response.Body, http.RequestAborted);
        }).AllowAnonymous();
    }

    /// <summary>
    /// RFC 7591 Dynamic Client Registration shim. Entra does not support DCR for
    /// public clients, so the gateway returns a pre-registered Entra <c>client_id</c>
    /// and echoes the redirect URIs the MCP client supplied. The redirect URIs are
    /// not actually registered with Entra — the authorize/callback proxy ensures they
    /// never leave the gateway.
    /// </summary>
    private static void MapDynamicClientRegistrationShim(
        WebApplication app,
        McpGatewayOAuthOptions mcpOAuth,
        IConfigurationSection azureSection,
        string? azureClientId)
    {
        app.MapPost("/oauth/register", async (HttpContext context) =>
        {
            var clientId = ResolveDcrClientId(mcpOAuth, azureSection, azureClientId);
            if (string.IsNullOrWhiteSpace(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "invalid_client_metadata",
                    error_description = "Set AuthProviders:AzureAd:SpaClientId (native/public MCP app) or Mcp:OAuth:DynamicClientRegistrationClientId so the gateway can advertise dynamic client registration."
                }, context.RequestAborted);
                return;
            }

            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var bodyText = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;

            var redirectUris = new List<string>();
            if (!string.IsNullOrWhiteSpace(bodyText)
                && JsonNode.Parse(bodyText) is JsonObject bodyObj
                && bodyObj["redirect_uris"] is JsonArray uris)
            {
                foreach (var el in uris)
                {
                    if (el is JsonValue jv && jv.TryGetValue<string>(out var u) && !string.IsNullOrWhiteSpace(u))
                        redirectUris.Add(u);
                }
            }

            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                client_id = clientId,
                client_id_issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                client_secret_expires_at = 0,
                redirect_uris = redirectUris.ToArray()
            }, context.RequestAborted);
        }).AllowAnonymous();
    }

    /// <summary>
    /// Parameters the gateway owns and must not pass through verbatim: <c>resource</c>
    /// trips AADSTS9010010, and <c>redirect_uri</c> / <c>state</c> are substituted with
    /// gateway-controlled values so Entra only ever sees its registered callback URL.
    /// </summary>
    private static bool IsParameterReplacedByGateway(string name) =>
        string.Equals(name, "resource", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "redirect_uri", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "state", StringComparison.OrdinalIgnoreCase);

    private static string? ResolveDcrClientId(
        McpGatewayOAuthOptions mcpOAuth,
        IConfigurationSection azureSection,
        string? azureClientId) =>
        string.IsNullOrWhiteSpace(mcpOAuth.DynamicClientRegistrationClientId)
            ? (string.IsNullOrWhiteSpace(azureSection["SpaClientId"]) ? azureClientId : azureSection["SpaClientId"])
            : mcpOAuth.DynamicClientRegistrationClientId;
}
