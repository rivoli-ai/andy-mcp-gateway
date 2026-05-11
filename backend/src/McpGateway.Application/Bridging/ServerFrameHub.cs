using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace McpGateway.Application.Bridging;

/// <summary>
/// Per-session demultiplexer for server-bound frames. Used by upstream session
/// implementations to:
/// <list type="bullet">
///   <item>complete pending <c>SendRequestAsync</c> tasks when a matching response arrives,</item>
///   <item>fan out notifications and server-initiated requests to every active subscriber
///   (gateway can have several concurrent SSE/streamable observers on the same session).</item>
/// </list>
/// </summary>
internal sealed class ServerFrameHub : IDisposable
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();
    private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;

    /// <summary>Register a pending request id so a matching response can complete the task.</summary>
    public Task<string> ExpectResponse(string idKey, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[idKey] = tcs;

        var registration = cancellationToken.Register(() =>
        {
            if (_pending.TryRemove(idKey, out var slot))
                slot.TrySetCanceled(cancellationToken);
        });

        // Dispose the registration when the task completes one way or another.
        _ = tcs.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        return tcs.Task;
    }

    /// <summary>Dispatch a server-bound frame: either complete a pending request, or fan out.</summary>
    public void Dispatch(string raw)
    {
        if (_disposed != 0) return;

        var frame = JsonRpcFrame.Parse(raw);

        if (frame.IsResponse)
        {
            var key = frame.IdKey();
            if (key is not null && _pending.TryRemove(key, out var slot))
            {
                slot.TrySetResult(raw);
                return;
            }
        }

        // Notifications + unmatched responses + server-initiated requests all go to subscribers.
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(raw);
    }

    /// <summary>Subscribe to server-initiated frames. Each subscriber gets its own queue.</summary>
    public async IAsyncEnumerable<string> Subscribe([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _subscribers[id] = channel;

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
            await foreach (var frame in channel.Reader.ReadAllAsync(linked.Token).ConfigureAwait(false))
                yield return frame;
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
            channel.Writer.TryComplete();
        }
    }

    /// <summary>Fail every pending request — used during session teardown.</summary>
    public void FaultAllPending(Exception error)
    {
        foreach (var (key, slot) in _pending)
        {
            if (_pending.TryRemove(key, out var removed))
                removed.TrySetException(error);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _cts.Cancel();
        FaultAllPending(new ObjectDisposedException(nameof(ServerFrameHub)));
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryComplete();
        _subscribers.Clear();
        _cts.Dispose();
    }
}
