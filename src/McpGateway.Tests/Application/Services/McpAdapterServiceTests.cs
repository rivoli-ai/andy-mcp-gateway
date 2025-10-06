using AutoMapper;
using FluentAssertions;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Services;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpGateway.Tests.Application.Services;

/// <summary>
/// Unit tests for the McpAdapterService application service.
/// </summary>
public class McpAdapterServiceTests
{
    private readonly Mock<IMcpAdapterRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<McpAdapterService>> _mockLogger;
    private readonly McpAdapterService _service;

    public McpAdapterServiceTests()
    {
        _mockRepository = new Mock<IMcpAdapterRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<McpAdapterService>>();
        _service = new McpAdapterService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdapterExists_ShouldReturnDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var adapter = new McpAdapter { Id = id, Name = "Test Adapter" };
        var dto = new McpAdapterDto { Id = id, Name = "Test Adapter" };

        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(adapter);
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(adapter)).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(dto);
        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdapterDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((McpAdapter?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenAdapterExists_ShouldReturnDto()
    {
        // Arrange
        var name = "Test Adapter";
        var adapter = new McpAdapter { Id = Guid.NewGuid(), Name = name };
        var dto = new McpAdapterDto { Id = adapter.Id, Name = name };

        _mockRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync(adapter);
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(adapter)).Returns(dto);

        // Act
        var result = await _service.GetByNameAsync(name);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(dto);
        _mockRepository.Verify(r => r.GetByNameAsync(name), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenAdapterDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var name = "Non-existent Adapter";
        _mockRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync((McpAdapter?)null);

        // Act
        var result = await _service.GetByNameAsync(name);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByNameAsync(name), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAdapterListDto()
    {
        // Arrange
        var adapters = new List<McpAdapter>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", IsHealthy = true, Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", IsHealthy = false, Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 3", IsHealthy = true, Enabled = false }
        };

        var dtos = adapters.Select(a => new McpAdapterDto
        {
            Id = a.Id,
            Name = a.Name,
            IsHealthy = a.IsHealthy,
            Enabled = a.Enabled,
            Status = a.Enabled ? (a.IsHealthy ? "healthy" : "unhealthy") : "disabled"
        }).ToList();

        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(adapters);
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(It.IsAny<McpAdapter>()))
            .Returns<McpAdapter>(a => dtos.First(d => d.Id == a.Id));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Adapters.Should().HaveCount(3);
        result.Total.Should().Be(3);
        result.Healthy.Should().Be(1);
        result.Unhealthy.Should().Be(1);
        result.Disabled.Should().Be(1);
    }

    [Fact]
    public async Task GetEnabledAsync_ShouldReturnOnlyEnabledAdapters()
    {
        // Arrange
        var adapters = new List<McpAdapter>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", IsHealthy = true, Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", IsHealthy = false, Enabled = true }
        };

        var dtos = adapters.Select(a => new McpAdapterDto
        {
            Id = a.Id,
            Name = a.Name,
            IsHealthy = a.IsHealthy,
            Enabled = a.Enabled,
            Status = a.IsHealthy ? "healthy" : "unhealthy"
        }).ToList();

        _mockRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(adapters);
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(It.IsAny<McpAdapter>()))
            .Returns<McpAdapter>(a => dtos.First(d => d.Id == a.Id));

        // Act
        var result = await _service.GetEnabledAsync();

        // Assert
        result.Should().NotBeNull();
        result.Adapters.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Healthy.Should().Be(1);
        result.Unhealthy.Should().Be(1);
        result.Disabled.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WhenNameDoesNotExist_ShouldCreateAdapter()
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

        var adapter = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Url = createDto.Url,
            Description = createDto.Description,
            TimeoutSeconds = createDto.TimeoutSeconds,
            Enabled = createDto.Enabled,
            CreatedBy = createDto.CreatedBy
        };

        var dto = new McpAdapterDto
        {
            Id = adapter.Id,
            Name = adapter.Name,
            Url = adapter.Url,
            Description = adapter.Description,
            TimeoutSeconds = adapter.TimeoutSeconds,
            Enabled = adapter.Enabled,
            CreatedBy = adapter.CreatedBy
        };

        _mockRepository.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(false);
        _mockMapper.Setup(m => m.Map<McpAdapter>(createDto)).Returns(adapter);
        _mockRepository.Setup(r => r.CreateAsync(adapter)).ReturnsAsync(adapter);
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(adapter)).Returns(dto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(dto);
        _mockRepository.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(adapter), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateMcpAdapterDto
        {
            Name = "Existing Adapter",
            Url = "http://localhost:3000"
        };

        _mockRepository.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
        _mockRepository.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<McpAdapter>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdapterExists_ShouldUpdateAdapter()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMcpAdapterDto
        {
            Name = "Updated Adapter",
            Url = "http://localhost:3001",
            UpdatedBy = "test-user"
        };

        var existingAdapter = new McpAdapter
        {
            Id = id,
            Name = "Original Adapter",
            Url = "http://localhost:3000"
        };

        var updatedAdapter = new McpAdapter
        {
            Id = id,
            Name = updateDto.Name,
            Url = updateDto.Url
        };

        var dto = new McpAdapterDto
        {
            Id = id,
            Name = updateDto.Name,
            Url = updateDto.Url
        };

        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingAdapter);
        _mockRepository.Setup(r => r.ExistsByNameAsync(updateDto.Name)).ReturnsAsync(false);
        _mockMapper.Setup(m => m.Map(updateDto, existingAdapter));
        _mockRepository.Setup(r => r.UpdateAsync(existingAdapter)).ReturnsAsync(updatedAdapter);
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(updatedAdapter)).Returns(dto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(dto);
        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingAdapter), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdapterDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMcpAdapterDto { Name = "Updated Adapter" };

        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((McpAdapter?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(id, updateDto));
        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<McpAdapter>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdapterExists_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdapterDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFilteredResults()
    {
        // Arrange
        var adapters = new List<McpAdapter>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Adapter 1", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Test Adapter 2", Enabled = false }
        };

        var dtos = adapters.Select(a => new McpAdapterDto
        {
            Id = a.Id,
            Name = a.Name,
            Enabled = a.Enabled,
            Status = a.Enabled ? "healthy" : "disabled"
        }).ToList();

        _mockRepository.Setup(r => r.SearchAsync("Test", true)).ReturnsAsync(adapters.Where(a => a.Enabled));
        _mockMapper.Setup(m => m.Map<McpAdapterDto>(It.IsAny<McpAdapter>()))
            .Returns<McpAdapter>(a => dtos.First(d => d.Id == a.Id));

        // Act
        var result = await _service.SearchAsync("Test", true);

        // Assert
        result.Should().NotBeNull();
        result.Adapters.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task ReloadMappingsAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _service.ReloadMappingsAsync();

        // Assert
        result.Should().BeTrue();
    }
}






