using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Proxying;

/// <summary>
/// Rewrites relative MCP endpoints inside SSE <c>data:</c> lines so clients keep talking to the gateway.
/// </summary>
public sealed class SseEndpointRewriter(ILogger<SseEndpointRewriter> logger)
{
    private const string SseDataPrefix = "data: ";

    public string RewriteLine(string line, string adapterName)
    {
        if (!line.StartsWith(SseDataPrefix))
        {
            return line;
        }

        var data = line.Substring(SseDataPrefix.Length).Trim();

        if (ShouldRewritePath(data))
        {
            return RewriteDirectPath(data, adapterName);
        }

        if (IsJsonObject(data))
        {
            return RewriteJsonEndpoint(line, data, adapterName);
        }

        return line;
    }

    private static bool ShouldRewritePath(string data)
    {
        return data.StartsWith('/') && !data.StartsWith("/adapters/");
    }

    private static bool IsJsonObject(string data)
    {
        return data.StartsWith('{') && data.EndsWith('}');
    }

    private string RewriteDirectPath(string data, string adapterName)
    {
        var rewrittenPath = $"/adapters/{adapterName}{data}";
        logger.LogDebug("Rewriting SSE endpoint: {Original} -> {Rewritten}", data, rewrittenPath);
        return $"{SseDataPrefix}{rewrittenPath}";
    }

    private string RewriteJsonEndpoint(string originalLine, string data, string adapterName)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (TryExtractEndpoint(root, out var endpoint) && ShouldRewritePath(endpoint))
            {
                var rewrittenEndpoint = $"/adapters/{adapterName}{endpoint}";
                var newJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["endpoint"] = rewrittenEndpoint
                });

                logger.LogDebug("Rewriting SSE JSON endpoint: {Original} -> {Rewritten}", endpoint, rewrittenEndpoint);
                return $"{SseDataPrefix}{newJson}";
            }
        }
        catch (JsonException)
        {
            logger.LogDebug("Failed to parse SSE JSON data, returning original: {Data}", data);
        }

        return originalLine;
    }

    private static bool TryExtractEndpoint(JsonElement root, out string endpoint)
    {
        if (root.TryGetProperty("endpoint", out var endpointProp))
        {
            endpoint = endpointProp.GetString() ?? string.Empty;
            return !string.IsNullOrEmpty(endpoint);
        }

        endpoint = string.Empty;
        return false;
    }
}
