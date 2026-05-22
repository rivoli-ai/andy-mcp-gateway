using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MapsterMapper;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Mapping;
using McpGateway.Domain.Enums;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Services;

/// <summary>
/// Application service for MCP adapter management: CRUD, search, and health checks.
/// </summary>
public sealed class McpAdapterService : IMcpAdapterService
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    private readonly IMcpAdapterRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<McpAdapterService> _logger;

    public McpAdapterService(
        IMcpAdapterRepository repository,
        IMapper mapper,
        ILogger<McpAdapterService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<McpAdapterDto?> GetByIdAsync(Guid id) =>
        MapOrNull(await _repository.GetByIdAsync(id));

    public async Task<McpAdapterDto?> GetByNameAsync(string name) =>
        MapOrNull(await _repository.GetByNameAsync(name));

    public async Task<AdapterListDto> GetAllAsync() =>
        BuildList(await _repository.GetAllAsync());

    public async Task<AdapterListDto> GetEnabledAsync() =>
        BuildList(await _repository.GetEnabledAsync());

    public async Task<AdapterListDto> SearchAsync(string? name = null, bool? enabled = null) =>
        BuildList(await _repository.SearchAsync(name, enabled));

    public async Task<McpAdapterDto> CreateAsync(CreateMcpAdapterDto dto)
    {
        if (await _repository.ExistsByNameAsync(dto.Name))
            throw new InvalidOperationException($"Adapter with name '{dto.Name}' already exists");

        var adapter = _mapper.Map<McpAdapter>(dto);
        adapter.CreatedAt = DateTime.UtcNow;
        adapter.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.CreateAsync(adapter);
        _logger.LogInformation("Created MCP adapter: {Name} -> {Url}", created.Name, created.Url);

        return MapToDto(created);
    }

    public async Task<McpAdapterDto> UpdateAsync(Guid id, UpdateMcpAdapterDto dto)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Adapter with ID '{id}' not found");

        if (!string.IsNullOrEmpty(dto.Name)
            && !string.Equals(dto.Name, existing.Name, StringComparison.Ordinal)
            && await _repository.ExistsByNameAsync(dto.Name))
        {
            throw new InvalidOperationException($"Adapter with name '{dto.Name}' already exists");
        }

        McpAdapterPartialUpdate.Apply(dto, existing);
        existing.MarkAsUpdated(dto.UpdatedBy);

        var updated = await _repository.UpdateAsync(existing);
        _logger.LogInformation("Updated MCP adapter: {Name} -> {Url}", updated.Name, updated.Url);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (deleted)
            _logger.LogInformation("Deleted MCP adapter with ID: {Id}", id);
        return deleted;
    }

    public async Task<AdapterHealthDto> CheckHealthAsync(Guid id)
    {
        var adapter = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Adapter with ID '{id}' not found");

        return await ProbeAndPersistHealthAsync(adapter);
    }

    public async Task<IEnumerable<AdapterHealthDto>> CheckAllHealthAsync()
    {
        var adapters = await _repository.GetEnabledAsync();
        var results = new List<AdapterHealthDto>();

        foreach (var adapter in adapters)
        {
            try
            {
                results.Add(await ProbeAndPersistHealthAsync(adapter));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for adapter {Name}", adapter.Name);
                results.Add(new AdapterHealthDto
                {
                    Id = adapter.Id,
                    Name = adapter.Name,
                    Url = adapter.Url,
                    Status = "error",
                    LastCheck = DateTime.UtcNow,
                    LastError = ex.Message
                });
            }
        }

        return results;
    }

    private async Task<AdapterHealthDto> ProbeAndPersistHealthAsync(McpAdapter adapter)
    {
        var probe = await ProbeAsync(adapter).ConfigureAwait(false);

        await _repository.UpdateHealthStatusAsync(adapter.Id, probe.IsHealthy, probe.ResponseTimeMs, probe.Error)
            .ConfigureAwait(false);

        return new AdapterHealthDto
        {
            Id = adapter.Id,
            Name = adapter.Name,
            Url = adapter.Url,
            Status = probe.IsHealthy ? "healthy" : "unhealthy",
            LastCheck = DateTime.UtcNow,
            ResponseTimeMs = probe.ResponseTimeMs,
            LastError = probe.Error
        };
    }

    /// <summary>
    /// MCP-aware probe. For streamable HTTP we POST a JSON-RPC <c>initialize</c> and
    /// require a valid JSON-RPC response. For SSE we open the event stream and accept
    /// any first event (servers send <c>endpoint</c> as soon as the session is ready).
    /// </summary>
    private static async Task<HealthCheckResult> ProbeAsync(McpAdapter adapter)
    {
        var start = DateTime.UtcNow;
        try
        {
            using var cts = new CancellationTokenSource(HealthCheckTimeout);
            return adapter.Type switch
            {
                AdapterType.StreamableHttp => await ProbeStreamableHttpAsync(adapter, start, cts.Token).ConfigureAwait(false),
                AdapterType.Sse => await ProbeSseAsync(adapter, start, cts.Token).ConfigureAwait(false),
                _ => new HealthCheckResult(false, ElapsedMs(start), $"Unsupported adapter type '{adapter.Type}'")
            };
        }
        catch (OperationCanceledException)
        {
            return new HealthCheckResult(false, ElapsedMs(start), $"Health check timed out after {HealthCheckTimeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(false, ElapsedMs(start), ex.Message);
        }
    }

    private static async Task<HealthCheckResult> ProbeStreamableHttpAsync(McpAdapter adapter, DateTime start, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = HealthCheckTimeout };
        using var request = new HttpRequestMessage(HttpMethod.Post, adapter.Url);
        // MCP streamable-HTTP requires the client to accept BOTH application/json (single response)
        // AND text/event-stream (server may upgrade to a stream for the same response).
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        ApplyConfiguredHeaders(request, adapter);

        request.Content = new StringContent(BuildInitializePayload(), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return new HealthCheckResult(false, ElapsedMs(start), $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        // text/event-stream upgrade: read just enough to see the first frame, then bail.
        if (contentType.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            return await ReadFirstSsePayloadAsync(stream, start, ct).ConfigureAwait(false);
        }

        // Plain JSON response — parse and validate it's a JSON-RPC response (result OR error).
        using var doc = await JsonDocument.ParseAsync(stream, default, ct).ConfigureAwait(false);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return new HealthCheckResult(false, ElapsedMs(start), "Response was not a JSON object");

        if (doc.RootElement.TryGetProperty("error", out var errorEl))
        {
            var msg = errorEl.TryGetProperty("message", out var m) ? m.GetString() : "JSON-RPC error";
            return new HealthCheckResult(false, ElapsedMs(start), msg ?? "JSON-RPC error");
        }

        if (!doc.RootElement.TryGetProperty("result", out _))
            return new HealthCheckResult(false, ElapsedMs(start), "Missing 'result' field in JSON-RPC response");

        return new HealthCheckResult(true, ElapsedMs(start), null);
    }

    private static async Task<HealthCheckResult> ProbeSseAsync(McpAdapter adapter, DateTime start, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = HealthCheckTimeout };
        using var request = new HttpRequestMessage(HttpMethod.Get, adapter.Url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        ApplyConfiguredHeaders(request, adapter);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return new HealthCheckResult(false, ElapsedMs(start), $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!contentType.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase))
            return new HealthCheckResult(false, ElapsedMs(start), $"Unexpected content-type '{contentType}' — expected text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await ReadFirstSsePayloadAsync(stream, start, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the SSE stream just long enough to receive one non-empty event, then returns
    /// healthy. Any partial read counts: MCP servers typically send an <c>endpoint</c>
    /// event immediately so we don't need to decode it — its presence is enough.
    /// </summary>
    private static async Task<HealthCheckResult> ReadFirstSsePayloadAsync(Stream stream, DateTime start, CancellationToken ct)
    {
        var buffer = new byte[256];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
        return read > 0
            ? new HealthCheckResult(true, ElapsedMs(start), null)
            : new HealthCheckResult(false, ElapsedMs(start), "Stream closed before any event");
    }

    private static void ApplyConfiguredHeaders(HttpRequestMessage request, McpAdapter adapter)
    {
        if (adapter.Headers is null) return;
        foreach (var (name, value) in adapter.Headers)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            // Content-Type / Accept etc. belong on Content.Headers, but for the probe we
            // only forward custom auth-style headers (Authorization, X-MCP-Key, …).
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string BuildInitializePayload()
    {
        var initialize = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "mcp-gateway-health", version = "1.0.0" }
            }
        };
        return JsonSerializer.Serialize(initialize);
    }

    private AdapterListDto BuildList(IEnumerable<McpAdapter> adapters)
    {
        var items = adapters.Select(MapToDto).ToList();
        return new AdapterListDto
        {
            Adapters = items,
            Total = items.Count,
            Healthy = items.Count(a => a.IsHealthy && a.Enabled),
            Unhealthy = items.Count(a => !a.IsHealthy && a.Enabled),
            Disabled = items.Count(a => !a.Enabled)
        };
    }

    private McpAdapterDto MapToDto(McpAdapter adapter) => _mapper.Map<McpAdapterDto>(adapter);

    private McpAdapterDto? MapOrNull(McpAdapter? adapter) => adapter is null ? null : MapToDto(adapter);

    private static int ElapsedMs(DateTime start) => (int)(DateTime.UtcNow - start).TotalMilliseconds;

    private readonly record struct HealthCheckResult(bool IsHealthy, int? ResponseTimeMs, string? Error);
}
