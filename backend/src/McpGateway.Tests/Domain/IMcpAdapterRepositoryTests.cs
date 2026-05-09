using FluentAssertions;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Xunit;

namespace McpGateway.Tests.Domain;

/// <summary>
/// Unit tests for the IMcpAdapterRepository interface contract.
/// These tests verify the interface definition and method signatures.
/// </summary>
public class IMcpAdapterRepositoryTests
{
    [Fact]
    public void Interface_ShouldHaveExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IMcpAdapterRepository);

        // Act & Assert
        interfaceType.GetMethod(nameof(IMcpAdapterRepository.GetByIdAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapter?>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.GetByNameAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapter?>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.GetAllAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<IEnumerable<McpAdapter>>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.GetEnabledAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<IEnumerable<McpAdapter>>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.CreateAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapter>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.UpdateAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<McpAdapter>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.DeleteAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<bool>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.ExistsAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<bool>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.ExistsByNameAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<bool>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.SearchAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task<IEnumerable<McpAdapter>>));

        interfaceType.GetMethod(nameof(IMcpAdapterRepository.UpdateHealthStatusAsync))
            .Should().NotBeNull()
            .And.Subject.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void GetByIdAsync_ShouldAcceptGuidParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.GetByIdAsync));

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
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.GetByNameAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("name");
    }

    [Fact]
    public void CreateAsync_ShouldAcceptMcpAdapterParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.CreateAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(McpAdapter));
        method.GetParameters()[0].Name.Should().Be("adapter");
    }

    [Fact]
    public void UpdateAsync_ShouldAcceptMcpAdapterParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.UpdateAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(McpAdapter));
        method.GetParameters()[0].Name.Should().Be("adapter");
    }

    [Fact]
    public void DeleteAsync_ShouldAcceptGuidParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.DeleteAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
    }

    [Fact]
    public void ExistsAsync_ShouldAcceptGuidParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.ExistsAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
    }

    [Fact]
    public void ExistsByNameAsync_ShouldAcceptStringParameter()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.ExistsByNameAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[0].Name.Should().Be("name");
    }

    [Fact]
    public void SearchAsync_ShouldAcceptOptionalStringAndBoolParameters()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.SearchAsync));

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

    [Fact]
    public void UpdateHealthStatusAsync_ShouldAcceptCorrectParameters()
    {
        // Arrange
        var method = typeof(IMcpAdapterRepository).GetMethod(nameof(IMcpAdapterRepository.UpdateHealthStatusAsync));

        // Act & Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(4);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Guid));
        method.GetParameters()[0].Name.Should().Be("id");
        method.GetParameters()[1].ParameterType.Should().Be(typeof(bool));
        method.GetParameters()[1].Name.Should().Be("isHealthy");
        method.GetParameters()[2].ParameterType.Should().Be(typeof(int?));
        method.GetParameters()[2].Name.Should().Be("responseTimeMs");
        method.GetParameters()[2].HasDefaultValue.Should().BeTrue();
        method.GetParameters()[3].ParameterType.Should().Be(typeof(string));
        method.GetParameters()[3].Name.Should().Be("error");
        method.GetParameters()[3].HasDefaultValue.Should().BeTrue();
    }
}






