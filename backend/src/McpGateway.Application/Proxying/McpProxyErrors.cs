using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace McpGateway.Application.Proxying;

/// <summary>
/// Helpers for writing error payloads to a proxy response that may already be partially
/// committed (SSE streams) or still buffered (regular HTTP).
/// </summary>
public static class McpProxyErrors
{
    /// <summary>
    /// Best-effort write of an SSE <c>error</c> event. No-ops if the response has already
    /// flushed headers or the connection is broken.
    /// </summary>
    public static async Task TryWriteSseErrorEventAsync(HttpContext httpContext, string errorMessage)
    {
        if (httpContext.Response.HasStarted)
            return;

        try
        {
            var data = JsonSerializer.Serialize(new { error = errorMessage });
            await httpContext.Response.WriteAsync($"event: error\ndata: {data}\n\n", httpContext.RequestAborted)
                .ConfigureAwait(false);
            await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch
        {
            // Connection may already be torn down — nothing useful we can do here.
        }
    }

    /// <summary>Writes a JSON <c>{"error": ...}</c> body with the given status code, if the response is still buffered.</summary>
    public static async Task WriteJsonErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message })).ConfigureAwait(false);
    }
}
