using AutoMapper;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Models;

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
            .ReverseMap();
    }
}
