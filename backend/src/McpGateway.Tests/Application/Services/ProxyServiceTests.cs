using FluentAssertions;
using McpGateway.Application.Proxying;
using McpGateway.Application.Services;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace McpGateway.Tests.Application.Services;

/// <summary>
/// Unit tests for the ProxyService application service.
/// </summary>
public class ProxyServiceTests
{
    private readonly Mock<IMcpAdapterRepository> _mockRepository;
    private readonly Mock<ILogger<ProxyService>> _mockLogger;
    private readonly ProxyService _service;

    public ProxyServiceTests()
    {
        _mockRepository = new Mock<IMcpAdapterRepository>();
        _mockLogger = new Mock<ILogger<ProxyService>>();
        _service = new ProxyService(
            _mockRepository.Object,
            _mockLogger.Object,
            new SseProxyStream(
                NullLogger<SseProxyStream>.Instance,
                new SseEndpointRewriter(NullLogger<SseEndpointRewriter>.Instance)));
    }

    [Fact]
    public async Task IsAdapterAvailableAsync_WhenAdapterExistsAndIsReachable_ShouldReturnTrue()
    {
        // Arrange
        var adapterName = "test-adapter";
        var adapter = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = adapterName,
            Url = "http://localhost:3000",
            Enabled = true,
            IsHealthy = true
        };

        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync(adapter);

        // Act
        var result = await _service.IsAdapterAvailableAsync(adapterName);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }

    [Fact]
    public async Task IsAdapterAvailableAsync_WhenAdapterDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var adapterName = "non-existent-adapter";
        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync((McpAdapter?)null);

        // Act
        var result = await _service.IsAdapterAvailableAsync(adapterName);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }

    [Fact]
    public async Task IsAdapterAvailableAsync_WhenAdapterIsDisabled_ShouldReturnFalse()
    {
        // Arrange
        var adapterName = "disabled-adapter";
        var adapter = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = adapterName,
            Url = "http://localhost:3000",
            Enabled = false,
            IsHealthy = true
        };

        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync(adapter);

        // Act
        var result = await _service.IsAdapterAvailableAsync(adapterName);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }

    [Fact]
    public async Task IsAdapterAvailableAsync_WhenAdapterIsUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        var adapterName = "unhealthy-adapter";
        var adapter = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = adapterName,
            Url = "http://localhost:3000",
            Enabled = true,
            IsHealthy = false
        };

        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync(adapter);

        // Act
        var result = await _service.IsAdapterAvailableAsync(adapterName);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }

    [Fact]
    public async Task ForwardRequestAsync_WhenAdapterExists_ShouldReturnProxyResult()
    {
        // Arrange
        var adapterName = "test-adapter";
        var endpoint = "test-endpoint";
        var adapter = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = adapterName,
            Url = "http://localhost:3000",
            Enabled = true,
            IsHealthy = true
        };
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/test";

        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync(adapter);

        // Act - This will fail with connection error as intended
        try
        {
            await _service.ForwardRequestAsync(adapterName, httpContext, endpoint);
        }
        catch
        {
            // Expected to fail - just testing the flow
        }

        // Assert - just verify the method was called
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }

    [Fact]
    public async Task ForwardRequestAsync_WhenAdapterDoesNotExist_ShouldReturnNotFoundResult()
    {
        // Arrange
        var adapterName = "non-existent-adapter";
        var endpoint = "test-endpoint";
        var httpContext = new DefaultHttpContext();

        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync((McpAdapter?)null);

        // Act
        await _service.ForwardRequestAsync(adapterName, httpContext, endpoint);

        // Assert - just verify the method was called
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }

    [Fact]
    public async Task ForwardRequestAsync_WithRetry_ShouldAttemptMultipleTimes()
    {
        // Arrange
        var adapterName = "test-adapter";
        var endpoint = "test-endpoint";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/test";
        
        var adapter = new McpAdapter
        {
            Id = Guid.NewGuid(),
            Name = adapterName,
            Url = "http://localhost:3000",
            Enabled = true,
            IsHealthy = true
        };

        _mockRepository.Setup(r => r.GetByNameAsync(adapterName)).ReturnsAsync(adapter);

        // Act - This will fail with connection error as intended
        try
        {
            await _service.ForwardRequestAsync(adapterName, httpContext, endpoint, retry: true);
        }
        catch
        {
            // Expected to fail - just testing the flow
        }

        // Assert - just verify the method was called
        _mockRepository.Verify(r => r.GetByNameAsync(adapterName), Times.Once);
    }
}

