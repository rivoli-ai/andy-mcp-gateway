using Microsoft.AspNetCore.Mvc;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace McpGateway.Controllers;

/// <summary>
/// API Controller for managing MCP adapters.
/// Provides REST endpoints for CRUD operations, health checking, and adapter management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AdaptersController : ControllerBase
{
    private readonly IMcpAdapterService _adapterService;
    private readonly ExcelService _excelService;
    private readonly ILogger<AdaptersController> _logger;

    public AdaptersController(
        IMcpAdapterService adapterService, 
        ExcelService excelService,
        ILogger<AdaptersController> logger)
    {
        _adapterService = adapterService;
        _excelService = excelService;
        _logger = logger;
    }

    /// <summary>
    /// Get all MCP adapters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AdapterListDto>> GetAllAdapters()
    {
        try
        {
            var result = await _adapterService.GetAllAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all adapters");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get enabled MCP adapters only
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<AdapterListDto>> GetEnabledAdapters()
    {
        try
        {
            var result = await _adapterService.GetEnabledAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enabled adapters");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get adapter by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<McpAdapterDto>> GetAdapterById(Guid id)
    {
        try
        {
            var adapter = await _adapterService.GetByIdAsync(id);
            if (adapter == null)
            {
                return NotFound(new { error = $"Adapter with ID '{id}' not found" });
            }
            return Ok(adapter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving adapter {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get adapter by name
    /// </summary>
    [HttpGet("name/{name}")]
    public async Task<ActionResult<McpAdapterDto>> GetAdapterByName(string name)
    {
        try
        {
            var adapter = await _adapterService.GetByNameAsync(name);
            if (adapter == null)
            {
                return NotFound(new { error = $"Adapter with name '{name}' not found" });
            }
            return Ok(adapter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving adapter {Name}", name);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new MCP adapter
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<McpAdapterDto>> CreateAdapter([FromBody] CreateMcpAdapterDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Health check is performed by the service before saving
            var adapter = await _adapterService.CreateAsync(dto);
            
            // Perform health check after creating the adapter
            try
            {
                var healthCheck = await _adapterService.CheckHealthAsync(adapter.Id);
                _logger.LogInformation("Health check completed for newly created adapter {Name}: {Status}", 
                    adapter.Name, healthCheck.Status);
            }
            catch (Exception healthEx)
            {
                _logger.LogWarning(healthEx, "Health check failed for newly created adapter {Name}", adapter.Name);
                // Don't fail the creation if health check fails
            }
            
            return CreatedAtAction(nameof(GetAdapterById), new { id = adapter.Id }, adapter);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating adapter");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing MCP adapter
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<McpAdapterDto>> UpdateAdapter(Guid id, [FromBody] UpdateMcpAdapterDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adapter = await _adapterService.UpdateAsync(id, dto);
            
            // Perform health check after updating the adapter
            try
            {
                var healthCheck = await _adapterService.CheckHealthAsync(id);
                _logger.LogInformation("Health check completed for updated adapter {Name}: {Status}", 
                    adapter.Name, healthCheck.Status);
            }
            catch (Exception healthEx)
            {
                _logger.LogWarning(healthEx, "Health check failed for updated adapter {Name}", adapter.Name);
                // Don't fail the update if health check fails
            }
            
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
            _logger.LogError(ex, "Error updating adapter {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete an MCP adapter
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAdapter(Guid id)
    {
        try
        {
            var result = await _adapterService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { error = $"Adapter with ID '{id}' not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting adapter {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check health status of a specific adapter
    /// </summary>
    [HttpGet("{id:guid}/health")]
    public async Task<ActionResult<AdapterHealthDto>> CheckAdapterHealth(Guid id)
    {
        try
        {
            var health = await _adapterService.CheckHealthAsync(id);
            return Ok(health);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for adapter {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check health status of all adapters
    /// </summary>
    [HttpPost("health-check")]
    public async Task<ActionResult<IEnumerable<AdapterHealthDto>>> CheckAllAdaptersHealth()
    {
        try
        {
            var healthChecks = await _adapterService.CheckAllHealthAsync();
            return Ok(healthChecks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for all adapters");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Search adapters by name and/or enabled status
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<AdapterListDto>> SearchAdapters([FromQuery] string? name = null, [FromQuery] bool? enabled = null)
    {
        try
        {
            var result = await _adapterService.SearchAsync(name, enabled);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching adapters");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reload adapter mappings
    /// </summary>
    [HttpPost("reload")]
    public async Task<ActionResult> ReloadMappings()
    {
        try
        {
            var result = await _adapterService.ReloadMappingsAsync();
            return Ok(new { message = "Mappings reloaded successfully", success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading mappings");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Export all adapters to Excel file
    /// </summary>
    [HttpGet("export")]
    public async Task<ActionResult> ExportAdaptersToExcel()
    {
        try
        {
            var adapters = await _adapterService.GetAllAsync();
            
            if (adapters.Adapters == null || !adapters.Adapters.Any())
            {
                return BadRequest(new { error = "No adapters to export" });
            }

            var excelData = _excelService.ExportAdaptersToExcel(adapters.Adapters);
            
            var fileName = $"MCP_Adapters_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            
            return File(excelData, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting adapters to Excel");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Import adapters from Excel file
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult> ImportAdaptersFromExcel(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be an Excel (.xlsx) file" });
            }

            using var stream = file.OpenReadStream();
            var (adapters, errors) = _excelService.ImportAdaptersFromExcel(stream);

            if (errors.Any() && !adapters.Any())
            {
                return BadRequest(new { error = "Import failed", errors });
            }

            var successCount = 0;
            var failedAdapters = new List<object>();

            foreach (var adapter in adapters)
            {
                try
                {
                    await _adapterService.CreateAsync(adapter);
                    successCount++;
                }
                catch (InvalidOperationException ex)
                {
                    failedAdapters.Add(new { name = adapter.Name, error = ex.Message });
                }
                catch (Exception ex)
                {
                    failedAdapters.Add(new { name = adapter.Name, error = "Failed to create adapter" });
                    _logger.LogError(ex, "Error creating adapter {Name} during import", adapter.Name);
                }
            }

            return Ok(new
            {
                message = $"Import completed. {successCount} adapters imported successfully.",
                successCount,
                failedCount = failedAdapters.Count,
                validationErrors = errors,
                failedAdapters
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing adapters from Excel");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
