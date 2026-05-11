using System.Runtime.CompilerServices;

namespace McpGateway.Application.Bridging;

/// <summary>One parsed Server-Sent Event from an upstream stream.</summary>
public readonly record struct SseEvent(string EventName, string Data);

/// <summary>
/// Streaming SSE parser. Reads <c>text/event-stream</c> framing from a <see cref="StreamReader"/>
/// and yields one <see cref="SseEvent"/> per "event" — i.e. the run of <c>data:</c> lines
/// terminated by a blank line. Comments (<c>:</c>-prefixed) and unknown fields are ignored,
/// which mirrors the WHATWG spec closely enough for MCP.
/// </summary>
public static class SseEventReader
{
    public static async IAsyncEnumerable<SseEvent> ReadAsync(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var eventName = "message";
        var dataBuffer = new System.Text.StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (line is null)
                yield break; // upstream closed

            if (line.Length == 0)
            {
                if (dataBuffer.Length > 0)
                {
                    yield return new SseEvent(eventName, dataBuffer.ToString());
                    dataBuffer.Clear();
                }
                eventName = "message";
                continue;
            }

            if (line[0] == ':')
                continue; // comment / keepalive

            var colon = line.IndexOf(':');
            string field;
            string value;
            if (colon < 0)
            {
                field = line;
                value = string.Empty;
            }
            else
            {
                field = line[..colon];
                value = colon + 1 < line.Length && line[colon + 1] == ' '
                    ? line[(colon + 2)..]
                    : line[(colon + 1)..];
            }

            switch (field)
            {
                case "event":
                    eventName = value;
                    break;
                case "data":
                    if (dataBuffer.Length > 0)
                        dataBuffer.Append('\n');
                    dataBuffer.Append(value);
                    break;
                // "id" and "retry" — not needed by the bridge today
            }
        }
    }
}
