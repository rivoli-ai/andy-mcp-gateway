using AutoMapper;
using McpGateway.Application.DTOs;
using McpGateway.Domain.Models;

namespace McpGateway.Application.Mapping;

/// <summary>
/// AutoMapper profile for mapping between domain models and DTOs.
/// Defines mapping configurations for MCP adapter data transfer objects.
/// </summary>
public class DtosMapperProfile : Profile
{
    public DtosMapperProfile()
    {
        // McpAdapter mappings
        CreateMap<McpAdapter, McpAdapterDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                !src.Enabled ? "disabled" : 
                src.IsHealthy ? "healthy" : "unhealthy"));
        CreateMap<CreateMcpAdapterDto, McpAdapter>();
        CreateMap<UpdateMcpAdapterDto, McpAdapter>()
            .ForMember(dest => dest.Name, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Name)))
            .ForMember(dest => dest.Url, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Url)))
            .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
            .ForMember(dest => dest.TimeoutSeconds, opt => opt.Condition(src => src.TimeoutSeconds.HasValue))
            .ForMember(dest => dest.Enabled, opt => opt.Condition(src => src.Enabled.HasValue))
            .ForMember(dest => dest.Type, opt => opt.Condition(src => src.Type.HasValue))
            .ForMember(dest => dest.Headers, opt => opt.Condition(src => src.Headers != null))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Condition(src => !string.IsNullOrEmpty(src.UpdatedBy)))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastHealthCheck, opt => opt.Ignore())
            .ForMember(dest => dest.IsHealthy, opt => opt.Ignore())
            .ForMember(dest => dest.LastResponseTimeMs, opt => opt.Ignore())
            .ForMember(dest => dest.LastError, opt => opt.Ignore());

    }
}
