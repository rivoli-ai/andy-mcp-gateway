using AutoMapper;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Models;
using System.Text.Json;

namespace McpGateway.Infrastructure.Mapping;

/// <summary>
/// AutoMapper profile for mapping between domain entities and domain models.
/// Defines mapping configurations for Entity Framework entities.
/// </summary>
public class EntityMapperProfile : Profile
{
    public EntityMapperProfile()
    {
        CreateMap<McpAdapterEntity, McpAdapter>()
            .ForMember(dest => dest.Headers, opt => opt.MapFrom(src => DeserializeHeaders(src.Headers)))
            .ReverseMap()
            .ForMember(dest => dest.Headers, opt => opt.MapFrom(src => SerializeHeaders(src.Headers)));
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
