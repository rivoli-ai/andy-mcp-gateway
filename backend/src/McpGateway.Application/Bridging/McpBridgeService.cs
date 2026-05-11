using System.Text;
using System.Text.Json;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Proxying;
using McpGateway.Domain.Enums;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Bridging;

/// <summary>
/// Orchestrator that maps client transport ↔ upstream transport.
/// <list type="bullet">
///   <item>When both ends speak the same transport — passthrough via <see cref="IProxyService"/>.</item>
///   <item>Otherwise spin up an <see cref="IMcpBridgeSession"/> for the upstream, register it
///   in <see cref="McpBridgeSessionStore"/>, and translate JSON-RPC frames in both directions.</item>
/// </list>
/// </summary>
public sealed class McpBridgeService : IMcpBridgeService
{
    private const string SessionIdHeader = "Mcp-Session-Id";
    private const string SseSessionQueryParam = "session";

    private readonly IMcpAdapterRepository _adapters;
    private readonly IProxyService _proxy;
    private readonly McpBridgeSessionStore _sessions;
    private readonly ILogger<McpBridgeService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public McpBridgeService(
        IMcpAdapterRepository adapters,
        IProxyService proxy,
        McpBridgeSessionStore sessions,
        ILogger<McpBridgeService> logger,
        ILoggerFactory loggerFactory)
    {
        _adapters = adapters;
        _proxy = proxy;
        _sessions = sessions;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task HandleStreamablePostAsync(string adapterName, HttpContext context, CancellationToken cancellationToken)
    {
        var adapter = await ResolveAdapterAsync(adapterName, context).ConfigureAwait(false);
        if (adapter is null) return;

        if (adapter.Type == AdapterType.StreamableHttp)
        {
            // Same transport → existing proxy path.
            await _proxy.ForwardStreamableHttpRequestAsync(adapterName, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Streamable client → SSE upstream. Bridge.
        var body = await ReadBodyAsync(context, cancellationToken).ConfigureAwait(false);
        var frame = JsonRpcFrame.Parse(body);

        var entry = await GetOrCreateSessionAsync(adapter, context.Request.Headers[SessionIdHeader].ToString(), cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (frame.IsNotification)
            {
                await entry.Session.SendNotificationAsync(frame, cancellationToken).ConfigureAwait(false);
                context.Response.Headers[SessionIdHeader] = entry.SessionId;
                context.Response.StatusCode = StatusCodes.Status202Accepted;
                return;
            }

            if (!frame.IsRequest)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest,
                    "Body must be a JSON-RPC request or notification.", cancellationToken).ConfigureAwait(false);
                return;
            }

            var responseRaw = await entry.Session.SendRequestAsync(frame, cancellationToken).ConfigureAwait(false);
            context.Response.Headers[SessionIdHeader] = entry.SessionId;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync(responseRaw, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // client gone
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bridge error: streamable-HTTP POST → SSE upstream for {AdapterName}", adapterName);
            await WriteErrorAsync(context, StatusCodes.Status502BadGateway, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task HandleStreamableGetAsync(string adapterName, HttpContext context, CancellationToken cancellationToken)
    {
        var adapter = await ResolveAdapterAsync(adapterName, context).ConfigureAwait(false);
        if (adapter is null) return;

        if (adapter.Type == AdapterType.StreamableHttp)
        {
            await _proxy.ForwardStreamableHttpRequestAsync(adapterName, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Streamable client GET → server stream. Translate to subscription on the SSE upstream session.
        var entry = await GetOrCreateSessionAsync(adapter, context.Request.Headers[SessionIdHeader].ToString(), cancellationToken)
            .ConfigureAwait(false);

        await PipeFramesAsSseAsync(entry, context, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleSseStreamAsync(string adapterName, HttpContext context, CancellationToken cancellationToken)
    {
        var adapter = await ResolveAdapterAsync(adapterName, context).ConfigureAwait(false);
        if (adapter is null) return;

        if (adapter.Type == AdapterType.Sse)
        {
            await _proxy.ForwardSseRequestAsync(adapterName, context).ConfigureAwait(false);
            return;
        }

        // SSE client → streamable HTTP upstream. Bridge.
        var entry = await GetOrCreateSessionAsync(adapter, sessionId: null, cancellationToken).ConfigureAwait(false);

        ConfigureSseResponse(context);
        // Tell the client where to POST its JSON-RPC frames (mirrors the upstream-SSE contract).
        var messagesUrl = $"/adapters/{adapterName}/messages?{SseSessionQueryParam}={entry.SessionId}";
        await WriteSseAsync(context, eventName: "endpoint", data: messagesUrl, cancellationToken).ConfigureAwait(false);

        await PipeFramesAsSseAsync(entry, context, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleSseMessagesAsync(string adapterName, HttpContext context, CancellationToken cancellationToken)
    {
        var adapter = await ResolveAdapterAsync(adapterName, context).ConfigureAwait(false);
        if (adapter is null) return;

        var sessionId = context.Request.Query[SseSessionQueryParam].ToString();
        if (string.IsNullOrEmpty(sessionId) || _sessions.Touch(sessionId) is not { } entry
            || !string.Equals(entry.AdapterName, adapter.Name, StringComparison.Ordinal))
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Unknown or missing session id.", cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var body = await ReadBodyAsync(context, cancellationToken).ConfigureAwait(false);
        var frame = JsonRpcFrame.Parse(body);

        if (frame.IsNotification)
        {
            await entry.Session.SendNotificationAsync(frame, cancellationToken).ConfigureAwait(false);
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return;
        }

        if (!frame.IsRequest)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest,
                "Body must be a JSON-RPC request or notification.", cancellationToken).ConfigureAwait(false);
            return;
        }

        // SSE-client semantics: 202 here, the actual response goes back via the open SSE
        // stream. We forward the request and fire-and-forget the response into the hub.
        context.Response.StatusCode = StatusCodes.Status202Accepted;

        _ = Task.Run(async () =>
        {
            try
            {
                var responseRaw = await entry.Session.SendRequestAsync(frame, CancellationToken.None).ConfigureAwait(false);
                if (entry.Session is IBridgeSessionFramePusher pusher)
                    pusher.PushServerInitiated(responseRaw);
                else
                    _logger.LogWarning("SSE-bridge response could not be pushed back to client (session does not support fan-out).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bridge error: SSE client → streamable upstream send for {AdapterName}", adapterName);
            }
        }, CancellationToken.None);
    }

    private async Task<McpAdapter?> ResolveAdapterAsync(string adapterName, HttpContext context)
    {
        var adapter = await _adapters.GetByNameAsync(adapterName).ConfigureAwait(false);
        if (adapter is null)
        {
            await McpProxyErrors.WriteJsonErrorAsync(context, StatusCodes.Status404NotFound,
                $"Adapter '{adapterName}' not found").ConfigureAwait(false);
            return null;
        }
        if (!adapter.Enabled)
        {
            await McpProxyErrors.WriteJsonErrorAsync(context, StatusCodes.Status400BadRequest,
                $"Adapter '{adapterName}' is disabled").ConfigureAwait(false);
            return null;
        }
        return adapter;
    }

    private async Task<BridgeSessionEntry> GetOrCreateSessionAsync(McpAdapter adapter, string? sessionId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(sessionId) && _sessions.Touch(sessionId) is { } existing
            && string.Equals(existing.AdapterName, adapter.Name, StringComparison.Ordinal))
        {
            return existing;
        }

        IMcpBridgeSession upstream;
        if (adapter.Type == AdapterType.Sse)
        {
            var sse = new SseUpstreamSession(new Uri(adapter.Url), adapter.Headers, _loggerFactory.CreateLogger<SseUpstreamSession>());
            await sse.StartAsync(cancellationToken).ConfigureAwait(false);
            upstream = sse;
        }
        else
        {
            upstream = new StreamableHttpUpstreamSession(new Uri(adapter.Url), adapter.Headers,
                _loggerFactory.CreateLogger<StreamableHttpUpstreamSession>());
        }

        var entry = new BridgeSessionEntry
        {
            SessionId = _sessions.CreateId(),
            AdapterName = adapter.Name,
            Session = upstream
        };
        _sessions.Register(entry);
        _logger.LogInformation("Opened bridge session {SessionId} for adapter {Adapter} (upstream={UpstreamType})",
            entry.SessionId, adapter.Name, adapter.Type);
        return entry;
    }

    private static async Task PipeFramesAsSseAsync(BridgeSessionEntry entry, HttpContext context, CancellationToken cancellationToken)
    {
        ConfigureSseResponse(context);

        try
        {
            await foreach (var frame in entry.Session.SubscribeServerInitiated(cancellationToken).ConfigureAwait(false))
            {
                if (context.Response.HasStarted && context.RequestAborted.IsCancellationRequested)
                    break;
                await WriteSseAsync(context, eventName: "message", data: frame, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // client closed connection
        }
    }

    private static async Task<string> ReadBodyAsync(HttpContext context, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ConfigureSseResponse(HttpContext context)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";
    }

    private static async Task WriteSseAsync(HttpContext context, string eventName, string data, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(eventName) && eventName != "message")
            sb.Append("event: ").Append(eventName).Append('\n');
        foreach (var line in data.Split('\n'))
            sb.Append("data: ").Append(line).Append('\n');
        sb.Append('\n');
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await context.Response.Body.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string message, CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
    }
}

