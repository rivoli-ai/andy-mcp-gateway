using System.Text.Json;
using McpGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

/// <summary>
/// API Controller for proxying requests to MCP adapters.
/// Handles request forwarding, streaming, and message routing to registered adapters.
/// </summary>
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
    public async Task<IActionResult> ForwardSseRequest(string adapterName)
    {
        var result = await proxyService.ForwardSseRequestAsync(adapterName, HttpContext);
        return ConvertProxyResult(result);
    }

    [AllowAnonymous]
    [HttpPost("{name}/mcp")]
    [HttpGet("{name}/mcp")]
    public async Task<IActionResult> ForwardStreamableHttpRequest(string name, CancellationToken cancellationToken)
    {
        var result = await proxyService.ForwardStreamableHttpRequestAsync(name, HttpContext, cancellationToken);
        return ConvertProxyResult(result);
    }


    /// <summary>
    /// Forward messages to an MCP adapter
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{adapterName}/messages")]
    public async Task<IActionResult> SendMessages(string adapterName)
    {
        try
        {
            var method = BuildMethodWithQueryString("messages");
            var result = await proxyService.ForwardRequestAsync(adapterName, HttpContext, method, retry: true);
            return ConvertProxyResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding messages to adapter {AdapterName}", adapterName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }


    /// <summary>
    /// Forward messages to an MCP adapter
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{adapterName}/message")]
    public async Task<IActionResult> SendMessage(string adapterName)
    {
        try
        {
            var method = BuildMethodWithQueryString("message");
            var result = await proxyService.ForwardRequestAsync(adapterName,HttpContext, method, retry: true);
            return ConvertProxyResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding messages to adapter {AdapterName}", adapterName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }


    private string BuildMethodWithQueryString(string method)
    {
        return !Request.QueryString.HasValue ? method :
            $"{method}{(method.Contains('?') ? "&" : "?")}{Request.QueryString.Value.TrimStart('?')}";
    }

    private IActionResult ConvertProxyResult(ProxyResult result)
    {
        if (result.Success)
        {
            if (result.JsonContent.HasValue)
                return Ok(result.JsonContent.Value);
            return Ok(result.Content);
        }
        return StatusCode(result.StatusCode, new { error = result.Error });
    }

}
