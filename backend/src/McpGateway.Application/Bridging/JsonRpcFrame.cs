using System.Text.Json;

namespace McpGateway.Application.Bridging;

/// <summary>
/// Minimal projection of a JSON-RPC 2.0 frame as exchanged on the MCP wire. The bridge
/// only needs to read three things — does this frame carry an <c>id</c>, what's the
/// <c>method</c> (for diagnostics), and is it a request, a response, or a notification —
/// so we keep this as a lazy / non-allocating parser around the raw JSON string.
/// </summary>
public readonly record struct JsonRpcFrame(string Raw, JsonElement? Id, string? Method, bool HasResult, bool HasError)
{
    /// <summary>A frame is a notification when it has no <c>id</c> field at all.</summary>
    public bool IsNotification => Id is null && Method is not null;

    /// <summary>A frame is a server-bound response when it carries an <c>id</c> plus <c>result</c> or <c>error</c>.</summary>
    public bool IsResponse => Id is not null && (HasResult || HasError);

    /// <summary>A frame is a client-bound request when it has an <c>id</c> and a <c>method</c>.</summary>
    public bool IsRequest => Id is not null && Method is not null && !HasResult && !HasError;

    /// <summary>Canonical representation of the id (or null) — useful as a dictionary key.</summary>
    public string? IdKey()
    {
        if (Id is not { } el) return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => "s:" + el.GetString(),
            JsonValueKind.Number => "n:" + el.GetRawText(),
            JsonValueKind.Null => "z",
            _ => "x:" + el.GetRawText()
        };
    }

    public static JsonRpcFrame Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new JsonRpcFrame(raw ?? string.Empty, null, null, false, false);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new JsonRpcFrame(raw, null, null, false, false);

            JsonElement? id = root.TryGetProperty("id", out var idEl) ? idEl.Clone() : null;
            var method = root.TryGetProperty("method", out var mEl) && mEl.ValueKind == JsonValueKind.String
                ? mEl.GetString()
                : null;
            var hasResult = root.TryGetProperty("result", out _);
            var hasError = root.TryGetProperty("error", out _);
            return new JsonRpcFrame(raw, id, method, hasResult, hasError);
        }
        catch (JsonException)
        {
            return new JsonRpcFrame(raw, null, null, false, false);
        }
    }
}
