using McpGateway.Application.Interfaces;
using McpGateway.Application.Proxying;
using McpGateway.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

/// <summary>
/// MCP transport surface — gateway-public routes that forward to the upstream adapter
/// configured under <c>{adapterName}</c>. Authentication is enforced by
/// <see cref="McpTransportAuthorizationPolicy.Name"/>.
/// </summary>
[ApiController]
[Authorize(Policy = McpTransportAuthorizationPolicy.Name)]
[Route("adapters")]
public sealed class ProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IProxyService proxyService, ILogger<ProxyController> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    /// <summary>Open a Server-Sent Events stream from the adapter to the client.</summary>
    [HttpGet("{adapterName}/sse")]
    public async Task ForwardSseRequest(string adapterName)
    {
        try
        {
            await _proxyService.ForwardSseRequestAsync(adapterName, HttpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE request to {AdapterName}", adapterName);
        }
    }

    /// <summary>Forward a streamable HTTP MCP exchange to the adapter.</summary>
    [HttpGet("{adapterName}/mcp")]
    [HttpPost("{adapterName}/mcp")]
    public async Task ForwardStreamableHttpRequest(string adapterName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP request to {AdapterName} started at {Time}", adapterName, DateTime.UtcNow);

        try
        {
            using var registration = cancellationToken.Register(() =>
                _logger.LogWarning("MCP request to {AdapterName} cancelled at {Time}", adapterName, DateTime.UtcNow));

            await _proxyService.ForwardStreamableHttpRequestAsync(adapterName, HttpContext, cancellationToken);

            _logger.LogInformation("MCP request to {AdapterName} completed at {Time}", adapterName, DateTime.UtcNow);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MCP request to {AdapterName} was cancelled", adapterName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in streamable HTTP request to {AdapterName}", adapterName);
        }
    }

    /// <summary>Forward a POST /messages payload to the adapter, with retry.</summary>
    [HttpPost("{adapterName}/messages")]
    public Task SendMessages(string adapterName) => ForwardWithErrorHandlingAsync(adapterName, "messages");

    /// <summary>Forward a POST /message payload to the adapter, with retry.</summary>
    [HttpPost("{adapterName}/message")]
    public Task SendMessage(string adapterName) => ForwardWithErrorHandlingAsync(adapterName, "message");

    private async Task ForwardWithErrorHandlingAsync(string adapterName, string endpointName)
    {
        try
        {
            var endpoint = AppendQueryString(endpointName);
            await _proxyService.ForwardRequestAsync(adapterName, HttpContext, endpoint, retry: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding {Endpoint} to adapter {AdapterName}", endpointName, adapterName);
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
