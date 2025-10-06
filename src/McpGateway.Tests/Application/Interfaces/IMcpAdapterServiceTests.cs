using FluentAssertions;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using Xunit;

namespace McpGateway.Tests.Application.Interfaces;

/// <summary>
/// Unit tests for the IMcpAdapterService interface contract.
/// These tests verify the interface definition and method signatures.
/// </summary>
public class IMcpAdapterServiceTests
{
    [Fact]
    public void Interface_ShouldHaveExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IMcpAdapterService);

        // Act & Assert
        interfaceType.GetMethod(nameof(IMcpAdapterService.GetByIdAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapterDto?>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.GetByNameAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapterDto?>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.GetAllAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<AdapterListDto>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.GetEnabledAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<AdapterListDto>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.CreateAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapterDto>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.UpdateAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapterDto>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.DeleteAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<bool>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.CheckHealthAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<AdapterHealthDto>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.CheckAllHealthAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<IEnumerable<AdapterHealthDto>>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.SearchAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<AdapterListDto>));

        interfaceType.GetMethod(nameof(IMcpAdapterService.ReloadMappingsAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<bool>));
    }

    [Fact]
    public void GetByIdAsync_ShouldAcceptGuidParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.GetByIdAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
    }

    [Fact]
    public void GetByNameAsync_ShouldAcceptStringParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.GetByNameAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("name");
    }

    [Fact]
    public void CreateAsync_ShouldAcceptCreateMcpAdapterDtoParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.CreateAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(CreateMcpAdapterDto));
        method.GetParameters()[0].Name.Should().Be("dto");
    }

    [Fact]
    public void UpdateAsync_ShouldAcceptGuidAndUpdateMcpAdapterDtoParameters()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.UpdateAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(2);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
        method.GetParameters()[1].ParameterType.Should().Be(typeof(UpdateMcpAdapterDto));
        method.GetParameters()[1].Name.Should().Be("dto");
    }

    [Fact]
    public void DeleteAsync_ShouldAcceptGuidParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.DeleteAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
    }

    [Fact]
    public void CheckHealthAsync_ShouldAcceptGuidParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.CheckHealthAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
    }

    [Fact]
    public void SearchAsync_ShouldAcceptOptionalStringAndBoolParameters()
    {
        // Arrange
        var method = typeof(IMcpAdapterService).GetMethod(nameof(IMcpAdapterService.SearchAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(2);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("name");
        method.GetParameters()[0].HasDefaultValue.Should().BeTrue();
        method.GetParameters()[1].ParameterType.Should().Be(typeof(bool?));
        method.GetParameters()[1].Name.Should().Be("enabled");
        method.GetParameters()[1].HasDefaultValue.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for the IProxyService interface contract.
/// These tests verify the interface definition and method signatures.
/// </summary>
public class IProxyServiceTests
{
    [Fact]
    public void Interface_ShouldHaveExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IProxyService);

        // Act & Assert
        interfaceType.GetMethod(nameof(IProxyService.ForwardRequestAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<ProxyResult>));

        interfaceType.GetMethod(nameof(IProxyService.ForwardGetRequestAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<ProxyResult>));

        interfaceType.GetMethod(nameof(IProxyService.IsAdapterAvailableAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<bool>));

        interfaceType.GetMethod(nameof(IProxyService.ForwardSseRequestAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<ProxyResult>));
    }

    [Fact]
    public void ForwardRequestAsync_ShouldAcceptCorrectParameters()
    {
        // Arrange
        var method = typeof(IProxyService).GetMethod(nameof(IProxyService.ForwardRequestAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(4);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("adapterName");
        method.GetParameters()[1].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[1].Name.Should().Be("endpoint");
        method.GetParameters()[2].ParameterType.Should().Be(typeof(System.Text.Json.JsonElement));
        method.GetParameters()[2].Name.Should().Be("body");
        method.GetParameters()[3].ParameterType.Should().Be(typeof(bool));
        method.GetParameters()[3].Name.Should().Be("retry");
        method.GetParameters()[3].HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void ForwardGetRequestAsync_ShouldAcceptCorrectParameters()
    {
        // Arrange
        var method = typeof(IProxyService).GetMethod(nameof(IProxyService.ForwardGetRequestAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(3);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("adapterName");
        method.GetParameters()[1].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[1].Name.Should().Be("endpoint");
        method.GetParameters()[2].ParameterType.Should().Be(typeof(bool));
        method.GetParameters()[2].Name.Should().Be("retry");
        method.GetParameters()[2].HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void IsAdapterAvailableAsync_ShouldAcceptStringParameter()
    {
        // Arrange
        var method = typeof(IProxyService).GetMethod(nameof(IProxyService.IsAdapterAvailableAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("adapterName");
    }

    [Fact]
    public void ForwardStreamableHttpRequestAsync_ShouldAcceptCorrectParameters()
    {
        // Arrange
        var sseMethods = typeof(IProxyService).GetMethods().Where(m => m.Name == nameof(IProxyService.ForwardSseRequestAsync)).ToArray();
        var streamableMethods = typeof(IProxyService).GetMethods().Where(m => m.Name == nameof(IProxyService.ForwardStreamableHttpRequestAsync)).ToArray();

        // Act & Assert
        sseMethods.Should().HaveCount(1);
        streamableMethods.Should().HaveCount(1);

        // Check ForwardSseRequestAsync method
        var sseMethod = sseMethods.First();
        sseMethod.GetParameters().Should().HaveCount(2);
        sseMethod.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        sseMethod.GetParameters()[0].Name.Should().Be("adapterName");
        sseMethod.GetParameters()[1].ParameterType.Should().Be(typeof(Microsoft.AspNetCore.Http.HttpContext));
        sseMethod.GetParameters()[1].Name.Should().Be("httpContext");

        // Check ForwardStreamableHttpRequestAsync method
        var streamableMethod = streamableMethods.First();
        streamableMethod.GetParameters().Should().HaveCount(3);
        streamableMethod.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        streamableMethod.GetParameters()[0].Name.Should().Be("adapterName");
        streamableMethod.GetParameters()[1].ParameterType.Should().Be(typeof(Microsoft.AspNetCore.Http.HttpContext));
        streamableMethod.GetParameters()[1].Name.Should().Be("httpContext");
        streamableMethod.GetParameters()[2].ParameterType.Should().Be(typeof(CancellationToken));
        streamableMethod.GetParameters()[2].Name.Should().Be("cancellationToken");
    }
}
