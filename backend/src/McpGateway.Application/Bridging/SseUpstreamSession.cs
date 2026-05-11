using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Bridging;

/// <summary>
/// <see cref="IMcpBridgeSession"/> implementation for upstream servers that speak the
/// legacy two-channel MCP SSE protocol:
/// <list type="bullet">
///   <item><c>GET /sse</c> — long-lived event stream, first event is <c>event: endpoint</c>
///   whose <c>data</c> is the URL to POST client messages to.</item>
///   <item><c>POST &lt;endpoint&gt;</c> — JSON-RPC requests / notifications go here; response is
///   typically 202 Accepted, and the actual JSON-RPC response is published on the SSE stream.</item>
/// </list>
/// The session opens the upstream stream once at startup and runs a background pump
/// that demultiplexes incoming events through a <see cref="ServerFrameHub"/>.
/// </summary>
internal sealed class SseUpstreamSession : IMcpBridgeSession, IBridgeSessionFramePusher
{
    public void PushServerInitiated(string rawFrame) => _hub.Dispatch(rawFrame);


    private readonly Uri _sseEndpoint;
    private readonly IReadOnlyDictionary<string, string>? _configuredHeaders;
    private readonly ILogger _logger;
    private readonly HttpClient _http;
    private readonly ServerFrameHub _hub = new();
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _pumpTask;
    private TaskCompletionSource<Uri>? _endpointTcs;
    private int _disposed;

    public SseUpstreamSession(Uri sseEndpoint, IReadOnlyDictionary<string, string>? configuredHeaders, ILogger logger)
    {
        _sseEndpoint = sseEndpoint;
        _configuredHeaders = configuredHeaders;
        _logger = logger;
        _http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    /// <summary>Open the upstream <c>GET /sse</c> stream and wait until the <c>endpoint</c> event arrives.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _endpointTcs = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pumpTask = Task.Run(() => PumpAsync(_lifetime.Token), CancellationToken.None);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        linked.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            await _endpointTcs.Task.WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException("Upstream SSE server did not send an 'endpoint' event within 15s.");
        }
    }

    public async Task<string> SendRequestAsync(JsonRpcFrame request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var idKey = request.IdKey()
            ?? throw new ArgumentException("Request frame must have an id.", nameof(request));

        var endpoint = await ResolveEndpointAsync(cancellationToken).ConfigureAwait(false);
        var responseTask = _hub.ExpectResponse(idKey, cancellationToken);
        await PostFrameAsync(endpoint, request.Raw, cancellationToken).ConfigureAwait(false);
        return await responseTask.ConfigureAwait(false);
    }

    public async Task SendNotificationAsync(JsonRpcFrame notification, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var endpoint = await ResolveEndpointAsync(cancellationToken).ConfigureAwait(false);
        await PostFrameAsync(endpoint, notification.Raw, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<string> SubscribeServerInitiated(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _hub.Subscribe(cancellationToken);
    }

    private async Task<Uri> ResolveEndpointAsync(CancellationToken cancellationToken)
    {
        if (_endpointTcs is null)
            throw new InvalidOperationException("Session was not started.");
        return await _endpointTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PostFrameAsync(Uri endpoint, string body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        ApplyHeaders(request);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var bodyText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Upstream rejected SSE messages POST: {(int)response.StatusCode} {response.ReasonPhrase} — {bodyText}");
        }
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _sseEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            ApplyHeaders(request);

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            await foreach (var sse in SseEventReader.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                if (string.Equals(sse.EventName, "endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    var endpointUri = BuildEndpointUri(sse.Data);
                    _endpointTcs?.TrySetResult(endpointUri);
                    continue;
                }

                if (string.IsNullOrEmpty(sse.Data))
                    continue;

                _hub.Dispatch(sse.Data);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Upstream SSE pump faulted for {Endpoint}", _sseEndpoint);
            _endpointTcs?.TrySetException(ex);
            _hub.FaultAllPending(ex);
        }
    }

    private Uri BuildEndpointUri(string raw)
    {
        var trimmed = raw.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
            return absolute;
        // Relative — resolve against the SSE endpoint authority.
        return new Uri(_sseEndpoint, trimmed);
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

    private void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(SseUpstreamSession));
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
        _lifetime.Dispose();
        _http.Dispose();
    }
}
