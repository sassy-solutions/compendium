// -----------------------------------------------------------------------
// <copyright file="TranslationErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Tests;

public class TranslationErrorsTests
{
    [Fact]
    public void Prefix_HasExpectedValue()
    {
        // Act / Assert
        TranslationErrors.Prefix.Should().Be("Translation");
    }

    [Fact]
    public void UnsupportedLanguage_ReturnsValidationError()
    {
        // Act
        var error = TranslationErrors.UnsupportedLanguage("klingon");

        // Assert
        error.Code.Should().Be("Translation.UnsupportedLanguage");
        error.Message.Should().Contain("klingon");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void TextTooLong_ReturnsValidationErrorMentioningLengthAndMaximum()
    {
        // Act
        var error = TranslationErrors.TextTooLong(length: 12_000, maximum: 5_000);

        // Assert
        error.Code.Should().Be("Translation.TextTooLong");
        error.Message.Should().Contain("12000");
        error.Message.Should().Contain("5000");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ProviderUnreachable_WithoutReason_ReturnsUnavailableErrorWithProviderName()
    {
        // Act
        var error = TranslationErrors.ProviderUnreachable("deepl");

        // Assert
        error.Code.Should().Be("Translation.ProviderUnreachable");
        error.Message.Should().Contain("deepl");
        error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public void ProviderUnreachable_WithReason_IncludesReasonInMessage()
    {
        // Act
        var error = TranslationErrors.ProviderUnreachable("deepl", "DNS lookup failed");

        // Assert
        error.Code.Should().Be("Translation.ProviderUnreachable");
        error.Message.Should().Contain("deepl");
        error.Message.Should().Contain("DNS lookup failed");
        error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public void RateLimited_WithoutRetryAfter_ReturnsTooManyRequestsErrorWithGenericMessage()
    {
        // Act
        var error = TranslationErrors.RateLimited();

        // Assert
        error.Code.Should().Be("Translation.RateLimited");
        error.Message.Should().Contain("rate limit");
        error.Type.Should().Be(ErrorType.TooManyRequests);
    }

    [Fact]
    public void RateLimited_WithRetryAfter_IncludesRetryWindowInMessage()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var error = TranslationErrors.RateLimited(retryAfter);

        // Assert
        error.Code.Should().Be("Translation.RateLimited");
        error.Message.Should().Contain("45");
        error.Type.Should().Be(ErrorType.TooManyRequests);
    }
}
