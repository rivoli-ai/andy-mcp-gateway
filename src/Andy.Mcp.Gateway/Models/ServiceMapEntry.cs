namespace Andy.Mcp.Gateway.Models;

/// <summary>
/// One entry in the gateway's service map. Each entry declares how to reach a
/// given MCP-tool-providing service: a local URL (when the service is running
/// on the same machine, e.g. via Conductor's LocalRuntime) and/or a remote URL
/// pattern (for the cloud tenant).
///
/// At least one of <see cref="LocalUrl"/> and <see cref="RemoteUrlPattern"/>
/// must be non-null. Entries with both let the router prefer-local-fallback-
/// remote (MG1). Entries with only one are valid but degenerate.
///
/// <para>
/// <see cref="RemoteUrlPattern"/> may contain a <c>{tenantSlug}</c> placeholder
/// that MG3 substitutes at request time from the bound tenant. MG1 itself
/// performs no substitution — it returns the pattern verbatim and lets
/// downstream middleware fill it in.
/// </para>
/// </summary>
public sealed record ServiceMapEntry(
    string ServiceId,
    string? LocalUrl,
    string? RemoteUrlPattern,
    bool RequiresAuth);
