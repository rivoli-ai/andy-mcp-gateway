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

    public static Uri BuildAdapterUri(Uri originalUri, string newAddress)
    {
        ArgumentNullException.ThrowIfNull(originalUri);
        ArgumentException.ThrowIfNullOrEmpty(newAddress);

        var pathSegments = originalUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var newBaseUri = new Uri(newAddress, UriKind.Absolute);
        var adjustedPath = BuildAdjustedPath(pathSegments);

        return CreateUriWithPath(newBaseUri, adjustedPath, originalUri);
    }

    private static string BuildAdjustedPath(string[] segments)
    {
        var path = '/' + string.Join('/', segments.Skip(2));

        if (path.EndsWith("/messages") || path.EndsWith("/message"))
        {
            path += "/";
        }

        return path;
    }

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
}
