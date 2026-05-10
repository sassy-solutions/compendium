// -----------------------------------------------------------------------
// <copyright file="ExperimentAssignmentTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.FeatureFlags.Tests.Models;

public class ExperimentAssignmentTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        // Arrange
        var value = new { copy = "Buy now" };

        // Act
        var assignment = new ExperimentAssignment("v1", value, InExperiment: true);

        // Assert
        assignment.VariationKey.Should().Be("v1");
        assignment.Value.Should().BeSameAs(value);
        assignment.InExperiment.Should().BeTrue();
    }

    [Fact]
    public void Constructor_AllowsFallbackAssignment()
    {
        // Act
        var assignment = new ExperimentAssignment("control", "default", InExperiment: false);

        // Assert
        assignment.VariationKey.Should().Be("control");
        assignment.Value.Should().Be("default");
        assignment.InExperiment.Should().BeFalse();
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        // Act
        var a = new ExperimentAssignment("v1", 42, true);
        var b = new ExperimentAssignment("v1", 42, true);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Records_WithDifferentVariation_AreNotEqual()
    {
        // Act
        var a = new ExperimentAssignment("v1", 42, true);
        var b = new ExperimentAssignment("v2", 42, true);

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Records_WithDifferentEnrollment_AreNotEqual()
    {
        // Act
        var a = new ExperimentAssignment("v1", 42, true);
        var b = new ExperimentAssignment("v1", 42, false);

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void With_AllowsNonDestructiveMutation()
    {
        // Arrange
        var original = new ExperimentAssignment("v1", 42, true);

        // Act
        var mutated = original with { VariationKey = "v2" };

        // Assert
        mutated.VariationKey.Should().Be("v2");
        mutated.Value.Should().Be(42);
        mutated.InExperiment.Should().BeTrue();
        original.VariationKey.Should().Be("v1");
    }
}
