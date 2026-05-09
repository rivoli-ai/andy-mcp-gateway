namespace McpGateway.Application.Auth;

/// <summary>Entra v2 expects API scopes as <c>api://{app-id}/scope-name</c> in protected-resource metadata.</summary>
public static class McpOAuthScopeHelper
{
    public static string[] ForEntraProtectedResourceMetadata(string[] scopes, string apiClientIdForUri)
    {
        if (scopes is not { Length: > 0 } || string.IsNullOrWhiteSpace(apiClientIdForUri))
            return scopes;

        var appId = apiClientIdForUri.Trim();
        return scopes.Select(s =>
        {
            var t = (s ?? "").Trim();
            if (t.Length == 0)
                return t;
            if (t.Contains("://", StringComparison.Ordinal))
                return t;
            return $"api://{appId}/{t.TrimStart('/')}";
        }).ToArray();
    }
}
