using FluentAssertions;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Services;
using McpGateway.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpGateway.Tests.Controllers;

/// <summary>
/// Unit tests for the AdaptersController API controller.
/// </summary>
public class AdaptersControllerTests
{
    private readonly Mock<IMcpAdapterService> _mockService;
    private readonly Mock<ILogger<AdaptersController>> _mockLogger;
    private readonly ExcelService _excelService;
    private readonly AdaptersController _controller;

    public AdaptersControllerTests()
    {
        _mockService = new Mock<IMcpAdapterService>();
        _mockLogger = new Mock<ILogger<AdaptersController>>();
        _excelService = new ExcelService();
        _controller = new AdaptersController(_mockService.Object, _excelService, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllAdapters_WhenSuccessful_ShouldReturnOkWithAdapterList()
    {
        // Arrange
        var adapterList = new AdapterListDto
        {
            Adapters = new List<McpAdapterDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Adapter 1", Url = "http://localhost:3001" },
                new() { Id = Guid.NewGuid(), Name = "Adapter 2", Url = "http://localhost:3002" }
            },
            Total = 2,
            Healthy = 1,
            Unhealthy = 1,
            Disabled = 0
        };

        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(adapterList);

        // Act
        var result = await _controller.GetAllAdapters();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(adapterList);
        _mockService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAdapters_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllAsync()).ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetAllAdapters();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEnabledAdapters_WhenSuccessful_ShouldReturnOkWithAdapterList()
    {
        // Arrange
        var adapterList = new AdapterListDto
        {
            Adapters = new List<McpAdapterDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Enabled Adapter", Url = "http://localhost:3001", Enabled = true }
            },
            Total = 1,
            Healthy = 1,
            Unhealthy = 0,
            Disabled = 0
        };

        _mockService.Setup(s => s.GetEnabledAsync()).ReturnsAsync(adapterList);

        // Act
        var result = await _controller.GetEnabledAdapters();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(adapterList);
        _mockService.Verify(s => s.GetEnabledAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAdapterById_WhenAdapterExists_ShouldReturnOkWithAdapter()
    {
        // Arrange
        var id = Guid.NewGuid();
        var adapter = new McpAdapterDto
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000"
        };

        _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(adapter);

        // Act
        var result = await _controller.GetAdapterById(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(adapter);
        _mockService.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetAdapterById_WhenAdapterDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((McpAdapterDto?)null);

        // Act
        var result = await _controller.GetAdapterById(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);
        _mockService.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetAdapterByName_WhenAdapterExists_ShouldReturnOkWithAdapter()
    {
        // Arrange
        var name = "Test Adapter";
        var adapter = new McpAdapterDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = "http://localhost:3000"
        };

        _mockService.Setup(s => s.GetByNameAsync(name)).ReturnsAsync(adapter);

        // Act
        var result = await _controller.GetAdapterByName(name);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(adapter);
        _mockService.Verify(s => s.GetByNameAsync(name), Times.Once);
    }

    [Fact]
    public async Task GetAdapterByName_WhenAdapterDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var name = "Non-existent Adapter";
        _mockService.Setup(s => s.GetByNameAsync(name)).ReturnsAsync((McpAdapterDto?)null);

        // Act
        var result = await _controller.GetAdapterByName(name);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);
        _mockService.Verify(s => s.GetByNameAsync(name), Times.Once);
    }

    [Fact]
    public async Task CreateAdapter_WhenValidDto_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateMcpAdapterDto
        {
            Name = "New Adapter",
            Url = "http://localhost:3000",
            Description = "Test Description",
            TimeoutSeconds = 60,
            Enabled = true,
            CreatedBy = "test-user"
        };

