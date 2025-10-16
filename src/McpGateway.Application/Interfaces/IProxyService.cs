using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace McpGateway.Application.Interfaces;

/// <summary>
/// Service interface for proxying HTTP requests to MCP adapters.
/// Handles request forwarding, streaming, and health checking for adapters.
/// </summary>
public interface IProxyService
{
    Task<ProxyResult> ForwardRequestAsync(string adapterName, HttpContext context, string endpoint,bool retry = false);
    Task<bool> IsAdapterAvailableAsync(string adapterName);
    Task<ProxyResult> ForwardSseRequestAsync(string adapterName, HttpContext httpContext);
    Task<ProxyResult> ForwardStreamableHttpRequestAsync(string adapterName, HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>
/// Result object containing the outcome of a proxy operation.
/// Includes success status, HTTP status code, content, and error information.
/// </summary>
public class ProxyResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Content { get; set; }
    public JsonElement? JsonContent { get; set; }
    public string? Error { get; set; }
}
