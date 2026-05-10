// -----------------------------------------------------------------------
// <copyright file="FeatureFlagErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.FeatureFlags.Tests;

public class FeatureFlagErrorsTests
{
    [Fact]
    public void Prefix_IsFeatureFlags()
    {
        // Assert
        FeatureFlagErrors.Prefix.Should().Be("FeatureFlags");
    }

    [Fact]
    public void FlagNotFound_ReturnsNotFoundError()
    {
        // Act
        var error = FeatureFlagErrors.FlagNotFound("checkout-v2");

        // Assert
        error.Code.Should().Be("FeatureFlags.FlagNotFound");
        error.Message.Should().Contain("checkout-v2");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void InvalidContext_ReturnsValidationError()
    {
        // Act
        var error = FeatureFlagErrors.InvalidContext("missing tenantId");

        // Assert
        error.Code.Should().Be("FeatureFlags.InvalidContext");
        error.Message.Should().Contain("missing tenantId");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ProviderUnreachable_ReturnsUnavailableError()
    {
        // Act
        var error = FeatureFlagErrors.ProviderUnreachable("growthbook");

        // Assert
        error.Code.Should().Be("FeatureFlags.ProviderUnreachable");
        error.Message.Should().Contain("growthbook");
        error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public void RateLimited_WithoutRetryAfter_ReturnsTooManyRequestsErrorWithGenericMessage()
    {
        // Act
        var error = FeatureFlagErrors.RateLimited();

        // Assert
        error.Code.Should().Be("FeatureFlags.RateLimited");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("rate limited");
    }

    [Fact]
    public void RateLimited_WithRetryAfter_IncludesRetrySeconds()
    {
        // Act
        var error = FeatureFlagErrors.RateLimited(TimeSpan.FromSeconds(60));

        // Assert
        error.Code.Should().Be("FeatureFlags.RateLimited");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("60");
    }
}
