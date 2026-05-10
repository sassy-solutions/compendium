// -----------------------------------------------------------------------
// <copyright file="TranslationOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Tests.Models;

public class TranslationOptionsTests
{
    [Fact]
    public void TranslationOptions_Constructor_AssignsAllProperties()
    {
        // Act
        var opts = new TranslationOptions("en", "fr", Formality.More);

        // Assert
        opts.SourceLanguage.Should().Be("en");
        opts.TargetLanguage.Should().Be("fr");
        opts.Formality.Should().Be(Formality.More);
    }

    [Fact]
    public void TranslationOptions_OmittedFormality_DefaultsToDefault()
    {
        // Act
        var opts = new TranslationOptions("en", "de");

        // Assert
        opts.Formality.Should().Be(Formality.Default);
    }

    [Fact]
    public void TranslationOptions_NullSourceLanguage_IsAllowed()
    {
        // Act
        var opts = new TranslationOptions(null, "es", Formality.PreferLess);

        // Assert
        opts.SourceLanguage.Should().BeNull();
        opts.TargetLanguage.Should().Be("es");
        opts.Formality.Should().Be(Formality.PreferLess);
    }

    [Fact]
    public void TranslationOptions_EquatableByValue_ReturnsTrueForIdenticalContent()
    {
        // Arrange
        var a = new TranslationOptions("en", "fr", Formality.More);
        var b = new TranslationOptions("en", "fr", Formality.More);

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void TranslationOptions_EquatableByValue_ReturnsFalseWhenAnyFieldDiffers()
    {
        // Arrange
        var baseline = new TranslationOptions("en", "fr", Formality.More);

        // Act / Assert
        baseline.Should().NotBe(baseline with { SourceLanguage = "de" });
        baseline.Should().NotBe(baseline with { TargetLanguage = "es" });
        baseline.Should().NotBe(baseline with { Formality = Formality.Less });
    }

    [Fact]
    public void TranslationOptions_WithExpression_ReplacesSingleField()
    {
        // Arrange
        var opts = new TranslationOptions("en", "fr", Formality.Default);

        // Act
        var updated = opts with { TargetLanguage = "it" };

        // Assert
        updated.SourceLanguage.Should().Be("en");
        updated.TargetLanguage.Should().Be("it");
        updated.Formality.Should().Be(Formality.Default);
    }
}
