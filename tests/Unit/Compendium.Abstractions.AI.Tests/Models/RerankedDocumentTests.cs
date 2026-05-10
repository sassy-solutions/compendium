// -----------------------------------------------------------------------
// <copyright file="RerankedDocumentTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Tests.Models;

public sealed class RerankedDocumentTests
{
    [Fact]
    public void RerankedDocument_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var doc = new RerankedDocument
        {
            Index = 2,
            RelevanceScore = 0.87,
        };

        // Assert
        doc.Index.Should().Be(2);
        doc.RelevanceScore.Should().Be(0.87);
        doc.Document.Should().BeNull();
    }

    [Fact]
    public void RerankedDocument_WithDocument_PopulatesText()
    {
        // Arrange & Act
        var doc = new RerankedDocument
        {
            Index = 0,
            RelevanceScore = 0.99,
            Document = "The quick brown fox.",
        };

        // Assert
        doc.Document.Should().Be("The quick brown fox.");
    }

    [Fact]
    public void RerankedDocument_Equality_BasedOnValues()
    {
        // Arrange
        var a = new RerankedDocument { Index = 1, RelevanceScore = 0.5, Document = "x" };
        var b = new RerankedDocument { Index = 1, RelevanceScore = 0.5, Document = "x" };
        var c = new RerankedDocument { Index = 1, RelevanceScore = 0.6, Document = "x" };

        // Act & Assert
        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void RerankedDocument_WithExpression_ReturnsModifiedCopy()
    {
        // Arrange
        var original = new RerankedDocument { Index = 0, RelevanceScore = 0.1 };

        // Act
        var copy = original with { RelevanceScore = 0.95 };

        // Assert
        copy.Index.Should().Be(0);
        copy.RelevanceScore.Should().Be(0.95);
        original.RelevanceScore.Should().Be(0.1);
    }
}
