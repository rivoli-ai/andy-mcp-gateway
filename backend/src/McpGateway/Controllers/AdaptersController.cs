using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

/// <summary>
/// REST API for managing MCP adapters: CRUD, health checks, search, and Excel import/export.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AdaptersController : ControllerBase
{
    private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IMcpAdapterService _adapters;
    private readonly ExcelService _excel;
    private readonly ILogger<AdaptersController> _logger;

    public AdaptersController(
        IMcpAdapterService adapters,
        ExcelService excel,
        ILogger<AdaptersController> logger)
    {
        _adapters = adapters;
        _excel = excel;
        _logger = logger;
    }

    /// <summary>List all adapters with summary counts.</summary>
    [HttpGet]
    public async Task<ActionResult<AdapterListDto>> GetAll() =>
        await Execute(() => _adapters.GetAllAsync(), "Error retrieving all adapters");

    /// <summary>List enabled adapters only.</summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<AdapterListDto>> GetEnabled() =>
        await Execute(() => _adapters.GetEnabledAsync(), "Error retrieving enabled adapters");

    /// <summary>Look up a single adapter by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<McpAdapterDto>> GetById(Guid id)
    {
        try
        {
            var adapter = await _adapters.GetByIdAsync(id);
            return adapter is null
                ? NotFound(new { error = $"Adapter with ID '{id}' not found" })
                : Ok(adapter);
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error retrieving adapter {Id}", id);
        }
    }

    /// <summary>Look up a single adapter by name.</summary>
    [HttpGet("name/{name}")]
    public async Task<ActionResult<McpAdapterDto>> GetByName(string name)
    {
        try
        {
            var adapter = await _adapters.GetByNameAsync(name);
            return adapter is null
                ? NotFound(new { error = $"Adapter with name '{name}' not found" })
                : Ok(adapter);
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error retrieving adapter {Name}", name);
        }
    }

    /// <summary>Create a new adapter and run an initial health check.</summary>
    [HttpPost]
    public async Task<ActionResult<McpAdapterDto>> Create([FromBody] CreateMcpAdapterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var adapter = await _adapters.CreateAsync(dto);
            await TryProbeHealthAsync(adapter.Id, adapter.Name, "newly created");
            return CreatedAtAction(nameof(GetById), new { id = adapter.Id }, adapter);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error creating adapter");
        }
    }

    /// <summary>Update an existing adapter and re-run its health check.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<McpAdapterDto>> Update(Guid id, [FromBody] UpdateMcpAdapterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var adapter = await _adapters.UpdateAsync(id, dto);
            await TryProbeHealthAsync(id, adapter.Name, "updated");
            return Ok(adapter);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error updating adapter {Id}", id);
        }
    }

    /// <summary>Delete an adapter.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            return await _adapters.DeleteAsync(id)
                ? NoContent()
                : NotFound(new { error = $"Adapter with ID '{id}' not found" });
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error deleting adapter {Id}", id);
        }
    }

    /// <summary>Probe the health of a single adapter and persist the result.</summary>
    [HttpGet("{id:guid}/health")]
    public async Task<ActionResult<AdapterHealthDto>> CheckHealth(Guid id)
    {
        try
        {
            return Ok(await _adapters.CheckHealthAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error checking health for adapter {Id}", id);
        }
    }

    /// <summary>Probe the health of every enabled adapter and persist the results.</summary>
    [HttpPost("health-check")]
    public async Task<ActionResult<IEnumerable<AdapterHealthDto>>> CheckAllHealth() =>
        await Execute(() => _adapters.CheckAllHealthAsync(), "Error checking health for all adapters");

    /// <summary>Search adapters by name fragment and/or enabled flag.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<AdapterListDto>> Search([FromQuery] string? name = null, [FromQuery] bool? enabled = null) =>
        await Execute(() => _adapters.SearchAsync(name, enabled), "Error searching adapters");

    /// <summary>Stream all adapters as an Excel workbook.</summary>
    [HttpGet("export")]
    public async Task<ActionResult> Export()
    {
        try
        {
            var list = await _adapters.GetAllAsync();
            if (list.Adapters is null || !list.Adapters.Any())
                return BadRequest(new { error = "No adapters to export" });

            var bytes = _excel.ExportAdaptersToExcel(list.Adapters);
            var fileName = $"MCP_Adapters_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, ExcelMimeType, fileName);
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error exporting adapters to Excel");
        }
    }

    /// <summary>Bulk-create adapters from an uploaded Excel workbook.</summary>
    [HttpPost("import")]
    public async Task<ActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be an Excel (.xlsx) file" });

        try
        {
            await using var stream = file.OpenReadStream();
            var (adapters, errors) = _excel.ImportAdaptersFromExcel(stream);

            if (errors.Count > 0 && adapters.Count == 0)
                return BadRequest(new { error = "Import failed", errors });

            var failed = new List<object>();
            var successCount = 0;

            foreach (var dto in adapters)
            {
                try
                {
                    await _adapters.CreateAsync(dto);
                    successCount++;
                }
                catch (InvalidOperationException ex)
                {
                    failed.Add(new { name = dto.Name, error = ex.Message });
                }
                catch (Exception ex)
                {
                    failed.Add(new { name = dto.Name, error = "Failed to create adapter" });
                    _logger.LogError(ex, "Error creating adapter {Name} during import", dto.Name);
                }
            }

            return Ok(new
            {
                message = $"Import completed. {successCount} adapters imported successfully.",
                successCount,
                failedCount = failed.Count,
                validationErrors = errors,
                failedAdapters = failed
            });
        }
        catch (Exception ex)
        {
            return InternalError(ex, "Error importing adapters from Excel");
        }
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action, string errorContext)
    {
        try
        {
            return Ok(await action());
        }
        catch (Exception ex)
        {
            return InternalError(ex, errorContext);
        }
    }

    private ObjectResult InternalError(Exception ex, string messageTemplate, params object?[] args)
    {
        _logger.LogError(ex, messageTemplate, args);
        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
    }

    private async Task TryProbeHealthAsync(Guid adapterId, string adapterName, string adjective)
    {
        try
        {
            var health = await _adapters.CheckHealthAsync(adapterId);
            _logger.LogInformation("Health check completed for {Adjective} adapter {Name}: {Status}",
                adjective, adapterName, health.Status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for {Adjective} adapter {Name}", adjective, adapterName);
        }
    }
}
