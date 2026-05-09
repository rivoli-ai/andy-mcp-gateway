using System.Text.Json;
using System.Text.Json.Nodes;
using McpGateway.Application.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;

namespace McpGateway.Authentication.Internal;

internal static class EntraOAuthDiscoveryEndpointMapper
{
    public static void Map(WebApplication app, ResolvedMcpTransportAuthentication state)
    {
        if (!state.MapsEntraOAuthDiscoveryEndpoints)
            return;

        var entraIssuer = state.EntraIssuerForOidcProxy!.TrimEnd('/');
        var registrationEndpoint = $"{state.McpPublicBase}/oauth/register";
        var gatewayAuthorizeUrl = $"{state.McpPublicBase}/oauth2/authorize";
        var gatewayTokenUrl = $"{state.McpPublicBase}/oauth2/token";

        string? entraAuthorizationEndpoint = null;
        string? entraTokenEndpoint = null;
        try
        {
            using var scope = app.Services.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("McpGateway/1.0");
            var oidcJson = http.GetStringAsync($"{entraIssuer}/.well-known/openid-configuration").GetAwaiter().GetResult();
            using var oidcDoc = JsonDocument.Parse(oidcJson);
            if (oidcDoc.RootElement.TryGetProperty("authorization_endpoint", out var ae))
                entraAuthorizationEndpoint = ae.GetString();
            if (oidcDoc.RootElement.TryGetProperty("token_endpoint", out var te))
                entraTokenEndpoint = te.GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] Could not resolve Entra OIDC endpoints (authorize/token passthrough disabled): {ex.Message}");
        }

        var mcpOAuth = state.McpOAuth;
        var azureSection = state.AzureAdSection;

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

        if (mcpOAuth.StripResourceOnAuthorizeRedirect && !string.IsNullOrWhiteSpace(entraAuthorizationEndpoint))
        {
            var entraAuthorizeTarget = entraAuthorizationEndpoint;
            app.MapGet("/oauth2/authorize", (HttpRequest request) =>
            {
                var qb = new QueryBuilder();
                foreach (var kv in request.Query)
                {
                    if (string.Equals(kv.Key, "resource", StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (var value in kv.Value)
                    {
                        if (value is null)
                            continue;
                        qb.Add(kv.Key, value);
                    }
                }

                return Results.Redirect(entraAuthorizeTarget + qb.ToString());
            }).AllowAnonymous();

            Console.WriteLine($"[AUTH] Entra AADSTS9010010 workaround: authorization_endpoint → {gatewayAuthorizeUrl} (strips resource= before redirect to Microsoft).");
        }

        if (mcpOAuth.StripResourceOnAuthorizeRedirect && !string.IsNullOrWhiteSpace(entraTokenEndpoint))
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
                    if (string.Equals(key, "resource", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var value = form[key].ToString();
                    if (value.Length > 0)
                        pairs.Add(new KeyValuePair<string, string>(key, value));
                }

                using var client = httpFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("McpGateway/1.0");
                using var upstream = new FormUrlEncodedContent(pairs);
                using var resp = await client.PostAsync(entraTokenEndpoint, upstream, http.RequestAborted);

                http.Response.StatusCode = (int)resp.StatusCode;
                if (resp.Content.Headers.ContentType is { } ct)
                    http.Response.ContentType = ct.ToString();
                await resp.Content.CopyToAsync(http.Response.Body, http.RequestAborted);
            }).AllowAnonymous();

            Console.WriteLine($"[AUTH] Entra token proxy: token_endpoint → {gatewayTokenUrl} (strips resource= on POST to Microsoft).");
        }

        app.MapPost("/oauth/register", async (HttpContext context) =>
        {
            var clientId = ResolveDcrClientId(mcpOAuth, azureSection, state.AzureClientId);
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

        Console.WriteLine($"[AUTH] OAuth DCR shim: registration_endpoint={registrationEndpoint} (RFC 7591 POST returns pre-registered Entra client_id).");
    }

    private static string? ResolveDcrClientId(
        McpGatewayOAuthOptions mcpOAuth,
        IConfigurationSection azureSection,
        string? azureClientId) =>
        string.IsNullOrWhiteSpace(mcpOAuth.DynamicClientRegistrationClientId)
            ? (string.IsNullOrWhiteSpace(azureSection["SpaClientId"]) ? azureClientId : azureSection["SpaClientId"])
            : mcpOAuth.DynamicClientRegistrationClientId;
}
