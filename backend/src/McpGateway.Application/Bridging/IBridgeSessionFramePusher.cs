namespace McpGateway.Application.Bridging;

/// <summary>
/// Optional capability for upstream sessions that can accept externally-produced frames
/// and surface them on the server-initiated stream. Used by the SSE-client bridge to
/// feed responses (received synchronously over streamable HTTP) back into the long-lived
/// SSE channel that's piping events to the client.
/// </summary>
public interface IBridgeSessionFramePusher
{
    void PushServerInitiated(string rawFrame);
}
