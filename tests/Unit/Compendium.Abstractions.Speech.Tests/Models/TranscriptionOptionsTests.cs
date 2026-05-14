// -----------------------------------------------------------------------
// <copyright file="TranscriptionOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class TranscriptionOptionsTests
{
    [Fact]
    public void TranscriptionOptions_Default_HasExpectedDefaults()
    {
        // Arrange / Act
        var opts = new TranscriptionOptions();

        // Assert
        opts.Language.Should().BeNull();
        opts.Model.Should().BeNull();
        opts.Diarization.Should().BeFalse();
        opts.Punctuation.Should().BeTrue();
    }

    [Fact]
    public void TranscriptionOptions_WithAllProperties_PreservesValues()
    {
        // Arrange / Act
        var opts = new TranscriptionOptions(
            Language: "fr-FR",
            Model: "whisper-large-v3",
            Diarization: true,
            Punctuation: false);

        // Assert
        opts.Language.Should().Be("fr-FR");
        opts.Model.Should().Be("whisper-large-v3");
        opts.Diarization.Should().BeTrue();
        opts.Punctuation.Should().BeFalse();
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    public void TranscriptionOptions_WithLanguage_PreservesValue(string language)
    {
        // Act
        var opts = new TranscriptionOptions(Language: language);

        // Assert
        opts.Language.Should().Be(language);
    }

    [Fact]
    public void TranscriptionOptions_RecordEquality_TwoIdentical_AreEqual()
    {
        // Arrange
        var first = new TranscriptionOptions("en", "whisper", true, false);
        var second = new TranscriptionOptions("en", "whisper", true, false);

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void TranscriptionOptions_RecordEquality_DifferingDiarization_AreNotEqual()
    {
        // Arrange
        var first = new TranscriptionOptions(Diarization: false);
        var second = new TranscriptionOptions(Diarization: true);

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void TranscriptionOptions_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new TranscriptionOptions();

        // Act
        var updated = original with { Language = "es-ES", Diarization = true };

        // Assert
        updated.Language.Should().Be("es-ES");
        updated.Diarization.Should().BeTrue();
        original.Language.Should().BeNull();
        original.Diarization.Should().BeFalse();
    }
}
