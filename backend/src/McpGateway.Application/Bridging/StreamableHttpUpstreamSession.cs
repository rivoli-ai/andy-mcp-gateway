using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Bridging;

/// <summary>
/// <see cref="IMcpBridgeSession"/> implementation for upstream servers that speak the
/// streamable-HTTP MCP transport.
/// <para>
/// Built to be tolerant of "older-style" servers that don't fully implement the spec:
/// <list type="bullet">
///   <item>Sends <c>Mcp-Session-Id</c> only after the upstream advertised one.</item>
///   <item>Accepts both <c>application/json</c> single response and <c>text/event-stream</c> upgrade.</item>
///   <item>Treats responses without a Content-Type as JSON.</item>
///   <item>Logs each upstream request/response at INFO so missing pieces are diagnosable.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class StreamableHttpUpstreamSession : IMcpBridgeSession, IBridgeSessionFramePusher
{
    public void PushServerInitiated(string rawFrame) => _hub.Dispatch(rawFrame);

    private const string SessionIdHeader = "Mcp-Session-Id";

    private readonly Uri _endpoint;
    private readonly IReadOnlyDictionary<string, string>? _configuredHeaders;
    private readonly ILogger _logger;
    private readonly HttpClient _http;
    private readonly ServerFrameHub _hub = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _pumpInit = new(1, 1);
    private string? _sessionId;
    private Task? _pumpTask;
    private int _disposed;

    public StreamableHttpUpstreamSession(Uri endpoint, IReadOnlyDictionary<string, string>? configuredHeaders, ILogger logger)
    {
        _endpoint = endpoint;
        _configuredHeaders = configuredHeaders;
        _logger = logger;
        _http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public async Task<string> SendRequestAsync(JsonRpcFrame request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var idKey = request.IdKey()
            ?? throw new ArgumentException("Request frame must have an id.", nameof(request));

        using var http = await PostFrameAsync(request.Raw, cancellationToken).ConfigureAwait(false);
        var contentType = http.Content.Headers.ContentType?.MediaType ?? string.Empty;

        if (contentType.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Upstream {Endpoint} responded with SSE stream — demultiplexing for id {Id}", _endpoint, idKey);
            return await ReadResponseFromSseAsync(http, idKey, cancellationToken).ConfigureAwait(false);
        }

        // Any non-SSE response is treated as a single JSON body. Older / strict-text
        // servers omit Content-Type entirely — we still read and return verbatim.
        var body = await http.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Upstream {Endpoint} POST response: status={Status} content-type='{ContentType}' bodyLen={Len}",
            _endpoint, (int)http.StatusCode, contentType, body?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException(
                $"Upstream returned an empty body on POST (status {(int)http.StatusCode}). " +
                "Older MCP servers may need a different endpoint path or HTTP verb.");
        }
        return body!;
    }

    public async Task SendNotificationAsync(JsonRpcFrame notification, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        using var http = await PostFrameAsync(notification.Raw, cancellationToken).ConfigureAwait(false);
        // Notifications usually return 202 Accepted with no body; nothing else to do.
    }

    public IAsyncEnumerable<string> SubscribeServerInitiated(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        // Kick off the GET pump the first time anybody subscribes.
        EnsurePumpAsync();
        return _hub.Subscribe(cancellationToken);
    }

    private void EnsurePumpAsync()
    {
        if (_pumpTask is not null) return;

        _pumpInit.Wait(_lifetime.Token);
        try
        {
            if (_pumpTask is null)
                _pumpTask = Task.Run(() => PumpServerStreamAsync(_lifetime.Token), CancellationToken.None);
        }
        finally
        {
            _pumpInit.Release();
        }
    }

    private async Task<HttpResponseMessage> PostFrameAsync(string body, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        ApplyHeaders(message);
        if (!string.IsNullOrEmpty(_sessionId))
            message.Headers.TryAddWithoutValidation(SessionIdHeader, _sessionId);

        _logger.LogInformation(
            "Upstream {Endpoint} POST started (bodyLen={Len}, sessionId={Session})",
            _endpoint, body?.Length ?? 0, _sessionId ?? "<none>");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstream {Endpoint} POST transport-level failure", _endpoint);
            throw new HttpRequestException(
                $"Cannot reach upstream MCP server at {_endpoint}: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            string failure;
            try { failure = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); }
            catch { failure = "<no body>"; }
            _logger.LogWarning(
                "Upstream {Endpoint} POST returned {Status}: {Body}",
                _endpoint, (int)response.StatusCode, Truncate(failure, 500));
            response.Dispose();
            throw new HttpRequestException(
                $"Upstream rejected streamable-HTTP POST: {(int)response.StatusCode} {response.ReasonPhrase} — {Truncate(failure, 200)}");
        }

        if (response.Headers.TryGetValues(SessionIdHeader, out var values))
        {
            var first = values.FirstOrDefault();
            if (!string.IsNullOrEmpty(first))
            {
                if (!string.Equals(_sessionId, first, StringComparison.Ordinal))
                    _logger.LogInformation("Upstream {Endpoint} assigned Mcp-Session-Id={Session}", _endpoint, first);
                _sessionId = first;
            }
        }

        return response;
    }

    private async Task<string> ReadResponseFromSseAsync(HttpResponseMessage response, string idKey, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await foreach (var sse in SseEventReader.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(sse.Data))
                continue;

            var parsed = JsonRpcFrame.Parse(sse.Data);
            if (parsed.IdKey() == idKey && parsed.IsResponse)
                return sse.Data;

            // Intermediate notification / unrelated frame — surface to subscribers.
            _hub.Dispatch(sse.Data);
        }

        throw new InvalidOperationException($"Upstream stream closed before producing a response for id {idKey}.");
    }

    private async Task PumpServerStreamAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            ApplyHeaders(request);
            if (!string.IsNullOrEmpty(_sessionId))
                request.Headers.TryAddWithoutValidation(SessionIdHeader, _sessionId);

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Upstream streamable-HTTP GET stream not available: {Status} {Reason}",
                    (int)response.StatusCode, response.ReasonPhrase);
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            await foreach (var sse in SseEventReader.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                if (!string.IsNullOrEmpty(sse.Data))
                    _hub.Dispatch(sse.Data);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Streamable-HTTP server-stream pump faulted for {Endpoint}", _endpoint);
            _hub.FaultAllPending(ex);
        }
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        if (_configuredHeaders is null) return;
        foreach (var (name, value) in _configuredHeaders)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "...";

    private void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(StreamableHttpUpstreamSession));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _lifetime.Cancel();
        if (_pumpTask is not null)
        {
            try { await _pumpTask.ConfigureAwait(false); }
            catch { /* swallow shutdown noise */ }
        }
        _hub.Dispose();
        _pumpInit.Dispose();
        _lifetime.Dispose();
        _http.Dispose();
    }
}
