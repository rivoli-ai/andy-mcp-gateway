using McpGateway.Application.DTOs;
using McpGateway.Domain.Models;

namespace McpGateway.Application.Mapping;

/// <summary>
/// Applies an <see cref="UpdateMcpAdapterDto"/> onto a destination <see cref="McpAdapter"/>.
/// </summary>
/// <remarks>
/// PATCH semantics: a property is only copied when the source value is non-null (and, for
/// strings, non-empty). This is done imperatively rather than via Mapster's convention merge
/// because Mapster maps null <c>int?</c>/<c>bool?</c> onto non-nullable destinations as
/// <c>0</c>/<c>false</c>, which would silently overwrite existing values during a partial update.
/// </remarks>
public static class McpAdapterPartialUpdate
{
    public static void Apply(UpdateMcpAdapterDto src, McpAdapter dest)
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
