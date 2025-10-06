using AutoMapper;
using FluentAssertions;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using McpGateway.Infrastructure.Data;
using McpGateway.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace McpGateway.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for the McpAdapterRepository infrastructure service.
/// </summary>
public class McpAdapterRepositoryTests : IDisposable
{
    private readonly DbContextOptions<McpGatewayDbContext> _options;
    private readonly McpGatewayDbContext _context;
    private readonly Mock<IMapper> _mockMapper;
    private readonly McpAdapterRepository _repository;

    public McpAdapterRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<McpGatewayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new McpGatewayDbContext(_options);
        _mockMapper = new Mock<IMapper>();
        _repository = new McpAdapterRepository(_context, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ShouldReturnMappedModel()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new McpAdapterEntity
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Enabled = true
        };

        var model = new McpAdapter
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            Enabled = true
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(entity)).Returns(model);

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(model);
        _mockMapper.Verify(m => m.Map<McpAdapter>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
        _mockMapper.Verify(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()), Times.Never);
    }

    [Fact]
    public async Task GetByNameAsync_WhenEntityExists_ShouldReturnMappedModel()
    {
        // Arrange
        var name = "Test Adapter";
        var entity = new McpAdapterEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = "http://localhost:3000",
            Enabled = true
        };

        var model = new McpAdapter
        {
            Id = entity.Id,
            Name = name,
            Url = "http://localhost:3000",
            Enabled = true
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(entity)).Returns(model);

        // Act
        var result = await _repository.GetByNameAsync(name);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(model);
        _mockMapper.Verify(m => m.Map<McpAdapter>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenEntityDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var name = "Non-existent Adapter";

        // Act
        var result = await _repository.GetByNameAsync(name);

        // Assert
        result.Should().BeNull();
        _mockMapper.Verify(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()), Times.Never);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var name = "Test Adapter";
        var entity = new McpAdapterEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = "http://localhost:3000",
            Enabled = true
        };

        var model = new McpAdapter
        {
            Id = entity.Id,
            Name = name,
            Url = "http://localhost:3000",
            Enabled = true
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(entity)).Returns(model);

        // Act
        var result = await _repository.GetByNameAsync("test adapter");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(model);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllMappedModels()
    {
        // Arrange
        var entities = new List<McpAdapterEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", Url = "http://localhost:3001", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", Url = "http://localhost:3002", Enabled = false }
        };

        var models = entities.Select(e => new McpAdapter
        {
            Id = e.Id,
            Name = e.Name,
            Url = e.Url,
            Enabled = e.Enabled
        }).ToList();

        _context.McpAdapters.AddRange(entities);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()))
            .Returns<McpAdapterEntity>(e => models.First(m => m.Id == e.Id));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(models);
    }

    [Fact]
    public async Task GetEnabledAsync_ShouldReturnOnlyEnabledMappedModels()
    {
        // Arrange
        var entities = new List<McpAdapterEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", Url = "http://localhost:3001", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", Url = "http://localhost:3002", Enabled = false },
            new() { Id = Guid.NewGuid(), Name = "Adapter 3", Url = "http://localhost:3003", Enabled = true }
        };

        var enabledModels = entities.Where(e => e.Enabled).Select(e => new McpAdapter
        {
            Id = e.Id,
            Name = e.Name,
            Url = e.Url,
            Enabled = e.Enabled
        }).ToList();

        _context.McpAdapters.AddRange(entities);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()))
            .Returns<McpAdapterEntity>(e => enabledModels.FirstOrDefault(m => m.Id == e.Id) ?? new McpAdapter());

        // Act
        var result = await _repository.GetEnabledAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.Enabled).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddEntityAndReturnMappedModel()
    {
        // Arrange
        var model = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "New Adapter",
            Url = "http://localhost:3000",
            Enabled = true
        };

        var entity = new McpAdapterEntity
        {
            Id = model.Id,
            Name = model.Name,
            Url = model.Url,
            Enabled = model.Enabled
        };

        _mockMapper.Setup(m => m.Map<McpAdapterEntity>(model)).Returns(entity);
        _mockMapper.Setup(m => m.Map<McpAdapter>(entity)).Returns(model);

        // Act
        var result = await _repository.CreateAsync(model);

        // Assert
        result.Should().Be(model);
        _context.McpAdapters.Should().HaveCount(1);
        _context.McpAdapters.First().Name.Should().Be("New Adapter");
        _mockMapper.Verify(m => m.Map<McpAdapterEntity>(model), Times.Once);
        _mockMapper.Verify(m => m.Map<McpAdapter>(entity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityExists_ShouldUpdateAndReturnMappedModel()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingEntity = new McpAdapterEntity
        {
            Id = id,
            Name = "Original Adapter",
            Url = "http://localhost:3000",
            Enabled = true
        };

        var updatedModel = new McpAdapter
        {
            Id = id,
            Name = "Updated Adapter",
            Url = "http://localhost:3001",
            Enabled = false
        };

        _context.McpAdapters.Add(existingEntity);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map(updatedModel, existingEntity))
            .Callback<McpAdapter, McpAdapterEntity>((source, dest) => {
                dest.Name = source.Name;
                dest.Url = source.Url;
                dest.Enabled = source.Enabled;
                dest.Description = source.Description;
                dest.TimeoutSeconds = source.TimeoutSeconds;
                dest.UpdatedAt = source.UpdatedAt;
                dest.UpdatedBy = source.UpdatedBy;
            });
        _mockMapper.Setup(m => m.Map<McpAdapter>(existingEntity)).Returns(updatedModel);

        // Act
        var result = await _repository.UpdateAsync(updatedModel);

        // Assert
        result.Should().Be(updatedModel);
        var updatedEntity = _context.McpAdapters.First();
        updatedEntity.Name.Should().Be("Updated Adapter");
        updatedEntity.Url.Should().Be("http://localhost:3001");
        updatedEntity.Enabled.Should().BeFalse();
        _mockMapper.Verify(m => m.Map(updatedModel, existingEntity), Times.Once);
        _mockMapper.Verify(m => m.Map<McpAdapter>(existingEntity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var model = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = "Non-existent Adapter",
            Url = "http://localhost:3000"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.UpdateAsync(model));
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new McpAdapterEntity
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000"
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();
        _context.McpAdapters.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityExists_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new McpAdapterEntity
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000"
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_WhenEntityExists_ShouldReturnTrue()
    {
        // Arrange
        var name = "Test Adapter";
        var entity = new McpAdapterEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = "http://localhost:3000"
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync(name);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_WhenEntityDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var name = "Non-existent Adapter";

        // Act
        var result = await _repository.ExistsByNameAsync(name);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var name = "Test Adapter";
        var entity = new McpAdapterEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = "http://localhost:3000"
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync("test adapter");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithNameFilter_ShouldReturnMatchingModels()
    {
        // Arrange
        var entities = new List<McpAdapterEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Adapter 1", Url = "http://localhost:3001", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Another Adapter", Url = "http://localhost:3002", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Test Adapter 2", Url = "http://localhost:3003", Enabled = false }
        };

        var models = entities.Select(e => new McpAdapter
        {
            Id = e.Id,
            Name = e.Name,
            Url = e.Url,
            Enabled = e.Enabled
        }).ToList();

        _context.McpAdapters.AddRange(entities);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()))
            .Returns<McpAdapterEntity>(e => models.First(m => m.Id == e.Id));

        // Act
        var result = await _repository.SearchAsync("Test", null);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.Name.Contains("Test")).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithEnabledFilter_ShouldReturnMatchingModels()
    {
        // Arrange
        var entities = new List<McpAdapterEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", Url = "http://localhost:3001", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", Url = "http://localhost:3002", Enabled = false },
            new() { Id = Guid.NewGuid(), Name = "Adapter 3", Url = "http://localhost:3003", Enabled = true }
        };

        var models = entities.Select(e => new McpAdapter
        {
            Id = e.Id,
            Name = e.Name,
            Url = e.Url,
            Enabled = e.Enabled
        }).ToList();

        _context.McpAdapters.AddRange(entities);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()))
            .Returns<McpAdapterEntity>(e => models.First(m => m.Id == e.Id));

        // Act
        var result = await _repository.SearchAsync(null, true);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.Enabled).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithBothFilters_ShouldReturnMatchingModels()
    {
        // Arrange
        var entities = new List<McpAdapterEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Adapter 1", Url = "http://localhost:3001", Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Test Adapter 2", Url = "http://localhost:3002", Enabled = false },
            new() { Id = Guid.NewGuid(), Name = "Another Adapter", Url = "http://localhost:3003", Enabled = true }
        };

        var models = entities.Select(e => new McpAdapter
        {
            Id = e.Id,
            Name = e.Name,
            Url = e.Url,
            Enabled = e.Enabled
        }).ToList();

        _context.McpAdapters.AddRange(entities);
        await _context.SaveChangesAsync();

        _mockMapper.Setup(m => m.Map<McpAdapter>(It.IsAny<McpAdapterEntity>()))
            .Returns<McpAdapterEntity>(e => models.First(m => m.Id == e.Id));

        // Act
        var result = await _repository.SearchAsync("Test", true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Test Adapter 1");
        result.First().Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateHealthStatusAsync_WhenEntityExists_ShouldUpdateHealthProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new McpAdapterEntity
        {
            Id = id,
            Name = "Test Adapter",
            Url = "http://localhost:3000",
            IsHealthy = false
        };

        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();

        var isHealthy = true;
        var responseTimeMs = 150;
        var error = "No errors";

        // Act
        await _repository.UpdateHealthStatusAsync(id, isHealthy, responseTimeMs, error);

        // Assert
        var updatedEntity = _context.McpAdapters.First();
        updatedEntity.IsHealthy.Should().Be(isHealthy);
        updatedEntity.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        updatedEntity.LastResponseTimeMs.Should().Be(responseTimeMs);
        updatedEntity.LastError.Should().Be(error);
    }

    [Fact]
    public async Task UpdateHealthStatusAsync_WhenEntityDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act & Assert
        await _repository.UpdateHealthStatusAsync(id, true, 150, "No errors");
        // Should not throw exception
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
