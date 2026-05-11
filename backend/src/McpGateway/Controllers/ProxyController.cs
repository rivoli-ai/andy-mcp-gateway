using McpGateway.Application.Bridging;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Proxying;
using McpGateway.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

/// <summary>
/// MCP transport surface. The gateway exposes <b>both</b> the legacy SSE pair
/// (<c>/adapters/{name}/sse</c> + <c>/adapters/{name}/messages</c>) and the modern
/// streamable-HTTP endpoint (<c>/adapters/{name}/mcp</c>) for <i>every</i> adapter,
/// regardless of which protocol the upstream actually speaks — the <see cref="IMcpBridgeService"/>
/// translates between the two transports when they don't match.
/// </summary>
[ApiController]
[Authorize(Policy = McpTransportAuthorizationPolicy.Name)]
[Route("adapters")]
public sealed class ProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly IMcpBridgeService _bridge;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IProxyService proxyService, IMcpBridgeService bridge, ILogger<ProxyController> logger)
    {
        _proxyService = proxyService;
        _bridge = bridge;
        _logger = logger;
    }

    /// <summary>SSE client opens the long-lived event stream.</summary>
    [HttpGet("{adapterName}/sse")]
    public Task OpenSseStream(string adapterName, CancellationToken cancellationToken) =>
        _bridge.HandleSseStreamAsync(adapterName, HttpContext, cancellationToken);

    /// <summary>SSE client POSTs a JSON-RPC frame for the upstream MCP server.</summary>
    [HttpPost("{adapterName}/messages")]
    public Task PostSseMessages(string adapterName, CancellationToken cancellationToken) =>
        _bridge.HandleSseMessagesAsync(adapterName, HttpContext, cancellationToken);

    /// <summary>
    /// Streamable-HTTP client POSTs JSON-RPC. Returns either a single JSON response or
    /// (if the upstream is SSE) the same response after the gateway demultiplexes the
    /// upstream event stream by JSON-RPC id.
    /// </summary>
    [HttpPost("{adapterName}/mcp")]
    public Task PostStreamable(string adapterName, CancellationToken cancellationToken) =>
        _bridge.HandleStreamablePostAsync(adapterName, HttpContext, cancellationToken);

    /// <summary>Streamable-HTTP client opens the server-initiated event stream on the MCP endpoint.</summary>
    [HttpGet("{adapterName}/mcp")]
    public Task OpenStreamableServerStream(string adapterName, CancellationToken cancellationToken) =>
        _bridge.HandleStreamableGetAsync(adapterName, HttpContext, cancellationToken);

    /// <summary>Legacy passthrough route — kept for compatibility with the previous proxy.</summary>
    [HttpPost("{adapterName}/message")]
    public async Task SendMessage(string adapterName)
    {
        try
        {
            var endpoint = AppendQueryString("message");
            await _proxyService.ForwardRequestAsync(adapterName, HttpContext, endpoint, retry: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding message to adapter {AdapterName}", adapterName);
            await McpProxyErrors.WriteJsonErrorAsync(HttpContext, StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    private string AppendQueryString(string endpoint)
    {
        if (!Request.QueryString.HasValue)
            return endpoint;

        var separator = endpoint.Contains('?') ? '&' : '?';
        return $"{endpoint}{separator}{Request.QueryString.Value!.TrimStart('?')}";
    }
}
