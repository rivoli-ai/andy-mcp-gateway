using FluentAssertions;
using Mapster;
using MapsterMapper;
using McpGateway.Application.DTOs;
using McpGateway.Application.Mapping;
using McpGateway.Domain.Models;
using Xunit;

namespace McpGateway.Tests.Application.Mapping;

/// <summary>
/// Unit tests for Mapster DTO mapping configuration (<see cref="DtoMappingRegister"/>).
/// </summary>
public class DtosMapperProfileTests
{
    private readonly IMapper _mapper;

    public DtosMapperProfileTests()
    {
        var config = new TypeAdapterConfig();
        new DtoMappingRegister().Register(config);
        config.Compile();
        _mapper = new ServiceMapper(new EmptyServiceProvider(), config);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
    [Fact]
    public void Map_FromMcpAdapterToMcpAdapterDto_ShouldMapAllProperties()
    {
        // Arrange
        var model = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Description = "Test Description",
            TimeoutSeconds = 60,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            LastHealthCheck = DateTime.UtcNow,
            IsHealthy = true,
            LastResponseTimeMs = 150,
            LastError = "No errors"
        };

        // Act
        var result = _mapper.Map<McpAdapterDto>(model);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(model.Id);
        result.Name.Should().Be(model.Name);
        result.Url.Should().Be(model.Url);
        result.Description.Should().Be(model.Description);
        result.TimeoutSeconds.Should().Be(model.TimeoutSeconds);
        result.Enabled.Should().Be(model.Enabled);
        result.CreatedAt.Should().Be(model.CreatedAt);
        result.UpdatedAt.Should().Be(model.UpdatedAt);
        result.CreatedBy.Should().Be(model.CreatedBy);
        result.UpdatedBy.Should().Be(model.UpdatedBy);
        result.LastHealthCheck.Should().Be(model.LastHealthCheck);
        result.IsHealthy.Should().Be(model.IsHealthy);
        result.LastResponseTimeMs.Should().Be(model.LastResponseTimeMs);
        result.LastError.Should().Be(model.LastError);
        result.Status.Should().Be("healthy"); // Enabled=true, IsHealthy=true
    }

    [Fact]
    public void Map_FromCreateMcpAdapterDtoToMcpAdapter_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new CreateMcpAdapterDto
        {
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Description = "Test Description",
            TimeoutSeconds = 60,
            Enabled = true,
            CreatedBy = "test-user"
        };

        // Act
        var result = _mapper.Map<McpAdapter>(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(Guid.Empty); // Default value
        result.Name.Should().Be(dto.Name);
        result.Url.Should().Be(dto.Url);
        result.Description.Should().Be(dto.Description);
        result.TimeoutSeconds.Should().Be(dto.TimeoutSeconds);
        result.Enabled.Should().Be(dto.Enabled);
        result.CreatedAt.Should().Be(default(DateTime)); // Default value
        result.UpdatedAt.Should().Be(default(DateTime)); // Default value
        result.CreatedBy.Should().Be(dto.CreatedBy);
        result.UpdatedBy.Should().BeNull(); // Not in CreateDto
        result.LastHealthCheck.Should().BeNull(); // Not in CreateDto
        result.IsHealthy.Should().BeFalse(); // Default value
        result.LastResponseTimeMs.Should().BeNull(); // Not in CreateDto
        result.LastError.Should().BeNull(); // Not in CreateDto
    }

    [Fact]
    public void Map_FromUpdateMcpAdapterDtoToMcpAdapter_ShouldMapOnlyNonNullProperties()
    {
        // Arrange
        var dto = new UpdateMcpAdapterDto
        {
            Name = "Updated Adapter",
            Url = "http://localhost:3001",
            Description = "Updated Description",
            TimeoutSeconds = 90,
            Enabled = false,
            UpdatedBy = "updated-user"
        };

        var existingModel = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "Original Adapter",
            Url = "http://localhost:3000",
            Description = "Original Description",
            TimeoutSeconds = 30,
            Enabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "original-user",
            UpdatedBy = "original-user",
            LastHealthCheck = DateTime.UtcNow.AddHours(-1),
            IsHealthy = true,
            LastResponseTimeMs = 100,
            LastError = "Original error"
        };

        // Act
        McpAdapterPartialUpdate.Apply(dto, existingModel);

