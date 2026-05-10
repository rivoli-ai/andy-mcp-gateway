using System.Text.Json;
using Mapster;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Models;

namespace McpGateway.Infrastructure.Mapping;

/// <summary>
/// Mapster registration for EF entities ↔ domain models (including headers JSON).
/// </summary>
public class EntityMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<McpAdapterEntity, McpAdapter>()
            .Map(dest => dest.Headers, src => DeserializeHeaders(src.Headers));

        config.NewConfig<McpAdapter, McpAdapterEntity>()
            .Map(dest => dest.Headers, src => SerializeHeaders(src.Headers));

        config.NewConfig<ApiKeyEntity, ApiKey>();
        config.NewConfig<ApiKey, ApiKeyEntity>();
    }

    private static Dictionary<string, string>? DeserializeHeaders(string? headersJson)
    {
        if (string.IsNullOrEmpty(headersJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson, (JsonSerializerOptions?)null);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeHeaders(Dictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0)
            return null;

        return JsonSerializer.Serialize(headers, (JsonSerializerOptions?)null);
    }
}
