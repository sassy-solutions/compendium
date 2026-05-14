// -----------------------------------------------------------------------
// <copyright file="NotificationErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests;

public class NotificationErrorsTests
{
    [Fact]
    public void InvalidToken_WithToken_ReturnsValidationError()
    {
        // Arrange
        const string token = "tok-xyz";

        // Act
        var error = NotificationErrors.InvalidToken(token);

        // Assert
        error.Code.Should().Be("Notification.InvalidToken");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{token}'");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("a-very-long-device-token-value-that-could-be-256-chars")]
    public void InvalidToken_WithVariousTokens_EmbedsTokenInMessage(string token)
    {
        // Act
        var error = NotificationErrors.InvalidToken(token);

        // Assert
        error.Code.Should().Be("Notification.InvalidToken");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{token}'");
    }

    [Fact]
    public void ProviderUnreachable_WithReason_ReturnsUnavailableError()
    {
        // Arrange
        const string reason = "DNS resolution failed";

        // Act
        var error = NotificationErrors.ProviderUnreachable(reason);

        // Assert
        error.Code.Should().Be("Notification.ProviderUnreachable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void RateLimited_WithReason_ReturnsTooManyRequestsError()
    {
        // Arrange
        const string reason = "100 req/sec exceeded";

        // Act
        var error = NotificationErrors.RateLimited(reason);

        // Assert
        error.Code.Should().Be("Notification.RateLimited");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain(reason);
    }

    [Theory]
    [InlineData(5000, 4096)]
    [InlineData(1, 0)]
    [InlineData(int.MaxValue, 1024)]
    public void PayloadTooLarge_WithSizes_ReturnsValidationErrorEmbeddingBothSizes(int sizeBytes, int maxBytes)
    {
        // Act
        var error = NotificationErrors.PayloadTooLarge(sizeBytes, maxBytes);

        // Assert
        error.Code.Should().Be("Notification.PayloadTooLarge");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(sizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        error.Message.Should().Contain(maxBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AllErrorFactories_ReturnNonNullErrorInstances()
    {
        // Act / Assert
        NotificationErrors.InvalidToken("t").Should().NotBeNull();
        NotificationErrors.ProviderUnreachable("r").Should().NotBeNull();
        NotificationErrors.RateLimited("r").Should().NotBeNull();
        NotificationErrors.PayloadTooLarge(1, 0).Should().NotBeNull();
        NotificationErrors.InvalidPhoneNumber("+1").Should().NotBeNull();
        NotificationErrors.MessageTooLong(1, 0).Should().NotBeNull();
    }

    [Fact]
    public void AllErrorCodes_StartWithNotificationPrefix()
    {
        // Act
        var codes = new[]
        {
            NotificationErrors.InvalidToken("t").Code,
            NotificationErrors.ProviderUnreachable("r").Code,
            NotificationErrors.RateLimited("r").Code,
            NotificationErrors.PayloadTooLarge(1, 0).Code,
            NotificationErrors.InvalidPhoneNumber("+1").Code,
            NotificationErrors.MessageTooLong(1, 0).Code,
        };

        // Assert
        codes.Should().OnlyContain(c => c.StartsWith("Notification.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("not-a-number")]
    [InlineData("")]
    public void InvalidPhoneNumber_WithVariousInputs_EmbedsNumberInMessage(string phoneNumber)
    {
        // Act
        var error = NotificationErrors.InvalidPhoneNumber(phoneNumber);

        // Assert
        error.Code.Should().Be("Notification.InvalidPhoneNumber");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{phoneNumber}'");
    }

    [Theory]
    [InlineData(1700, 1600)]
    [InlineData(161, 160)]
    [InlineData(1, 0)]
    public void MessageTooLong_WithLengths_EmbedsBothLengthsInMessage(int length, int maxLength)
    {
        // Act
        var error = NotificationErrors.MessageTooLong(length, maxLength);

        // Assert
        error.Code.Should().Be("Notification.MessageTooLong");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        error.Message.Should().Contain(maxLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
