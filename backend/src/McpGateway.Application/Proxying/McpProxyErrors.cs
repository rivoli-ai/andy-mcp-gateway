using System.Text.Json;
using McpGateway.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace McpGateway.Application.Proxying;

/// <summary>
/// JSON and plain HTTP error payloads for proxy endpoints.
/// </summary>
public static class McpProxyErrors
{
    public static async Task TryWriteSseErrorEventAsync(HttpContext httpContext, string errorMessage)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        try
        {
            var errorData = JsonSerializer.Serialize(new { error = errorMessage });
            var errorEvent = $"event: error\ndata: {errorData}\n\n";

            await httpContext.Response.WriteAsync(errorEvent, httpContext.RequestAborted).ConfigureAwait(false);
            await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch
        {
            // Connection may already be broken.
        }
    }

    public static async Task WriteJsonErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var errorJson = JsonSerializer.Serialize(new { error = message });
            await context.Response.WriteAsync(errorJson).ConfigureAwait(false);
        }
    }

    public static ProxyResult NotFound(string adapterName) =>
        Error(404, $"Adapter '{adapterName}' not found");

    public static ProxyResult DisabledAdapter(string adapterName) =>
        Error(400, $"Adapter '{adapterName}' is disabled");

    public static ProxyResult ServiceUnavailable(string message) =>
        Error(503, message);

    public static ProxyResult Error(int statusCode, string error) =>
        new()
        {
            Success = false,
            StatusCode = statusCode,
            Error = error
        };
}
