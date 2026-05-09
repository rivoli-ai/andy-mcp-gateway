using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Proxying;

/// <summary>
/// Pipes upstream SSE bytes to the client, applying <see cref="SseEndpointRewriter"/> per line.
/// </summary>
public sealed class SseProxyStream(ILogger<SseProxyStream> logger, SseEndpointRewriter rewriter)
{
    public async Task CopyToClientAsync(
        HttpResponseMessage upstreamResponse,
        HttpContext httpContext,
        string adapterName,
        CancellationToken cancellationToken)
    {
        await using var stream = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (HttpIOException ex) when (ex.Message.Contains("ResponseEnded"))
            {
                logger.LogDebug("SSE stream from {AdapterName} ended normally", adapterName);
                break;
            }

            if (line == null)
            {
                logger.LogDebug("SSE stream from {AdapterName} reached end of stream", adapterName);
                break;
            }

            var rewritten = rewriter.RewriteLine(line, adapterName);
            try
            {
                await httpContext.Response.WriteAsync($"{rewritten}\n", cancellationToken).ConfigureAwait(false);
                await httpContext.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Response has already started"))
            {
                logger.LogDebug("Client disconnected from SSE stream {AdapterName}", adapterName);
                break;
            }
        }
    }
}
