using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using System.Linq;
using System.Net.Http;

namespace McpGateway.Application.Proxying;

/// <summary>
/// Stateless HTTP forward helpers: build outbound requests from <see cref="HttpContext"/>,
/// map gateway URLs to upstream adapter URLs, and copy responses back.
/// </summary>
public static class McpProxyHttp
{
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

    private static HttpContent? CreateRequestContent(HttpContext context)
    {
        return context.Request.ContentLength > 0 ? new StreamContent(context.Request.Body) : null;
    }

    private static void CopyRequestHeaders(HttpContext context, HttpRequestMessage requestMessage)
    {
        foreach (var header in context.Request.Headers)
        {
            if (string.Equals(header.Key, HeaderNames.Host, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private static void AddForwardingHeaders(HttpContext context, HttpRequestMessage requestMessage)
    {
        var forwardedValue = $"for={context.Connection.RemoteIpAddress};proto={context.Request.Scheme};host={context.Request.Host.Value}";
        requestMessage.Headers.TryAddWithoutValidation("Forwarded", forwardedValue);
    }

    public static HttpClient CreateStreamingHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        return new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public static HttpClient CreateHttpClient(int timeoutSeconds)
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
    }

    public static Task<HttpResponseMessage> SendWithHeadersReadAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public static async Task CopyResponseToContextAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!context.Response.HasStarted)
        {
            SetResponseStatus(context, (int)response.StatusCode);
            CopyResponseHeaders(context, response);
        }

        await CopyResponseBodyAsync(context, response, cancellationToken).ConfigureAwait(false);
    }

    private static void SetResponseStatus(HttpContext context, int statusCode)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
        }
    }

    private static void CopyResponseHeaders(HttpContext context, HttpResponseMessage response)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove(HeaderNames.TransferEncoding);
    }

    private static async Task CopyResponseBodyAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await response.Content.CopyToAsync(context.Response.Body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps the inbound gateway URL (<c>/adapters/{name}/{verb}</c>) to the upstream URL.
    /// We rebuild the path so the adapter's full path (e.g. <c>/api/v1/mcp</c>) is preserved,
    /// only swapping the very last segment with the verb the client targeted
    /// (<c>sse</c>, <c>mcp</c>, <c>messages</c>, <c>message</c>).
    /// </summary>
    public static Uri BuildAdapterUri(Uri originalUri, string newAddress)
    {
        ArgumentNullException.ThrowIfNull(originalUri);
        ArgumentException.ThrowIfNullOrEmpty(newAddress);

        var clientSegments = originalUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var clientSuffix = ExtractClientSuffix(clientSegments);

        var newBaseUri = new Uri(newAddress, UriKind.Absolute);
        var adjustedPath = MergeUpstreamPath(newBaseUri.AbsolutePath, clientSuffix);

        var uriBuilder = new UriBuilder(newBaseUri.Scheme, newBaseUri.Host, newBaseUri.Port)
        {
            Path = adjustedPath,
            Query = originalUri.Query.TrimStart('?'),
            Fragment = originalUri.Fragment.TrimStart('#')
        };
        return uriBuilder.Uri;
    }

    /// <summary>
    /// Pulls out the part of the client URL that's beyond <c>/adapters/{name}/</c>. For
    /// <c>/adapters/foo/mcp</c> that's <c>"mcp"</c>; for <c>/adapters/foo/messages</c>
    /// it's <c>"messages"</c>; for <c>/adapters/foo</c> (no verb) it's empty.
    /// </summary>
    private static string ExtractClientSuffix(string[] clientSegments)
    {
        if (clientSegments.Length <= 2) return string.Empty;
        var suffix = string.Join('/', clientSegments.Skip(2));
        return (suffix == "messages" || suffix == "message") ? suffix + "/" : suffix;
    }

    /// <summary>
    /// Combines the upstream base path with the suffix the client requested. When the
    /// upstream path already ends with the same verb (e.g. adapter URL is
    /// <c>/api/v1/mcp</c> and client hits <c>/mcp</c>), we don't duplicate it — we just
    /// keep the upstream path verbatim. Otherwise we swap the last segment of the
    /// upstream path with the client suffix so SSE adapters whose configured URL is
    /// <c>/sse</c> keep proxying <c>/messages</c> to <c>/messages</c> on the same base.
    /// </summary>
    private static string MergeUpstreamPath(string upstreamPath, string clientSuffix)
    {
        if (string.IsNullOrEmpty(clientSuffix))
            return string.IsNullOrEmpty(upstreamPath) ? "/" : upstreamPath;

        var upstreamSegments = upstreamPath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        var suffixCore = clientSuffix.TrimEnd('/');

        // Case 1 — upstream path already ends with this verb. Don't duplicate it.
        if (upstreamSegments.Count > 0 && string.Equals(upstreamSegments[^1], suffixCore, StringComparison.OrdinalIgnoreCase))
            return "/" + string.Join('/', upstreamSegments) + (clientSuffix.EndsWith('/') ? "/" : string.Empty);

        // Case 2 — upstream ends with a different known MCP verb. Swap the verb.
        if (upstreamSegments.Count > 0 && IsKnownMcpVerb(upstreamSegments[^1]))
        {
            upstreamSegments[^1] = suffixCore;
            return "/" + string.Join('/', upstreamSegments) + (clientSuffix.EndsWith('/') ? "/" : string.Empty);
        }

        // Case 3 — upstream is at the root or at an unknown path. Append the verb.
        var prefix = upstreamSegments.Count > 0 ? "/" + string.Join('/', upstreamSegments) : string.Empty;
        return prefix + "/" + clientSuffix;
    }

    private static bool IsKnownMcpVerb(string segment) =>
        string.Equals(segment, "sse", StringComparison.OrdinalIgnoreCase)
        || string.Equals(segment, "mcp", StringComparison.OrdinalIgnoreCase)
        || string.Equals(segment, "messages", StringComparison.OrdinalIgnoreCase)
        || string.Equals(segment, "message", StringComparison.OrdinalIgnoreCase);
}
