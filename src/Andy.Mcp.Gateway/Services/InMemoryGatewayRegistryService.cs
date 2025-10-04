using System.Collections.Concurrent;
using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// In-memory implementation of the gateway registry service
/// </summary>
public class InMemoryGatewayRegistryService : IGatewayRegistryService
{
    private readonly ConcurrentDictionary<string, McpGateway> _gateways = new();

    public Task<IEnumerable<McpGateway>> GetAllGatewaysAsync()
    {
        return Task.FromResult<IEnumerable<McpGateway>>(_gateways.Values.ToList());
    }

    public Task<McpGateway?> GetGatewayByIdAsync(string id)
    {
        _gateways.TryGetValue(id, out var gateway);
        return Task.FromResult(gateway);
    }

    public Task<IEnumerable<McpGateway>> SearchGatewaysAsync(GatewaySearchQuery query)
    {
        var results = _gateways.Values.AsEnumerable();

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLowerInvariant();
            results = results.Where(g =>
                g.Name.ToLowerInvariant().Contains(searchTerm) ||
                g.Description.ToLowerInvariant().Contains(searchTerm));
        }

        // Filter by tags
        if (query.Tags != null && query.Tags.Any())
        {
            results = results.Where(g => g.Tags.Any(t => query.Tags.Contains(t)));
        }

        // Filter by status
        if (query.Status.HasValue)
        {
            results = results.Where(g => g.Status == query.Status.Value);
        }

        // Apply pagination
        var skip = (query.Page - 1) * query.PageSize;
        results = results.Skip(skip).Take(query.PageSize);

        return Task.FromResult(results);
    }

    public Task<McpGateway> CreateGatewayAsync(CreateGatewayRequest request)
    {
        var gateway = new McpGateway
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Endpoint = request.Endpoint,
            Version = request.Version,
            Tags = request.Tags,
            Metadata = request.Metadata,
            Status = GatewayStatus.Active,
            RegisteredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _gateways.TryAdd(gateway.Id, gateway);
        return Task.FromResult(gateway);
    }

    public Task<McpGateway?> UpdateGatewayAsync(string id, UpdateGatewayRequest request)
    {
        if (!_gateways.TryGetValue(id, out var gateway))
        {
            return Task.FromResult<McpGateway?>(null);
        }

        if (request.Name != null) gateway.Name = request.Name;
        if (request.Description != null) gateway.Description = request.Description;
        if (request.Endpoint != null) gateway.Endpoint = request.Endpoint;
        if (request.Version != null) gateway.Version = request.Version;
        if (request.Tags != null) gateway.Tags = request.Tags;
        if (request.Status.HasValue) gateway.Status = request.Status.Value;
        if (request.Metadata != null) gateway.Metadata = request.Metadata;

        gateway.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<McpGateway?>(gateway);
    }

    public Task<bool> DeleteGatewayAsync(string id)
    {
        return Task.FromResult(_gateways.TryRemove(id, out _));
    }
}