        var createdAdapter = new McpAdapterDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Url = createDto.Url,
            Description = createDto.Description,
            TimeoutSeconds = createDto.TimeoutSeconds,
            Enabled = createDto.Enabled,
            CreatedBy = createDto.CreatedBy
        };

        _mockService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdAdapter);

        // Act
        var result = await _controller.CreateAdapter(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtResult = result.Result as CreatedAtActionResult;
        createdAtResult!.Value.Should().Be(createdAdapter);
        createdAtResult.ActionName.Should().Be(nameof(_controller.GetAdapterById));
        createdAtResult.RouteValues!["id"].Should().Be(createdAdapter.Id);
        _mockService.Verify(s => s.CreateAsync(createDto), Times.Once);
    }

    [Fact]
    public async Task CreateAdapter_WhenInvalidDto_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateMcpAdapterDto();
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateAdapter(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        _mockService.Verify(s => s.CreateAsync(It.IsAny<CreateMcpAdapterDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateAdapter_WhenServiceThrowsInvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateMcpAdapterDto
        {
            Name = "Existing Adapter",
            Url = "http://localhost:3000"
        };

        _mockService.Setup(s => s.CreateAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Adapter with name 'Existing Adapter' already exists"));

        // Act
        var result = await _controller.CreateAdapter(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        _mockService.Verify(s => s.CreateAsync(createDto), Times.Once);
    }

    [Fact]
    public async Task UpdateAdapter_WhenValidDto_ShouldReturnOkWithUpdatedAdapter()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMcpAdapterDto
        {
            Name = "Updated Adapter",
            Url = "http://localhost:3001",
            Description = "Updated Description",
            TimeoutSeconds = 90,
            Enabled = false,
            UpdatedBy = "updated-user"
        };

        var updatedAdapter = new McpAdapterDto
        {
            Id = id,
            Name = updateDto.Name,
            Url = updateDto.Url,
            Description = updateDto.Description,
            TimeoutSeconds = updateDto.TimeoutSeconds!.Value,
            Enabled = updateDto.Enabled!.Value,
            UpdatedBy = updateDto.UpdatedBy
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto)).ReturnsAsync(updatedAdapter);

        // Act
        var result = await _controller.UpdateAdapter(id, updateDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(updatedAdapter);
        _mockService.Verify(s => s.UpdateAsync(id, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateAdapter_WhenInvalidDto_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMcpAdapterDto();
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.UpdateAdapter(id, updateDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        _mockService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateMcpAdapterDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAdapter_WhenAdapterDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMcpAdapterDto { Name = "Updated Adapter" };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto))
            .ThrowsAsync(new KeyNotFoundException($"Adapter with ID '{id}' not found"));

        // Act
        var result = await _controller.UpdateAdapter(id, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);
        _mockService.Verify(s => s.UpdateAsync(id, updateDto), Times.Once);
    }

    [Fact]
    public async Task DeleteAdapter_WhenAdapterExists_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteAdapter(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteAdapter_WhenAdapterDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteAdapter(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);
        _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task CheckAdapterHealth_WhenAdapterExists_ShouldReturnOkWithHealthDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var healthDto = new AdapterHealthDto
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Status = "healthy",
            LastCheck = DateTime.UtcNow,
            ResponseTimeMs = 150,
            LastError = null
        };

        _mockService.Setup(s => s.CheckHealthAsync(id)).ReturnsAsync(healthDto);

        // Act
        var result = await _controller.CheckAdapterHealth(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(healthDto);
        _mockService.Verify(s => s.CheckHealthAsync(id), Times.Once);
    }

    [Fact]
    public async Task CheckAdapterHealth_WhenAdapterDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.CheckHealthAsync(id))
            .ThrowsAsync(new KeyNotFoundException($"Adapter with ID '{id}' not found"));

        // Act
        var result = await _controller.CheckAdapterHealth(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);
        _mockService.Verify(s => s.CheckHealthAsync(id), Times.Once);
    }

    [Fact]
    public async Task CheckAllAdaptersHealth_WhenSuccessful_ShouldReturnOkWithHealthDtos()
    {
        // Arrange
        var healthDtos = new List<AdapterHealthDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", Status = "healthy" },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", Status = "unhealthy" }
        };

        _mockService.Setup(s => s.CheckAllHealthAsync()).ReturnsAsync(healthDtos);

        // Act
        var result = await _controller.CheckAllAdaptersHealth();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(healthDtos);
        _mockService.Verify(s => s.CheckAllHealthAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchAdapters_WhenSuccessful_ShouldReturnOkWithAdapterList()
    {
        // Arrange
        var adapterList = new AdapterListDto
        {
            Adapters = new List<McpAdapterDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Test Adapter", Url = "http://localhost:3000" }
            },
            Total = 1,
            Healthy = 1,
            Unhealthy = 0,
            Disabled = 0
        };

        _mockService.Setup(s => s.SearchAsync("Test", true)).ReturnsAsync(adapterList);

        // Act
        var result = await _controller.SearchAdapters("Test", true);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(adapterList);
        _mockService.Verify(s => s.SearchAsync("Test", true), Times.Once);
    }

    [Fact]
    public async Task ReloadMappings_WhenSuccessful_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        _mockService.Setup(s => s.ReloadMappingsAsync()).ReturnsAsync(true);

        // Act
        var result = await _controller.ReloadMappings();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);
        _mockService.Verify(s => s.ReloadMappingsAsync(), Times.Once);
    }
}
