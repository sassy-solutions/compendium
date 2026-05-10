// -----------------------------------------------------------------------
// <copyright file="SpeechErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests;

public class SpeechErrorsTests
{
    [Fact]
    public void UnsupportedFormat_WithMimeType_ReturnsValidationError()
    {
        // Arrange
        const string mimeType = "audio/x-unknown";

        // Act
        var error = SpeechErrors.UnsupportedFormat(mimeType);

        // Assert
        error.Code.Should().Be("Speech.UnsupportedFormat");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{mimeType}'");
    }

    [Theory]
    [InlineData("")]
    [InlineData("audio/wav")]
    [InlineData("application/octet-stream")]
    public void UnsupportedFormat_WithVariousMimes_EmbedsMimeInMessage(string mimeType)
    {
        // Act
        var error = SpeechErrors.UnsupportedFormat(mimeType);

        // Assert
        error.Code.Should().Be("Speech.UnsupportedFormat");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{mimeType}'");
    }

    [Fact]
    public void AudioTooLong_WithDurations_ReturnsValidationError()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(20);
        var maximum = TimeSpan.FromMinutes(10);

        // Act
        var error = SpeechErrors.AudioTooLong(duration, maximum);

        // Assert
        error.Code.Should().Be("Speech.AudioTooLong");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("1200");
        error.Message.Should().Contain("600");
    }

    [Fact]
    public void AudioTooLong_WithFractionalSeconds_FormatsBothValues()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(12.5);
        var maximum = TimeSpan.FromSeconds(10);

        // Act
        var error = SpeechErrors.AudioTooLong(duration, maximum);

        // Assert
        error.Message.Should().Contain("12.5");
        error.Message.Should().Contain("10");
    }

    [Fact]
    public void ProviderUnreachable_WithReason_ReturnsUnavailableError()
    {
        // Arrange
        const string reason = "DNS failure";

        // Act
        var error = SpeechErrors.ProviderUnreachable(reason);

        // Assert
        error.Code.Should().Be("Speech.ProviderUnreachable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void RateLimited_WithReason_ReturnsTooManyRequestsError()
    {
        // Arrange
        const string reason = "10 req/s exceeded";

        // Act
        var error = SpeechErrors.RateLimited(reason);

        // Assert
        error.Code.Should().Be("Speech.RateLimited");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void InvalidVoiceId_WithVoiceId_ReturnsValidationError()
    {
        // Arrange
        const string voiceId = "voice-unknown";

        // Act
        var error = SpeechErrors.InvalidVoiceId(voiceId);

        // Assert
        error.Code.Should().Be("Speech.InvalidVoiceId");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{voiceId}'");
    }

    [Theory]
    [InlineData("")]
    [InlineData("21m00Tcm4TlvDq8ikWAM")]
    [InlineData("voice-123")]
    public void InvalidVoiceId_WithVariousIds_EmbedsIdInMessage(string voiceId)
    {
        // Act
        var error = SpeechErrors.InvalidVoiceId(voiceId);

        // Assert
        error.Code.Should().Be("Speech.InvalidVoiceId");
        error.Message.Should().Contain($"'{voiceId}'");
    }

    [Fact]
    public void TextTooLong_WithLengths_ReturnsValidationError()
    {
        // Arrange
        const int length = 5000;
        const int maximum = 2500;

        // Act
        var error = SpeechErrors.TextTooLong(length, maximum);

        // Assert
        error.Code.Should().Be("Speech.TextTooLong");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("5000");
        error.Message.Should().Contain("2500");
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(10000, 9999)]
    [InlineData(int.MaxValue, 1)]
    public void TextTooLong_WithBoundaryValues_EmbedsBothInMessage(int length, int maximum)
    {
        // Act
        var error = SpeechErrors.TextTooLong(length, maximum);

        // Assert
        error.Message.Should().Contain(length.ToString());
        error.Message.Should().Contain(maximum.ToString());
    }

    [Fact]
    public void UnsupportedLanguage_WithLanguage_ReturnsValidationError()
    {
        // Arrange
        const string language = "xx-XX";

        // Act
        var error = SpeechErrors.UnsupportedLanguage(language);

        // Assert
        error.Code.Should().Be("Speech.UnsupportedLanguage");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{language}'");
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("klingon")]
    public void UnsupportedLanguage_WithVariousLanguages_EmbedsLanguageInMessage(string language)
    {
        // Act
        var error = SpeechErrors.UnsupportedLanguage(language);

        // Assert
        error.Message.Should().Contain($"'{language}'");
    }

    [Fact]
    public void AllErrorFactories_ReturnNonNullErrorInstances()
    {
        // Act / Assert
        SpeechErrors.UnsupportedFormat("audio/wav").Should().NotBeNull();
        SpeechErrors.AudioTooLong(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)).Should().NotBeNull();
        SpeechErrors.ProviderUnreachable("r").Should().NotBeNull();
        SpeechErrors.RateLimited("r").Should().NotBeNull();
        SpeechErrors.InvalidVoiceId("v").Should().NotBeNull();
        SpeechErrors.TextTooLong(1, 0).Should().NotBeNull();
        SpeechErrors.UnsupportedLanguage("l").Should().NotBeNull();
    }

    [Fact]
    public void AllErrorCodes_StartWithSpeechPrefix()
    {
        // Act
        var codes = new[]
        {
            SpeechErrors.UnsupportedFormat("m").Code,
            SpeechErrors.AudioTooLong(TimeSpan.Zero, TimeSpan.Zero).Code,
            SpeechErrors.ProviderUnreachable("r").Code,
            SpeechErrors.RateLimited("r").Code,
            SpeechErrors.InvalidVoiceId("v").Code,
            SpeechErrors.TextTooLong(1, 0).Code,
            SpeechErrors.UnsupportedLanguage("l").Code,
        };

        // Assert
        codes.Should().OnlyContain(c => c.StartsWith("Speech.", StringComparison.Ordinal));
    }
}
