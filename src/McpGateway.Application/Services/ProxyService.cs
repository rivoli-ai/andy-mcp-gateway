using McpGateway.Application.Interfaces;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace McpGateway.Application.Services;

/// <summary>
/// Service responsible for proxying HTTP requests to MCP adapters, including support for Server-Sent Events (SSE) streaming.
/// </summary>
public class ProxyService(
    IMcpAdapterRepository adapterRepository,
    ILogger<ProxyService> logger)
    : IProxyService
{
    private const string SseDataPrefix = "data: ";
    private const int DefaultRetryAttempts = 2;
    private const int BaseRetryDelayMs = 100;

    /// <summary>
    /// Forwards a streamable HTTP request with Server-Sent Events (SSE) support to the specified adapter.
    /// </summary>
    /// <param name="adapterName">The name of the target adapter</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <returns>A ProxyResult indicating the outcome of the operation</returns>
    public async Task<ProxyResult> ForwardSseRequestAsync(string adapterName, HttpContext httpContext)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName);
        if (adapter == null)
        {
            return CreateNotFoundResult(adapterName);
        }

        try
        {
 
            ConfigureSseResponse(httpContext);

            using var client = CreateStreamingHttpClient();
            var proxiedRequest = CreateProxiedRequest(httpContext, uri => BuildAdapterUri(uri, adapter.Url));

            using var response = await SendStreamingRequestAsync(client, proxiedRequest, httpContext.RequestAborted);

            response.EnsureSuccessStatusCode();

            await ProcessSseStreamAsync(response, httpContext, adapterName);

            return CreateSuccessResult(200, "SSE connection completed successfully");
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            return HandleClientDisconnection(adapterName);
        }
        catch (HttpRequestException ex)
        {
            return await HandleConnectionErrorAsync(ex, httpContext, adapterName);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return await HandleRequestTimeoutAsync(httpContext, adapterName);
        }
        catch (Exception ex)
        {
            return await HandleUnexpectedErrorAsync(ex, httpContext, adapterName);
        }
    }

    /// <summary>
    /// Forwards a streamable HTTP request to the specified adapter with cancellation support.
    /// </summary>
    /// <param name="adapterName">The name of the target adapter</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A ProxyResult indicating the outcome of the operation</returns>
    public async Task<ProxyResult> ForwardStreamableHttpRequestAsync(string adapterName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName);
        if (adapter == null)
        {
            SetResponseStatus(httpContext, StatusCodes.Status503ServiceUnavailable);
            return CreateServiceUnavailableResult($"Adapter '{adapterName}' not found");
        }

        try
        {
      
            var proxiedRequest = CreateProxiedRequest(httpContext, uri => BuildAdapterUri(uri, adapter.Url));

            using var client = CreateStreamingHttpClient();
            using var response = await SendRequestAsync(client, proxiedRequest, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            await CopyResponseToContextAsync(httpContext, response, cancellationToken);

            return CreateProxyResult(response.IsSuccessStatusCode, (int)response.StatusCode,
                response.IsSuccessStatusCode ? "Request forwarded successfully" : "Request failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding streamable HTTP request to adapter {AdapterName}", adapterName);
            return CreateErrorResult(500, ex.Message);
        }
    }

    /// <summary>
    /// Forwards a JSON POST request to the specified adapter endpoint.
    /// </summary>
    /// <param name="adapterName">The name of the target adapter</param>
    /// <param name="endpoint">The endpoint path to forward to</param>
    /// <param name="body">The JSON body to send</param>
    /// <param name="retry">Whether to enable retry logic</param>
    /// <returns>A ProxyResult containing the response from the adapter</returns>
    public async Task<ProxyResult> ForwardRequestAsync(string adapterName, string endpoint, JsonElement body, bool retry = false)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName);
        if (adapter == null)
        {
            return CreateNotFoundResult(adapterName);
        }

        if (!IsAdapterEnabled(adapter))
        {
            return CreateDisabledAdapterResult(adapterName);
        }

        return retry
            ? await ExecuteWithRetryAsync(() => SendPostRequestAsync(adapter, endpoint, body))
            : await SendPostRequestAsync(adapter, endpoint, body);
    }

    /// <summary>
    /// Forwards a GET request to the specified adapter endpoint.
    /// </summary>
    /// <param name="adapterName">The name of the target adapter</param>
    /// <param name="endpoint">The endpoint path to forward to</param>
    /// <param name="retry">Whether to enable retry logic</param>
    /// <returns>A ProxyResult containing the response from the adapter</returns>
    public async Task<ProxyResult> ForwardGetRequestAsync(string adapterName, string endpoint, bool retry = false)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName);
        if (adapter == null)
        {
            return CreateNotFoundResult(adapterName);
        }

        if (!IsAdapterEnabled(adapter))
        {
            return CreateDisabledAdapterResult(adapterName);
        }

        return retry
            ? await ExecuteWithRetryAsync(() => SendGetRequestAsync(adapter, endpoint))
            : await SendGetRequestAsync(adapter, endpoint);
    }

    /// <summary>
    /// Checks if the specified adapter is available and enabled.
    /// </summary>
    /// <param name="adapterName">The name of the adapter to check</param>
    /// <returns>True if the adapter is available and enabled, false otherwise</returns>
    public async Task<bool> IsAdapterAvailableAsync(string adapterName)
    {
        var adapter = await GetValidatedAdapterAsync(adapterName);
        return adapter != null && IsAdapterEnabled(adapter) && adapter.IsHealthy;
    }

    /// <summary>
    /// Retrieves and validates an adapter by name.
    /// </summary>
    /// <param name="adapterName">The name of the adapter to retrieve</param>
    /// <returns>The adapter if found, null otherwise</returns>
    private async Task<McpAdapter?> GetValidatedAdapterAsync(string adapterName)
    {
        return await adapterRepository.GetByNameAsync(adapterName);
    }

    /// <summary>
    /// Checks if an adapter is enabled for use.
    /// </summary>
    /// <param name="adapter">The adapter to check</param>
    /// <returns>True if the adapter is enabled, false otherwise</returns>
    private static bool IsAdapterEnabled(McpAdapter adapter)
    {
        return adapter.Enabled;
    }

    /// <summary>
    /// Configures HTTP response headers for Server-Sent Events.
    /// </summary>
    /// <param name="httpContext">The HTTP context to configure</param>
    private static void ConfigureSseResponse(HttpContext httpContext)
    {
        var headers = httpContext.Response.Headers;
        headers.Append("Content-Type", "text/event-stream");
        headers.Append("Cache-Control", "no-cache");
        headers.Append("Connection", "keep-alive");
        headers.Append("Access-Control-Allow-Origin", "*");
        headers.Add("X-Accel-Buffering", "no");
    }

    /// <summary>
    /// Processes a Server-Sent Events stream from the upstream server.
    /// </summary>
    /// <param name="response">The HTTP response containing the SSE stream</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="adapterName">The name of the adapter being proxied</param>
    private async Task ProcessSseStreamAsync(HttpResponseMessage response, HttpContext httpContext, string adapterName)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!httpContext.RequestAborted.IsCancellationRequested)
        {
            var line = await ReadSseLineAsync(reader, adapterName);
            if (line == null)
            {
                logger.LogDebug("SSE stream from {AdapterName} reached end of stream", adapterName);
                break;
            }

            if (!await WriteSseLineToResponseAsync(line, httpContext, adapterName))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Reads a single line from the SSE stream with error handling.
    /// </summary>
    /// <param name="reader">The stream reader</param>
    /// <param name="adapterName">The name of the adapter for logging</param>
    /// <returns>The line read from the stream, or null if stream ended</returns>
    private async Task<string?> ReadSseLineAsync(StreamReader reader, string adapterName)
    {
        try
        {
            return await reader.ReadLineAsync();
        }
        catch (HttpIOException ex) when (ex.Message.Contains("ResponseEnded"))
        {
            logger.LogDebug("SSE stream from {AdapterName} ended normally", adapterName);
            return null;
        }
    }

    /// <summary>
    /// Writes a single SSE line to the HTTP response after URL rewriting.
    /// </summary>
    /// <param name="line">The line to write</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="adapterName">The name of the adapter for URL rewriting</param>
    /// <returns>True if the line was written successfully, false if client disconnected</returns>
    private async Task<bool> WriteSseLineToResponseAsync(string line, HttpContext httpContext, string adapterName)
    {
        try
        {
            var rewrittenLine = RewriteSseEndpoints(line, adapterName);
            await httpContext.Response.WriteAsync($"{rewrittenLine}\n", httpContext.RequestAborted);
            await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted);
            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Response has already started"))
        {
            logger.LogDebug("Client disconnected from SSE stream {AdapterName}", adapterName);
            return false;
        }
    }

    /// <summary>
    /// Rewrites endpoint URLs in SSE data to include adapter routing.
    /// </summary>
    /// <param name="line">The SSE line to process</param>
    /// <param name="adapterName">The adapter name to inject into URLs</param>
    /// <returns>The line with rewritten URLs</returns>
    private string RewriteSseEndpoints(string line, string adapterName)
    {
        if (!line.StartsWith(SseDataPrefix))
        {
            return line;
        }

        var data = line.Substring(SseDataPrefix.Length).Trim();

        // Handle direct path rewriting
        if (ShouldRewritePath(data))
        {
            return RewriteDirectPath(data, adapterName);
        }

        // Handle JSON object rewriting
        if (IsJsonObject(data))
        {
            return RewriteJsonEndpoint(line, data, adapterName);
        }

        return line;
    }

    /// <summary>
    /// Determines if a path should be rewritten for adapter routing.
    /// </summary>
    /// <param name="data">The data to check</param>
    /// <returns>True if the path should be rewritten</returns>
    private static bool ShouldRewritePath(string data)
    {
        return data.StartsWith("/") && !data.StartsWith("/adapters/");
    }

    /// <summary>
    /// Determines if the data represents a JSON object.
    /// </summary>
    /// <param name="data">The data to check</param>
    /// <returns>True if the data appears to be a JSON object</returns>
    private static bool IsJsonObject(string data)
    {
        return data.StartsWith("{") && data.EndsWith("}");
    }

    /// <summary>
    /// Rewrites a direct path in SSE data.
    /// </summary>
    /// <param name="data">The extracted data portion</param>
    /// <param name="adapterName">The adapter name to inject</param>
    /// <returns>The rewritten SSE line</returns>
    private string RewriteDirectPath(string data, string adapterName)
    {
        var rewrittenPath = $"/adapters/{adapterName}{data}";
        logger.LogDebug("Rewriting SSE endpoint: {Original} -> {Rewritten}", data, rewrittenPath);
        return $"{SseDataPrefix}{rewrittenPath}";
    }

    /// <summary>
    /// Rewrites endpoint URLs within JSON objects in SSE data.
    /// </summary>
    /// <param name="originalLine">The original SSE line</param>
    /// <param name="data">The JSON data to process</param>
    /// <param name="adapterName">The adapter name to inject</param>
    /// <returns>The rewritten SSE line</returns>
    private string RewriteJsonEndpoint(string originalLine, string data, string adapterName)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (TryExtractEndpoint(root, out var endpoint) && ShouldRewritePath(endpoint))
            {
                var rewrittenEndpoint = $"/adapters/{adapterName}{endpoint}";
                var newJson = CreateRewrittenJsonObject(rewrittenEndpoint);

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

    /// <summary>
    /// Attempts to extract an endpoint property from a JSON element.
    /// </summary>
    /// <param name="root">The JSON root element</param>
    /// <param name="endpoint">The extracted endpoint value</param>
    /// <returns>True if an endpoint was successfully extracted</returns>
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

    /// <summary>
    /// Creates a JSON object with a rewritten endpoint property.
    /// </summary>
    /// <param name="rewrittenEndpoint">The new endpoint URL</param>
    /// <returns>Serialized JSON string</returns>
    private static string CreateRewrittenJsonObject(string rewrittenEndpoint)
    {
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["endpoint"] = rewrittenEndpoint
        });
    }

    /// <summary>
    /// Creates an HTTP client configured for streaming operations.
    /// </summary>
    /// <returns>A configured HttpClient instance</returns>
    private static HttpClient CreateStreamingHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        return new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan // SSE streams can run indefinitely
        };
    }

    /// <summary>
    /// Creates an HTTP client configured for standard operations with timeout.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds</param>
    /// <returns>A configured HttpClient instance</returns>
    private static HttpClient CreateHttpClient(int timeoutSeconds)
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
    }


    /// <summary>
    /// Creates a proxied HTTP request message from the current HTTP context.
    /// </summary>
    /// <param name="context">The current HTTP context</param>
    /// <param name="targetOverride">Optional function to override the target URI</param>
    /// <returns>A configured HttpRequestMessage</returns>
    public static HttpRequestMessage CreateProxiedRequest(HttpContext context, Func<Uri, Uri>? targetOverride = null)
    {
        var originalUri = new Uri(context.Request.GetEncodedUrl());
        var targetUri = targetOverride?.Invoke(originalUri) ?? originalUri;

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = targetUri,
            Content = CreateRequestContent(context)
        };

        CopyRequestHeaders(context, requestMessage);
        AddForwardingHeaders(context, requestMessage);

        return requestMessage;
    }

    /// <summary>
    /// Creates HTTP content from the request body if present.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>HttpContent if request has body, null otherwise</returns>
    private static HttpContent? CreateRequestContent(HttpContext context)
    {
        return context.Request.ContentLength > 0 ? new StreamContent(context.Request.Body) : null;
    }

    /// <summary>
    /// Copies request headers from HTTP context to the request message, excluding authorization headers.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="requestMessage">The request message to copy headers to</param>
    private static void CopyRequestHeaders(HttpContext context, HttpRequestMessage requestMessage)
    {
        foreach (var header in context.Request.Headers)
        {
            // Skip the inbound Authorization header for security
            if (string.Equals(header.Key, HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
            }
        }
    }

    /// <summary>
    /// Adds forwarding headers to track the original request.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="requestMessage">The request message to add headers to</param>
    private static void AddForwardingHeaders(HttpContext context, HttpRequestMessage requestMessage)
    {
        var forwardedValue = $"for={context.Connection.RemoteIpAddress};proto={context.Request.Scheme};host={context.Request.Host.Value}";
        requestMessage.Headers.TryAddWithoutValidation("Forwarded", forwardedValue);
    }

    /// <summary>
    /// Sends an HTTP request configured for streaming responses.
    /// </summary>
    /// <param name="client">The HTTP client</param>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The HTTP response message</returns>
    private static async Task<HttpResponseMessage> SendStreamingRequestAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP request and returns the response.
    /// </summary>
    /// <param name="client">The HTTP client</param>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The HTTP response message</returns>
    private static async Task<HttpResponseMessage> SendRequestAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Copies the proxied HTTP response to the current HTTP context.
    /// </summary>
    /// <param name="context">The current HTTP context</param>
    /// <param name="response">The response to copy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task CopyResponseToContextAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        SetResponseStatus(context, (int)response.StatusCode);
        CopyResponseHeaders(context, response);
        await CopyResponseBodyAsync(context, response, cancellationToken);
    }

    /// <summary>
    /// Sets the HTTP response status code.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="statusCode">The status code to set</param>
    private static void SetResponseStatus(HttpContext context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
    }

    /// <summary>
    /// Copies response headers from the proxied response to the current response.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="response">The proxied response</param>
    private static void CopyResponseHeaders(HttpContext context, HttpResponseMessage response)
    {
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        // Remove transfer encoding as it's handled by ASP.NET Core
        context.Response.Headers.Remove(HeaderNames.TransferEncoding);
    }

    /// <summary>
    /// Copies the response body from the proxied response to the current response.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="response">The proxied response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private static async Task CopyResponseBodyAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
    }

    /// <summary>
    /// Builds a new URI by replacing the address portion while preserving the path structure.
    /// </summary>
    /// <param name="originalUri">The original URI</param>
    /// <param name="newAddress">The new base address</param>
    /// <returns>A URI with the new address and adjusted path</returns>
    private static Uri BuildAdapterUri(Uri originalUri, string newAddress)
    {
        ArgumentNullException.ThrowIfNull(originalUri);
        ArgumentException.ThrowIfNullOrEmpty(newAddress);

        var pathSegments = ExtractPathSegments(originalUri);
        var newBaseUri = new Uri(newAddress, UriKind.Absolute);
        var adjustedPath = BuildAdjustedPath(pathSegments);

        return CreateUriWithPath(newBaseUri, adjustedPath, originalUri);
    }

    /// <summary>
    /// Extracts path segments from a URI, excluding empty segments.
    /// </summary>
    /// <param name="uri">The URI to extract segments from</param>
    /// <returns>Array of path segments</returns>
    private static string[] ExtractPathSegments(Uri uri)
    {
        return uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Builds an adjusted path by skipping the first two segments and handling special cases.
    /// </summary>
    /// <param name="segments">The path segments</param>
    /// <returns>The adjusted path</returns>
    private static string BuildAdjustedPath(string[] segments)
    {
        var path = '/' + string.Join('/', segments.Skip(2));

        // Special handling for messages endpoint
        if (path.EndsWith("/messages"))
        {
            path += "/";
        }

        return path;
    }

    /// <summary>
    /// Creates a new URI with the specified base, path, and query/fragment from the original.
    /// </summary>
    /// <param name="baseUri">The base URI</param>
    /// <param name="path">The path to use</param>
    /// <param name="originalUri">The original URI for query and fragment</param>
    /// <returns>The constructed URI</returns>
    private static Uri CreateUriWithPath(Uri baseUri, string path, Uri originalUri)
    {
        var uriBuilder = new UriBuilder(baseUri.Scheme, baseUri.Host, baseUri.Port)
        {
            Path = path,
            Query = originalUri.Query.TrimStart('?'),
            Fragment = originalUri.Fragment.TrimStart('#')
        };

        return uriBuilder.Uri;
    }


    /// <summary>
    /// Sends a POST request with JSON content to the specified adapter endpoint.
    /// </summary>
    /// <param name="adapter">The target adapter</param>
    /// <param name="endpoint">The endpoint path</param>
    /// <param name="body">The JSON body to send</param>
    /// <returns>A ProxyResult containing the response</returns>
    private async Task<ProxyResult> SendPostRequestAsync(McpAdapter adapter, string endpoint, JsonElement body)
    {
        try
        {
            var url = BuildAdapterEndpointUrl(adapter.Url, endpoint);

            using var client = CreateHttpClient(adapter.TimeoutSeconds);
            using var content = CreateJsonContent(body);

            using var response = await client.PostAsync(url, content);
            return await ProcessHttpResponseAsync(response, adapter.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send POST request to adapter {AdapterName} at endpoint {Endpoint}", adapter.Name, endpoint);
            return new ProxyResult
            {
                Success = false,
                StatusCode = 500,
                Error = $"Request failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sends a GET request to the specified adapter endpoint.
    /// </summary>
    /// <param name="adapter">The target adapter</param>
    /// <param name="endpoint">The endpoint path</param>
    /// <returns>A ProxyResult containing the response</returns>
    private async Task<ProxyResult> SendGetRequestAsync(McpAdapter adapter, string endpoint)
    {
        try
        {
            var url = BuildAdapterEndpointUrl(adapter.Url, endpoint);

            using var client = CreateHttpClient(adapter.TimeoutSeconds);
            using var response = await client.GetAsync(url);

            return await ProcessHttpResponseAsync(response, adapter.Name);
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

    /// <summary>
    /// Builds a complete URL for an adapter endpoint.
    /// </summary>
    /// <param name="adapterUrl">The base adapter URL</param>
    /// <param name="endpoint">The endpoint path</param>
    /// <returns>The complete URL</returns>
    private static string BuildAdapterEndpointUrl(string adapterUrl, string endpoint)
    {
        return $"{adapterUrl.TrimEnd('/')}/{endpoint}";
    }

    /// <summary>
    /// Creates JSON HTTP content from a JsonElement.
    /// </summary>
    /// <param name="body">The JSON body</param>
    /// <returns>StringContent with JSON data</returns>
    private static StringContent CreateJsonContent(JsonElement body)
    {
        var json = JsonSerializer.Serialize(body);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Processes an HTTP response and converts it to a ProxyResult.
    /// </summary>
    /// <param name="response">The HTTP response to process</param>
    /// <param name="adapterName">The name of the adapter for logging</param>
    /// <returns>A ProxyResult representing the response</returns>
    private async Task<ProxyResult> ProcessHttpResponseAsync(HttpResponseMessage response, string adapterName)
    {
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            return CreateSuccessfulResponseResult(statusCode, content);
        }

        LogUnsuccessfulResponse(adapterName, response.StatusCode, content);
        return CreateErrorResult(statusCode, content);
    }

    /// <summary>
    /// Creates a ProxyResult for successful responses, attempting to parse JSON.
    /// </summary>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="content">The response content</param>
    /// <returns>A ProxyResult with parsed content</returns>
    private static ProxyResult CreateSuccessfulResponseResult(int statusCode, string content)
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

    /// <summary>
    /// Logs unsuccessful HTTP responses.
    /// </summary>
    /// <param name="adapterName">The adapter name</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="content">The response content</param>
    private void LogUnsuccessfulResponse(string adapterName, HttpStatusCode statusCode, string content)
    {
        logger.LogWarning("MCP server {Server} returned {Status}: {Content}", adapterName, statusCode, content);
    }

    /// <summary>
    /// Executes an operation with exponential backoff retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <returns>The result of the operation</returns>
    private static async Task<ProxyResult> ExecuteWithRetryAsync(Func<Task<ProxyResult>> operation, int maxRetries = DefaultRetryAttempts)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch when (attempt < maxRetries)
            {
                await DelayBeforeRetry(attempt);
            }
        }

        return CreateServiceUnavailableResult("Service temporarily unavailable after retries");
    }

    /// <summary>
    /// Calculates and waits for the retry delay using exponential backoff.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (0-based)</param>
    private static async Task DelayBeforeRetry(int attemptNumber)
    {
        var delayMs = (int)(Math.Pow(2, attemptNumber) * BaseRetryDelayMs);
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs));
    }

    /// <summary>
    /// Handles client disconnection during SSE streaming.
    /// </summary>
    /// <param name="adapterName">The name of the adapter</param>
    /// <returns>A ProxyResult indicating successful cancellation</returns>
    private ProxyResult HandleClientDisconnection(string adapterName)
    {
        logger.LogInformation("SSE connection to {AdapterName} was cancelled by client", adapterName);
        return CreateSuccessResult(200, "SSE connection cancelled by client");
    }

    /// <summary>
    /// Handles HTTP connection errors during streaming.
    /// </summary>
    /// <param name="ex">The HTTP request exception</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="adapterName">The name of the adapter</param>
    /// <returns>A ProxyResult indicating the connection error</returns>
    private async Task<ProxyResult> HandleConnectionErrorAsync(HttpRequestException ex, HttpContext httpContext, string adapterName)
    {
        logger.LogWarning(ex, "HTTP error connecting to adapter {AdapterName}", adapterName);
        await TryWriteErrorEventAsync(httpContext, "Connection failed to upstream server");
        return CreateErrorResult(502, "Failed to connect to upstream server");
    }

    /// <summary>
    /// Handles request timeout errors during streaming.
    /// </summary>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="adapterName">The name of the adapter</param>
    /// <returns>A ProxyResult indicating the timeout error</returns>
    private async Task<ProxyResult> HandleRequestTimeoutAsync(HttpContext httpContext, string adapterName)
    {
        logger.LogWarning("SSE connection to {AdapterName} timed out", adapterName);
        await TryWriteErrorEventAsync(httpContext, "Connection timeout");
        return CreateErrorResult(504, "Connection timeout");
    }

    /// <summary>
    /// Handles unexpected errors during streaming.
    /// </summary>
    /// <param name="ex">The unexpected exception</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="adapterName">The name of the adapter</param>
    /// <returns>A ProxyResult indicating the internal error</returns>
    private async Task<ProxyResult> HandleUnexpectedErrorAsync(Exception ex, HttpContext httpContext, string adapterName)
    {
        logger.LogError(ex, "Unexpected error in SSE connection to {AdapterName}", adapterName);
        await TryWriteErrorEventAsync(httpContext, "Internal server error");
        return CreateErrorResult(500, "Internal server error");
    }

    /// <summary>
    /// Attempts to write an SSE error event to the response stream.
    /// </summary>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="errorMessage">The error message to send</param>
    private static async Task TryWriteErrorEventAsync(HttpContext httpContext, string errorMessage)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        try
        {
            var errorData = JsonSerializer.Serialize(new { error = errorMessage });
            var errorEvent = $"event: error\ndata: {errorData}\n\n";

            await httpContext.Response.WriteAsync(errorEvent, httpContext.RequestAborted);
            await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted);
        }
        catch
        {
            // Silently ignore errors when trying to write error response
            // as the connection may already be broken
        }
    }

    /// <summary>
    /// Creates a ProxyResult for successful operations.
    /// </summary>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="content">The success message</param>
    /// <returns>A successful ProxyResult</returns>
    private static ProxyResult CreateSuccessResult(int statusCode, string content)
    {
        return new ProxyResult
        {
            Success = true,
            StatusCode = statusCode,
            Content = content
        };
    }

    /// <summary>
    /// Creates a ProxyResult for error conditions.
    /// </summary>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="error">The error message</param>
    /// <returns>An error ProxyResult</returns>
    private static ProxyResult CreateErrorResult(int statusCode, string error)
    {
        return new ProxyResult
        {
            Success = false,
            StatusCode = statusCode,
            Error = error
        };
    }

    /// <summary>
    /// Creates a generic ProxyResult with specified parameters.
    /// </summary>
    /// <param name="success">Whether the operation was successful</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="content">The content or error message</param>
    /// <returns>A configured ProxyResult</returns>
    private static ProxyResult CreateProxyResult(bool success, int statusCode, string content)
    {
        return new ProxyResult
        {
            Success = success,
            StatusCode = statusCode,
            Content = success ? content : null,
            Error = success ? null : content
        };
    }

    /// <summary>
    /// Creates a ProxyResult for adapter not found scenarios.
    /// </summary>
    /// <param name="adapterName">The name of the adapter that was not found</param>
    /// <returns>A not found ProxyResult</returns>
    private static ProxyResult CreateNotFoundResult(string adapterName)
    {
        return CreateErrorResult(404, $"Adapter '{adapterName}' not found");
    }

    /// <summary>
    /// Creates a ProxyResult for disabled adapter scenarios.
    /// </summary>
    /// <param name="adapterName">The name of the disabled adapter</param>
    /// <returns>A bad request ProxyResult</returns>
    private static ProxyResult CreateDisabledAdapterResult(string adapterName)
    {
        return CreateErrorResult(400, $"Adapter '{adapterName}' is disabled");
    }

    /// <summary>
    /// Creates a ProxyResult for service unavailable scenarios.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A service unavailable ProxyResult</returns>
    private static ProxyResult CreateServiceUnavailableResult(string message)
    {
        return CreateErrorResult(503, message);
    }

    /// <summary>
    /// Legacy method for creating proxied HTTP requests. Use CreateProxiedRequest instead.
    /// </summary>
    [Obsolete("Use CreateProxiedRequest instead")]
    public static HttpRequestMessage CreateProxiedHttpRequest(HttpContext context, Func<Uri, Uri>? targetOverride = null)
    {
        return CreateProxiedRequest(context, targetOverride);
    }

    /// <summary>
    /// Legacy method for copying HTTP responses. Use CopyResponseToContextAsync instead.
    /// </summary>
    [Obsolete("Use CopyResponseToContextAsync instead")]
    public static Task CopyProxiedHttpResponseAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return CopyResponseToContextAsync(context, response, cancellationToken);
    }
}