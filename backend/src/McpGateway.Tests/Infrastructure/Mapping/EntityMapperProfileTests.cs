using FluentAssertions;
using Mapster;
using MapsterMapper;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Models;
using McpGateway.Infrastructure.Mapping;
using Xunit;

namespace McpGateway.Tests.Infrastructure.Mapping;

/// <summary>
/// Unit tests for Mapster entity mapping configuration (<see cref="EntityMappingRegister"/>).
/// </summary>
public class EntityMapperProfileTests
{
    private readonly IMapper _mapper;

    public EntityMapperProfileTests()
    {
        var config = new TypeAdapterConfig();
        new EntityMappingRegister().Register(config);
        config.Compile();
        _mapper = new ServiceMapper(new EmptyServiceProvider(), config);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public void Configuration_ShouldCompile()
    {
        var config = new TypeAdapterConfig();
        new EntityMappingRegister().Register(config);
        config.Compile();
    }

    [Fact]
    public void Map_FromMcpAdapterEntityToMcpAdapter_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new McpAdapterEntity
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
        var result = _mapper.Map<McpAdapter>(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
        result.Url.Should().Be(entity.Url);
        result.Description.Should().Be(entity.Description);
        result.TimeoutSeconds.Should().Be(entity.TimeoutSeconds);
        result.Enabled.Should().Be(entity.Enabled);
        result.CreatedAt.Should().Be(entity.CreatedAt);
        result.UpdatedAt.Should().Be(entity.UpdatedAt);
        result.CreatedBy.Should().Be(entity.CreatedBy);
        result.UpdatedBy.Should().Be(entity.UpdatedBy);
        result.LastHealthCheck.Should().Be(entity.LastHealthCheck);
        result.IsHealthy.Should().Be(entity.IsHealthy);
        result.LastResponseTimeMs.Should().Be(entity.LastResponseTimeMs);
        result.LastError.Should().Be(entity.LastError);
    }

    [Fact]
    public void Map_FromMcpAdapterToMcpAdapterEntity_ShouldMapAllProperties()
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
        var result = _mapper.Map<McpAdapterEntity>(model);

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
    }

    [Fact]
    public void Map_FromMcpAdapterEntityToMcpAdapter_WithNullValues_ShouldMapCorrectly()
    {
        // Arrange
        var entity = new McpAdapterEntity
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
        var result = _mapper.Map<McpAdapter>(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
        result.Url.Should().Be(entity.Url);
        result.Description.Should().BeNull();
        result.TimeoutSeconds.Should().Be(entity.TimeoutSeconds);
        result.Enabled.Should().Be(entity.Enabled);
        result.CreatedAt.Should().Be(entity.CreatedAt);
        result.UpdatedAt.Should().Be(entity.UpdatedAt);
        result.CreatedBy.Should().BeNull();
        result.UpdatedBy.Should().BeNull();
        result.LastHealthCheck.Should().BeNull();
        result.IsHealthy.Should().Be(entity.IsHealthy);
        result.LastResponseTimeMs.Should().BeNull();
        result.LastError.Should().BeNull();
    }

    [Fact]
    public void Map_FromMcpAdapterToMcpAdapterEntity_WithNullValues_ShouldMapCorrectly()
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
        var result = _mapper.Map<McpAdapterEntity>(model);

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
    }

    [Fact]
    public void Map_ShouldSupportReverseMapping()
    {
        // Arrange
        var originalEntity = new McpAdapterEntity
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
        var model = _mapper.Map<McpAdapter>(originalEntity);
        var mappedBackEntity = _mapper.Map<McpAdapterEntity>(model);

        // Assert
        mappedBackEntity.Should().NotBeNull();
        mappedBackEntity.Id.Should().Be(originalEntity.Id);
        mappedBackEntity.Name.Should().Be(originalEntity.Name);
        mappedBackEntity.Url.Should().Be(originalEntity.Url);
        mappedBackEntity.Description.Should().Be(originalEntity.Description);
        mappedBackEntity.TimeoutSeconds.Should().Be(originalEntity.TimeoutSeconds);
        mappedBackEntity.Enabled.Should().Be(originalEntity.Enabled);
        mappedBackEntity.CreatedAt.Should().Be(originalEntity.CreatedAt);
        mappedBackEntity.UpdatedAt.Should().Be(originalEntity.UpdatedAt);
        mappedBackEntity.CreatedBy.Should().Be(originalEntity.CreatedBy);
        mappedBackEntity.UpdatedBy.Should().Be(originalEntity.UpdatedBy);
        mappedBackEntity.LastHealthCheck.Should().Be(originalEntity.LastHealthCheck);
        mappedBackEntity.IsHealthy.Should().Be(originalEntity.IsHealthy);
        mappedBackEntity.LastResponseTimeMs.Should().Be(originalEntity.LastResponseTimeMs);
        mappedBackEntity.LastError.Should().Be(originalEntity.LastError);
    }
}






