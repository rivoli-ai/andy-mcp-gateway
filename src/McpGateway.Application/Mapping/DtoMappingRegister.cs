using Mapster;
using McpGateway.Application.DTOs;
using McpGateway.Domain.Models;

namespace McpGateway.Application.Mapping;

/// <summary>
/// Mapster registration for domain models ↔ API DTOs.
/// </summary>
public class DtoMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<McpAdapter, McpAdapterDto>()
            .Map(dest => dest.Status, src => !src.Enabled ? "disabled" : src.IsHealthy ? "healthy" : "unhealthy");

        config.NewConfig<CreateMcpAdapterDto, McpAdapter>();

        // Partial update is applied imperatively — Mapster's convention merge maps null int?/bool? onto
        // non-nullable destination members as 0/false, which would break PATCH semantics.
    }

    /// <summary>
    /// Applies optional fields from an update DTO onto an existing adapter (same rules as former AutoMapper profile).
    /// </summary>
    public static void ApplyPartialUpdate(UpdateMcpAdapterDto src, McpAdapter dest)
    {
        if (!string.IsNullOrEmpty(src.Name)) dest.Name = src.Name;
        if (!string.IsNullOrEmpty(src.Url)) dest.Url = src.Url;
        if (src.Description != null) dest.Description = src.Description;
        if (src.TimeoutSeconds.HasValue) dest.TimeoutSeconds = src.TimeoutSeconds.Value;
        if (src.Enabled.HasValue) dest.Enabled = src.Enabled.Value;
        if (src.Type.HasValue) dest.Type = src.Type.Value;
        if (src.Headers != null) dest.Headers = src.Headers;
        if (!string.IsNullOrEmpty(src.UpdatedBy)) dest.UpdatedBy = src.UpdatedBy;
    }
}
