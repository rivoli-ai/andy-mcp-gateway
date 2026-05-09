using McpGateway.Application.Interfaces;
using McpGateway.Application.Proxying;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Services;

/// <summary>
/// Orchestrates MCP adapter proxying: resolves adapter config, then delegates HTTP/SSE
/// mechanics to <see cref="McpProxyHttp"/> and <see cref="SseProxyStream"/>. Authentication
/// and authorization belong upstream in the ASP.NET pipeline.
/// </summary>
public sealed class ProxyService : IProxyService
{
    private readonly IMcpAdapterRepository _adapters;
    private readonly ILogger<ProxyService> _logger;
    private readonly SseProxyStream _sseProxyStream;

    public ProxyService(
        IMcpAdapterRepository adapters,
        ILogger<ProxyService> logger,
        SseProxyStream sseProxyStream)
    {
        _adapters = adapters;
        _logger = logger;
        _sseProxyStream = sseProxyStream;
    }

    public async Task ForwardSseRequestAsync(string adapterName, HttpContext httpContext)
    {
        var adapter = await _adapters.GetByNameAsync(adapterName).ConfigureAwait(false);
        if (adapter is null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(httpContext, StatusCodes.Status404NotFound,
                $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return;
        }

        try
        {
            ConfigureSseResponseHeaders(httpContext);

            using var client = McpProxyHttp.CreateStreamingHttpClient();
            using var request = McpProxyHttp.CreateProxiedRequest(httpContext, uri => McpProxyHttp.BuildAdapterUri(uri, adapter.Url));
            using var response = await McpProxyHttp.SendWithHeadersReadAsync(client, request, httpContext.RequestAborted)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await _sseProxyStream.CopyToClientAsync(response, httpContext, adapterName, httpContext.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("SSE connection to {AdapterName} was cancelled by client", adapterName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error connecting to adapter {AdapterName}", adapterName);
            await McpProxyErrors.TryWriteSseErrorEventAsync(httpContext, "Connection failed to upstream server").ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("SSE connection to {AdapterName} timed out", adapterName);
            await McpProxyErrors.TryWriteSseErrorEventAsync(httpContext, "Connection timeout").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SSE connection to {AdapterName}", adapterName);
            await McpProxyErrors.TryWriteSseErrorEventAsync(httpContext, "Internal server error").ConfigureAwait(false);
        }
    }

    public async Task ForwardStreamableHttpRequestAsync(string adapterName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var adapter = await _adapters.GetByNameAsync(adapterName).ConfigureAwait(false);
        if (adapter is null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(httpContext, StatusCodes.Status503ServiceUnavailable,
                $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return;
        }

        HttpClient? client = null;
        HttpRequestMessage? request = null;
        HttpResponseMessage? response = null;

        try
        {
            client = McpProxyHttp.CreateStreamingHttpClient();
            request = McpProxyHttp.CreateProxiedRequest(httpContext, uri => McpProxyHttp.BuildAdapterUri(uri, adapter.Url));
            response = await McpProxyHttp.SendWithHeadersReadAsync(client, request, cancellationToken).ConfigureAwait(false);

            await McpProxyHttp.CopyResponseToContextAsync(httpContext, response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Streamable request to adapter {AdapterName} was cancelled by client", adapterName);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Streamable request to adapter {AdapterName} timed out or was cancelled", adapterName);
        }
        catch (IOException ex) when (ex.InnerException is OperationCanceledException || cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Streamable request to adapter {AdapterName} encountered I/O error (client disconnect)", adapterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding streamable HTTP request to adapter {AdapterName}", adapterName);
            await McpProxyErrors.WriteJsonErrorAsync(httpContext, StatusCodes.Status500InternalServerError, "Internal server error")
                .ConfigureAwait(false);
        }
        finally
        {
            response?.Dispose();
            request?.Dispose();
            client?.Dispose();
        }
    }

    public async Task ForwardRequestAsync(string adapterName, HttpContext context, string endpoint, bool retry = false)
    {
        var adapter = await _adapters.GetByNameAsync(adapterName).ConfigureAwait(false);
        if (adapter is null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(context, StatusCodes.Status404NotFound,
                $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return;
        }

        if (!adapter.Enabled)
        {
            await McpProxyErrors.WriteJsonErrorAsync(context, StatusCodes.Status400BadRequest,
                $"Adapter '{adapterName}' is disabled").ConfigureAwait(false);
            return;
        }

        Func<Task> send = () => SendPostAsync(adapter, context, endpoint);
        if (retry)
            await McpProxyRetry.ExecuteWithRetryAsync(send).ConfigureAwait(false);
        else
            await send().ConfigureAwait(false);
    }

    public async Task<bool> IsAdapterAvailableAsync(string adapterName)
    {
        var adapter = await _adapters.GetByNameAsync(adapterName).ConfigureAwait(false);
        return adapter is { Enabled: true, IsHealthy: true };
    }

    private async Task SendPostAsync(McpAdapter adapter, HttpContext context, string endpoint)
    {
        try
        {
            using var client = McpProxyHttp.CreateStreamingHttpClient();
            using var request = McpProxyHttp.CreateProxiedRequest(context, uri => McpProxyHttp.BuildAdapterUri(uri, adapter.Url));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
                .ConfigureAwait(false);

            await McpProxyHttp.CopyResponseToContextAsync(context, response, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send POST request to adapter {AdapterName} at endpoint {Endpoint}",
                adapter.Name, endpoint);

            await McpProxyErrors.WriteJsonErrorAsync(context, StatusCodes.Status500InternalServerError,
                $"Request failed: {ex.Message}").ConfigureAwait(false);
            throw;
        }
    }

    private static void ConfigureSseResponseHeaders(HttpContext httpContext)
    {
        var headers = httpContext.Response.Headers;
        headers.Append("Content-Type", "text/event-stream");
        headers.Append("Cache-Control", "no-cache");
        headers.Append("Connection", "keep-alive");
        headers.Append("Access-Control-Allow-Origin", "*");
        headers["X-Accel-Buffering"] = "no";
    }
}
