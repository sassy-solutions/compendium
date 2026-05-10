// -----------------------------------------------------------------------
// <copyright file="RerankOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Tests.Models;

public sealed class RerankOptionsTests
{
    [Fact]
    public void RerankOptions_Default_HasExpectedDefaults()
    {
        // Arrange & Act
        var options = new RerankOptions();

        // Assert
        options.Model.Should().BeNull();
        options.TopN.Should().BeNull();
        options.ReturnDocuments.Should().BeFalse();
    }

    [Fact]
    public void RerankOptions_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var options = new RerankOptions
        {
            Model = "rerank-english-v3.0",
            TopN = 5,
            ReturnDocuments = true,
        };

        // Assert
        options.Model.Should().Be("rerank-english-v3.0");
        options.TopN.Should().Be(5);
        options.ReturnDocuments.Should().BeTrue();
    }

    [Fact]
    public void RerankOptions_Equality_BasedOnValues()
    {
        // Arrange
        var a = new RerankOptions { Model = "m", TopN = 3, ReturnDocuments = true };
        var b = new RerankOptions { Model = "m", TopN = 3, ReturnDocuments = true };
        var c = new RerankOptions { Model = "m", TopN = 4, ReturnDocuments = true };

        // Act & Assert
        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void RerankOptions_WithExpression_ReturnsModifiedCopy()
    {
        // Arrange
        var original = new RerankOptions { Model = "m", TopN = 3 };

        // Act
        var copy = original with { TopN = 10 };

        // Assert
        copy.Model.Should().Be("m");
        copy.TopN.Should().Be(10);
        original.TopN.Should().Be(3);
    }
}
