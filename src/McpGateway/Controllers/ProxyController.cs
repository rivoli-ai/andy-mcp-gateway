using System.Text.Json;
using McpGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

[ApiController]
[Authorize]
[Route("adapters")]
public class ProxyController(
    IProxyService proxyService,
    IMcpAdapterService adapterService,
    ILogger<ProxyController> logger)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{adapterName}/sse")]
    public async Task ForwardSseRequest(string adapterName)
    {
        try
        {
            await proxyService.ForwardSseRequestAsync(adapterName, HttpContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SSE request to {AdapterName}", adapterName);
        }
    }

    [AllowAnonymous]
[HttpPost("{name}/mcp")]
[HttpGet("{name}/mcp")]
public async Task ForwardStreamableHttpRequest(string name, CancellationToken cancellationToken)
{
    logger.LogInformation("Request started at {Time}", DateTime.UtcNow);
    
    try
    {
        // Log when cancellation is requested
        using (cancellationToken.Register(() => 
            logger.LogWarning("Cancellation requested at {Time}", DateTime.UtcNow)))
        {
            await proxyService.ForwardStreamableHttpRequestAsync(name, HttpContext, cancellationToken);
        }
        
        logger.LogInformation("Request completed at {Time}", DateTime.UtcNow);
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("Request was cancelled");
        throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in streamable HTTP request to {AdapterName}", name);
    }
}

    /// <summary>
    /// Forward messages to an MCP adapter
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{adapterName}/messages")]
    public async Task SendMessages(string adapterName)
    {
        try
        {
            var method = BuildMethodWithQueryString("messages");
            await proxyService.ForwardRequestAsync(adapterName, HttpContext, method, retry: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding messages to adapter {AdapterName}", adapterName);
            
            // Only write error if response hasn't started
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 500;
                HttpContext.Response.ContentType = "application/json";
                var errorJson = JsonSerializer.Serialize(new { error = "Internal server error" });
                await HttpContext.Response.WriteAsync(errorJson);
            }
        }
    }

    /// <summary>
    /// Forward message to an MCP adapter
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{adapterName}/message")]
    public async Task SendMessage(string adapterName)
    {
        try
        {
            var method = BuildMethodWithQueryString("message");
            await proxyService.ForwardRequestAsync(adapterName, HttpContext, method, retry: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding message to adapter {AdapterName}", adapterName);
            
            // Only write error if response hasn't started
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 500;
                HttpContext.Response.ContentType = "application/json";
                var errorJson = JsonSerializer.Serialize(new { error = "Internal server error" });
                await HttpContext.Response.WriteAsync(errorJson);
            }
        }
    }

    private string BuildMethodWithQueryString(string method)
    {
        return !Request.QueryString.HasValue ? method :
            $"{method}{(method.Contains('?') ? "&" : "?")}{Request.QueryString.Value.TrimStart('?')}";
    }
}