// -----------------------------------------------------------------------
// <copyright file="RealtimeErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Tests;

public class RealtimeErrorsTests
{
    [Fact]
    public void Prefix_IsRealtime()
    {
        // Arrange / Act / Assert
        RealtimeErrors.Prefix.Should().Be("Realtime");
    }

    [Fact]
    public void ChannelNotAuthorized_ReturnsFailureWithChannelName()
    {
        // Act
        var error = RealtimeErrors.ChannelNotAuthorized("tenant-1:orders");

        // Assert
        error.Code.Should().Be("Realtime.ChannelNotAuthorized");
        error.Message.Should().Contain("tenant-1:orders");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void MessageTooLarge_ReturnsValidationErrorWithSizes()
    {
        // Act
        var error = RealtimeErrors.MessageTooLarge(20_000, 10_240);

        // Assert
        error.Code.Should().Be("Realtime.MessageTooLarge");
        error.Message.Should().Contain("20000");
        error.Message.Should().Contain("10240");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ProviderUnreachable_WithoutReason_ReturnsGenericMessage()
    {
        // Act
        var error = RealtimeErrors.ProviderUnreachable("pusher");

        // Assert
        error.Code.Should().Be("Realtime.ProviderUnreachable");
        error.Message.Should().Contain("pusher");
        error.Message.Should().EndWith("is unreachable.");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void ProviderUnreachable_WithReason_IncludesReason()
    {
        // Act
        var error = RealtimeErrors.ProviderUnreachable("ably", "DNS failure");

        // Assert
        error.Code.Should().Be("Realtime.ProviderUnreachable");
        error.Message.Should().Contain("ably");
        error.Message.Should().Contain("DNS failure");
    }

    [Fact]
    public void RateLimited_WithoutRetryAfter_ReturnsGenericMessage()
    {
        // Act
        var error = RealtimeErrors.RateLimited();

        // Assert
        error.Code.Should().Be("Realtime.RateLimited");
        error.Message.Should().Contain("Rate limit exceeded");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void RateLimited_WithRetryAfter_IncludesSeconds()
    {
        // Arrange
        var retry = TimeSpan.FromSeconds(45);

        // Act
        var error = RealtimeErrors.RateLimited(retry);

        // Assert
        error.Code.Should().Be("Realtime.RateLimited");
        error.Message.Should().Contain("45");
    }
}
