using Microsoft.AspNetCore.Http;

namespace McpGateway.Application.Interfaces;

/// <summary>
/// Forwards inbound HTTP requests on adapter routes to the configured upstream MCP server.
/// Implementations cover Server-Sent Events, streamable HTTP, and one-shot POSTs.
/// </summary>
public interface IProxyService
{
    Task ForwardSseRequestAsync(string adapterName, HttpContext httpContext);

    Task ForwardStreamableHttpRequestAsync(string adapterName, HttpContext httpContext, CancellationToken cancellationToken);

    Task ForwardRequestAsync(string adapterName, HttpContext context, string endpoint, bool retry = false);

    Task<bool> IsAdapterAvailableAsync(string adapterName);
}
