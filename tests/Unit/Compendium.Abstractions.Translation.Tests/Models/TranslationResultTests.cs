// -----------------------------------------------------------------------
// <copyright file="TranslationResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Tests.Models;

public class TranslationResultTests
{
    [Fact]
    public void TranslationResult_Constructor_AssignsAllProperties()
    {
        // Act
        var result = new TranslationResult("Bonjour", "en", 0.95);

        // Assert
        result.TranslatedText.Should().Be("Bonjour");
        result.DetectedSourceLanguage.Should().Be("en");
        result.Confidence.Should().Be(0.95);
    }

    [Fact]
    public void TranslationResult_NullConfidence_IsAllowed()
    {
        // Act
        var result = new TranslationResult("Hola", "en", null);

        // Assert
        result.Confidence.Should().BeNull();
    }

    [Fact]
    public void TranslationResult_EquatableByValue_ReturnsTrueForIdenticalContent()
    {
        // Arrange
        var a = new TranslationResult("Hallo", "en", 0.8);
        var b = new TranslationResult("Hallo", "en", 0.8);

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void TranslationResult_EquatableByValue_ReturnsFalseWhenAnyFieldDiffers()
    {
        // Arrange
        var baseline = new TranslationResult("Hallo", "en", 0.8);

        // Act / Assert
        baseline.Should().NotBe(baseline with { TranslatedText = "Salut" });
        baseline.Should().NotBe(baseline with { DetectedSourceLanguage = "fr" });
        baseline.Should().NotBe(baseline with { Confidence = 0.5 });
        baseline.Should().NotBe(baseline with { Confidence = null });
    }

    [Fact]
    public void TranslationResult_WithExpression_ReplacesSingleField()
    {
        // Arrange
        var result = new TranslationResult("Ciao", "it", null);

        // Act
        var updated = result with { Confidence = 0.75 };

        // Assert
        updated.TranslatedText.Should().Be("Ciao");
        updated.DetectedSourceLanguage.Should().Be("it");
        updated.Confidence.Should().Be(0.75);
    }
}
