namespace McpGateway.Application.Bridging;

/// <summary>
/// Transport-agnostic view of an MCP session against an upstream server. The bridge
/// service holds one of these per gateway session and translates inbound client
/// frames into <see cref="SendRequestAsync"/> / <see cref="SendNotificationAsync"/>
/// calls, while server-initiated frames are surfaced via <see cref="SubscribeServerInitiated"/>.
/// </summary>
public interface IMcpBridgeSession : IAsyncDisposable
{
    /// <summary>
    /// Send a JSON-RPC request frame and wait for the matching response (correlated
    /// by the <c>id</c> field). Returns the response frame as a raw JSON string.
    /// Throws <see cref="OperationCanceledException"/> on timeout / cancellation.
    /// </summary>
    Task<string> SendRequestAsync(JsonRpcFrame request, CancellationToken cancellationToken);

    /// <summary>Send a JSON-RPC notification (no <c>id</c>) — fire-and-forget on the wire.</summary>
    Task SendNotificationAsync(JsonRpcFrame notification, CancellationToken cancellationToken);

    /// <summary>
    /// Stream of server-initiated frames (notifications + requests). Implementations
    /// MUST replay each frame to every active subscriber. Subscription ends when the
    /// returned async enumerable is disposed or <see cref="CancellationToken"/> trips.
    /// </summary>
    IAsyncEnumerable<string> SubscribeServerInitiated(CancellationToken cancellationToken);
}
