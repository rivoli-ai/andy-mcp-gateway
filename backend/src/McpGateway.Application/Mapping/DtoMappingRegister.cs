using Mapster;
using McpGateway.Application.DTOs;
using McpGateway.Domain.Models;

namespace McpGateway.Application.Mapping;

/// <summary>
/// Mapster registration for domain-model ↔ API-DTO mappings.
/// </summary>
public class DtoMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<McpAdapter, McpAdapterDto>()
            .Map(dest => dest.Status, src =>
                !src.Enabled ? "disabled" :
                src.IsHealthy ? "healthy" :
                "unhealthy");

        config.NewConfig<CreateMcpAdapterDto, McpAdapter>();

        // UpdateMcpAdapterDto → McpAdapter is applied imperatively in McpAdapterPartialUpdate.

        // API keys never expose their plaintext or ciphertext — the DTO mirrors only safe fields.
        config.NewConfig<ApiKey, ApiKeyDto>()
            .Map(dest => dest.IsActive, src => src.RevokedAt == null);
    }
}
