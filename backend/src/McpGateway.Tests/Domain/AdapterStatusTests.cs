using FluentAssertions;
using McpGateway.Domain.Enums;
using Xunit;

namespace McpGateway.Tests.Domain;

/// <summary>
/// Unit tests for the AdapterStatus enumeration.
/// </summary>
public class AdapterStatusTests
{
    [Theory]
    [InlineData(AdapterStatus.Unknown, 0)]
    [InlineData(AdapterStatus.Healthy, 1)]
    [InlineData(AdapterStatus.Unhealthy, 2)]
    [InlineData(AdapterStatus.Disabled, 3)]
    public void AdapterStatus_ShouldHaveCorrectValues(AdapterStatus status, int expectedValue)
    {
        // Act & Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void AdapterStatus_ShouldHaveAllExpectedValues()
    {
        // Arrange
        var expectedValues = new[] { 0, 1, 2, 3 };

        // Act
        var actualValues = Enum.GetValues<AdapterStatus>().Select(s => (int)s).ToArray();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Fact]
    public void AdapterStatus_ShouldHaveCorrectStringRepresentations()
    {
        // Act & Assert
        AdapterStatus.Unknown.ToString().Should().Be("Unknown");
        AdapterStatus.Healthy.ToString().Should().Be("Healthy");
        AdapterStatus.Unhealthy.ToString().Should().Be("Unhealthy");
        AdapterStatus.Disabled.ToString().Should().Be("Disabled");
    }

    [Theory]
    [InlineData(0, AdapterStatus.Unknown)]
    [InlineData(1, AdapterStatus.Healthy)]
    [InlineData(2, AdapterStatus.Unhealthy)]
    [InlineData(3, AdapterStatus.Disabled)]
    public void AdapterStatus_ShouldParseFromIntCorrectly(int value, AdapterStatus expectedStatus)
    {
        // Act
        var result = (AdapterStatus)value;

        // Assert
        result.Should().Be(expectedStatus);
    }
}






