using McpGateway.Application.Interfaces;
using McpGateway.Application.Proxying;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace McpGateway.Application.Services;

/// <summary>
/// Orchestrates MCP adapter proxying: resolves adapter config, delegates HTTP/SSE mechanics to <see cref="McpProxyHttp"/> and <see cref="SseProxyStream"/>.
/// Authentication and authorization belong in ASP.NET middleware / filters — this type only forwards after the request reaches the controller.
/// </summary>
public sealed class ProxyService(
    IMcpAdapterRepository adapterRepository,
    ILogger<ProxyService> logger,
    SseProxyStream sseProxyStream)
    : IProxyService
{
    /// <summary>Obsolete: use <see cref="McpProxyHttp.CreateProxiedRequest"/>.</summary>
    [Obsolete("Use McpProxyHttp.CreateProxiedRequest instead")]
    public static HttpRequestMessage CreateProxiedHttpRequest(HttpContext context, Func<Uri, Uri>? targetOverride = null) =>
        McpProxyHttp.CreateProxiedRequest(context, targetOverride);

    /// <summary>Obsolete: use <see cref="McpProxyHttp.CopyResponseToContextAsync"/>.</summary>
    [Obsolete("Use McpProxyHttp.CopyResponseToContextAsync instead")]
    public static Task CopyProxiedHttpResponseAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken) =>
        McpProxyHttp.CopyResponseToContextAsync(context, response, cancellationToken);

    public static HttpRequestMessage CreateProxiedRequest(HttpContext context, Func<Uri, Uri>? targetOverride = null) =>
        McpProxyHttp.CreateProxiedRequest(context, targetOverride);

    public static Task CopyResponseToContextAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken) =>
        McpProxyHttp.CopyResponseToContextAsync(context, response, cancellationToken);

    public async Task ForwardSseRequestAsync(string adapterName, HttpContext httpContext)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName).ConfigureAwait(false);
        if (adapter == null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(httpContext, StatusCodes.Status404NotFound,
                $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return;
        }

        try
        {
            ConfigureSseResponse(httpContext);

            using var client = McpProxyHttp.CreateStreamingHttpClient();
            using var proxiedRequest = McpProxyHttp.CreateProxiedRequest(httpContext, uri => McpProxyHttp.BuildAdapterUri(uri, adapter.Url));

            using var response = await McpProxyHttp.SendWithHeadersReadAsync(client, proxiedRequest, httpContext.RequestAborted)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await sseProxyStream.CopyToClientAsync(response, httpContext, adapterName, httpContext.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("SSE connection to {AdapterName} was cancelled by client", adapterName);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error connecting to adapter {AdapterName}", adapterName);
            await McpProxyErrors.TryWriteSseErrorEventAsync(httpContext, "Connection failed to upstream server").ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogWarning("SSE connection to {AdapterName} timed out", adapterName);
            await McpProxyErrors.TryWriteSseErrorEventAsync(httpContext, "Connection timeout").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in SSE connection to {AdapterName}", adapterName);
            await McpProxyErrors.TryWriteSseErrorEventAsync(httpContext, "Internal server error").ConfigureAwait(false);
        }
    }

    public async Task ForwardStreamableHttpRequestAsync(string adapterName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName).ConfigureAwait(false);
        if (adapter == null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(httpContext, StatusCodes.Status503ServiceUnavailable,
                $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return;
        }

        HttpClient? client = null;
        HttpRequestMessage? proxiedRequest = null;
        HttpResponseMessage? response = null;

        try
        {
            proxiedRequest = McpProxyHttp.CreateProxiedRequest(httpContext, uri => McpProxyHttp.BuildAdapterUri(uri, adapter.Url));
            client = McpProxyHttp.CreateStreamingHttpClient();

            response = await McpProxyHttp.SendWithHeadersReadAsync(client, proxiedRequest, cancellationToken).ConfigureAwait(false);

            await McpProxyHttp.CopyResponseToContextAsync(httpContext, response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Streamable request to adapter {AdapterName} was cancelled by client", adapterName);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Streamable request to adapter {AdapterName} timed out or was cancelled", adapterName);
        }
        catch (IOException ex) when (ex.InnerException is OperationCanceledException || cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Streamable request to adapter {AdapterName} encountered I/O error (client disconnect)", adapterName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding streamable HTTP request to adapter {AdapterName}", adapterName);

            if (!httpContext.Response.HasStarted)
            {
                await McpProxyErrors.WriteJsonErrorAsync(httpContext, 500, "Internal server error").ConfigureAwait(false);
            }
        }
        finally
        {
            response?.Dispose();
            proxiedRequest?.Dispose();
            client?.Dispose();
        }
    }

    public async Task ForwardRequestAsync(string adapterName, HttpContext context, string endpoint, bool retry = false)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName).ConfigureAwait(false);
        if (adapter == null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(context, 404, $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return;
        }

        if (!adapter.Enabled)
        {
            await McpProxyErrors.WriteJsonErrorAsync(context, 400, $"Adapter '{adapterName}' is disabled").ConfigureAwait(false);
            return;
        }

        if (retry)
        {
            await McpProxyRetry.ExecuteWithRetryAsync(() => SendPostRequestAsync(adapter, context, endpoint)).ConfigureAwait(false);
        }
        else
        {
            await SendPostRequestAsync(adapter, context, endpoint).ConfigureAwait(false);
        }
    }

    /// <summary>For internal or future use; forwards GET and returns a <see cref="ProxyResult"/>.</summary>
    public async Task<ProxyResult> ForwardGetRequestAsync(string adapterName, string endpoint, bool retry = false)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName).ConfigureAwait(false);
        if (adapter == null)
        {
            return McpProxyErrors.NotFound(adapterName);
        }

        if (!IsAdapterEnabled(adapter))
        {
            return McpProxyErrors.DisabledAdapter(adapterName);
        }

        return retry
            ? await McpProxyRetry.ExecuteWithRetryAsync(
                () => SendGetRequestAsync(adapter, endpoint),
                McpProxyErrors.ServiceUnavailable("Service temporarily unavailable after retries")).ConfigureAwait(false)
            : await SendGetRequestAsync(adapter, endpoint).ConfigureAwait(false);
    }

    public async Task<bool> IsAdapterAvailableAsync(string adapterName)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName).ConfigureAwait(false);
        return adapter != null && IsAdapterEnabled(adapter) && adapter.IsHealthy;
    }

    private async Task<McpAdapter?> GetValidatedAdapterAsync(string adapterName) =>
        await adapterRepository.GetByNameAsync(adapterName).ConfigureAwait(false);

    private static bool IsAdapterEnabled(McpAdapter adapter) => adapter.Enabled;

    private static void ConfigureSseResponse(HttpContext httpContext)
    {
        var headers = httpContext.Response.Headers;
        headers.Append("Content-Type", "text/event-stream");
        headers.Append("Cache-Control", "no-cache");
        headers.Append("Connection", "keep-alive");
        headers.Append("Access-Control-Allow-Origin", "*");
        headers.Add("X-Accel-Buffering", "no");
    }

    private async Task SendPostRequestAsync(McpAdapter adapter, HttpContext context, string endpoint)
    {
        try
        {
            using var client = McpProxyHttp.CreateStreamingHttpClient();
            using var proxiedRequest = McpProxyHttp.CreateProxiedRequest(context, uri => McpProxyHttp.BuildAdapterUri(uri, adapter.Url));
            using var response = await client.SendAsync(proxiedRequest, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
                .ConfigureAwait(false);

            await McpProxyHttp.CopyResponseToContextAsync(context, response, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send POST request to adapter {AdapterName} at endpoint {Endpoint}",
                adapter.Name, endpoint);

            await McpProxyErrors.WriteJsonErrorAsync(context, 500, $"Request failed: {ex.Message}").ConfigureAwait(false);
            throw;
        }
    }

    private async Task<ProxyResult> SendGetRequestAsync(McpAdapter adapter, string endpoint)
    {
        try
        {
            var url = $"{adapter.Url.TrimEnd('/')}/{endpoint}";

            using var client = McpProxyHttp.CreateHttpClient(adapter.TimeoutSeconds);
            using var response = await client.GetAsync(url).ConfigureAwait(false);

            return await ProcessHttpResponseAsync(response, adapter.Name).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send GET request to adapter {AdapterName} at endpoint {Endpoint}", adapter.Name, endpoint);
            return new ProxyResult
            {
                Success = false,
                StatusCode = 500,
                Error = $"Request failed: {ex.Message}"
            };
        }
    }

    private async Task<ProxyResult> ProcessHttpResponseAsync(HttpResponseMessage response, string adapterName)
    {
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var jsonContent = JsonSerializer.Deserialize<JsonElement>(content);
                return new ProxyResult
                {
                    Success = true,
                    StatusCode = statusCode,
                    JsonContent = jsonContent
                };
            }
            catch
            {
                return new ProxyResult
                {
                    Success = true,
                    StatusCode = statusCode,
                    Content = content
                };
            }
        }

        logger.LogWarning("MCP server {Server} returned {Status}: {Content}", adapterName, response.StatusCode, content);
        return McpProxyErrors.Error(statusCode, content);
    }
}