        // Assert
        existingModel.Id.Should().NotBe(Guid.Empty); // Should not change
        existingModel.Name.Should().Be(dto.Name);
        existingModel.Url.Should().Be(dto.Url);
        existingModel.Description.Should().Be(dto.Description);
        existingModel.TimeoutSeconds.Should().Be(dto.TimeoutSeconds!.Value);
        existingModel.Enabled.Should().Be(dto.Enabled!.Value);
        existingModel.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(1)); // Should not change
        existingModel.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(1)); // Should not change
        existingModel.CreatedBy.Should().Be("original-user"); // Should not change
        existingModel.UpdatedBy.Should().Be(dto.UpdatedBy);
        existingModel.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow.AddHours(-1), TimeSpan.FromSeconds(1)); // Should not change
        existingModel.IsHealthy.Should().BeTrue(); // Should not change
        existingModel.LastResponseTimeMs.Should().Be(100); // Should not change
        existingModel.LastError.Should().Be("Original error"); // Should not change
    }

    [Fact]
    public void Map_FromUpdateMcpAdapterDtoToMcpAdapter_WithNullValues_ShouldNotOverwriteExistingValues()
    {
        // Arrange
        var dto = new UpdateMcpAdapterDto
        {
            Name = null,
            Url = null,
            Description = null,
            TimeoutSeconds = null,
            Enabled = null,
            UpdatedBy = null
        };

        var existingModel = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "Original Adapter",
            Url = "http://localhost:3000",
            Description = "Original Description",
            TimeoutSeconds = 30,
            Enabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "original-user",
            UpdatedBy = "original-user",
            LastHealthCheck = DateTime.UtcNow.AddHours(-1),
            IsHealthy = true,
            LastResponseTimeMs = 100,
            LastError = "Original error"
        };

        // Act
        McpAdapterPartialUpdate.Apply(dto, existingModel);

        // Assert
        existingModel.Name.Should().Be("Original Adapter"); // Should not change
        existingModel.Url.Should().Be("http://localhost:3000"); // Should not change
        existingModel.Description.Should().Be("Original Description"); // Should not change
        existingModel.TimeoutSeconds.Should().Be(30); // Should not change
        existingModel.Enabled.Should().BeTrue(); // Should not change
        existingModel.UpdatedBy.Should().Be("original-user"); // Should not change
    }

    [Fact]
    public void Map_FromCreateMcpAdapterDtoToMcpAdapter_WithNullValues_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new CreateMcpAdapterDto
        {
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Description = null,
            TimeoutSeconds = 30,
            Enabled = true,
            CreatedBy = null
        };

        // Act
        var result = _mapper.Map<McpAdapter>(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Url.Should().Be(dto.Url);
        result.Description.Should().BeNull();
        result.TimeoutSeconds.Should().Be(dto.TimeoutSeconds);
        result.Enabled.Should().Be(dto.Enabled);
        result.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void Map_FromMcpAdapterToMcpAdapterDto_WithNullValues_ShouldMapCorrectly()
    {
        // Arrange
        var model = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Description = null,
            TimeoutSeconds = 30,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = null,
            UpdatedBy = null,
            LastHealthCheck = null,
            IsHealthy = false,
            LastResponseTimeMs = null,
            LastError = null
        };

        // Act
        var result = _mapper.Map<McpAdapterDto>(model);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(model.Id);
        result.Name.Should().Be(model.Name);
        result.Url.Should().Be(model.Url);
        result.Description.Should().BeNull();
        result.TimeoutSeconds.Should().Be(model.TimeoutSeconds);
        result.Enabled.Should().Be(model.Enabled);
        result.CreatedAt.Should().Be(model.CreatedAt);
        result.UpdatedAt.Should().Be(model.UpdatedAt);
        result.CreatedBy.Should().BeNull();
        result.UpdatedBy.Should().BeNull();
        result.LastHealthCheck.Should().BeNull();
        result.IsHealthy.Should().Be(model.IsHealthy);
        result.LastResponseTimeMs.Should().BeNull();
        result.LastError.Should().BeNull();
        result.Status.Should().Be("unhealthy"); // Enabled=true, IsHealthy=false
    }

    [Theory]
    [InlineData(true, true, "healthy")]
    [InlineData(true, false, "unhealthy")]
    [InlineData(false, true, "disabled")]
    [InlineData(false, false, "disabled")]
    public void Map_FromMcpAdapterToMcpAdapterDto_ShouldSetCorrectStatus(bool enabled, bool isHealthy, string expectedStatus)
    {
        // Arrange
        var model = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Enabled = enabled,
            IsHealthy = isHealthy
        };

        // Act
        var result = _mapper.Map<McpAdapterDto>(model);

        // Assert
        result.Status.Should().Be(expectedStatus);
    }
}
