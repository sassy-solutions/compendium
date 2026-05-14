// -----------------------------------------------------------------------
// <copyright file="AnalyticsErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Analytics.Tests;

public class AnalyticsErrorsTests
{
    [Fact]
    public void Prefix_ShouldBeAnalytics()
    {
        // Act
        var prefix = AnalyticsErrors.Prefix;

        // Assert
        prefix.Should().Be("Analytics");
    }

    [Fact]
    public void InvalidEvent_ShouldReturnValidationError()
    {
        // Act
        var error = AnalyticsErrors.InvalidEvent("name is empty");

        // Assert
        error.Code.Should().Be("Analytics.InvalidEvent");
        error.Message.Should().Contain("name is empty");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ProviderUnreachable_ShouldReturnFailureError()
    {
        // Act
        var error = AnalyticsErrors.ProviderUnreachable("posthog");

        // Assert
        error.Code.Should().Be("Analytics.ProviderUnreachable");
        error.Message.Should().Contain("posthog");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void RateLimited_WithoutRetryAfter_ShouldReturnGenericMessage()
    {
        // Act
        var error = AnalyticsErrors.RateLimited();

        // Assert
        error.Code.Should().Be("Analytics.RateLimited");
        error.Message.Should().Contain("rate limit");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void RateLimited_WithRetryAfter_ShouldIncludeRetryTime()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var error = AnalyticsErrors.RateLimited(retryAfter);

        // Assert
        error.Code.Should().Be("Analytics.RateLimited");
        error.Message.Should().Contain("45");
        error.Type.Should().Be(ErrorType.Failure);
    }
}
