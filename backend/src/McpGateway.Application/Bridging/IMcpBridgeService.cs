using Microsoft.AspNetCore.Http;

namespace McpGateway.Application.Bridging;

/// <summary>
/// Public entry points for cross-transport MCP bridging. Each method maps one client-side
/// route to the right downstream transport, falling back to passthrough when the client
/// and upstream already speak the same transport.
/// </summary>
public interface IMcpBridgeService
{
    /// <summary>Streamable-HTTP client POSTing JSON-RPC frames to <c>/adapters/{name}/mcp</c>.</summary>
    Task HandleStreamablePostAsync(string adapterName, HttpContext context, CancellationToken cancellationToken);

    /// <summary>Streamable-HTTP client opening the server-initiated GET stream on <c>/adapters/{name}/mcp</c>.</summary>
    Task HandleStreamableGetAsync(string adapterName, HttpContext context, CancellationToken cancellationToken);

    /// <summary>SSE client opening the event stream on <c>/adapters/{name}/sse</c>.</summary>
    Task HandleSseStreamAsync(string adapterName, HttpContext context, CancellationToken cancellationToken);

    /// <summary>SSE client POSTing a JSON-RPC frame to <c>/adapters/{name}/messages</c>.</summary>
    Task HandleSseMessagesAsync(string adapterName, HttpContext context, CancellationToken cancellationToken);
}
