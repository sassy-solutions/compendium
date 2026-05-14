// -----------------------------------------------------------------------
// <copyright file="WebhookErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Webhooks.Tests;

public class WebhookErrorsTests
{
    [Fact]
    public void Prefix_Is_Webhook()
    {
        // Act / Assert
        WebhookErrors.Prefix.Should().Be("Webhook");
    }

    [Theory]
    [InlineData("ep-1")]
    [InlineData("")]
    [InlineData("very-long-endpoint-id-aaaaaaaaaaaaaaaaa")]
    public void EndpointNotFound_WithVariousIds_ReturnsNotFoundErrorEmbeddingId(string endpointId)
    {
        // Act
        var error = WebhookErrors.EndpointNotFound(endpointId);

        // Assert
        error.Code.Should().Be("Webhook.EndpointNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain($"'{endpointId}'");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/x")]
    [InlineData("")]
    public void InvalidUrl_WithVariousInputs_ReturnsValidationErrorEmbeddingUrl(string url)
    {
        // Act
        var error = WebhookErrors.InvalidUrl(url);

        // Assert
        error.Code.Should().Be("Webhook.InvalidUrl");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{url}'");
    }

    [Fact]
    public void SigningSecretMissing_WithEndpointId_ReturnsValidationErrorEmbeddingId()
    {
        // Arrange
        const string endpointId = "ep-7";

        // Act
        var error = WebhookErrors.SigningSecretMissing(endpointId);

        // Assert
        error.Code.Should().Be("Webhook.SigningSecretMissing");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{endpointId}'");
    }

    [Fact]
    public void ProviderUnreachable_WithReason_ReturnsUnavailableErrorEmbeddingReason()
    {
        // Arrange
        const string reason = "DNS lookup failed";

        // Act
        var error = WebhookErrors.ProviderUnreachable(reason);

        // Assert
        error.Code.Should().Be("Webhook.ProviderUnreachable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void RateLimited_WithReason_ReturnsTooManyRequestsErrorEmbeddingReason()
    {
        // Arrange
        const string reason = "100 req/sec exceeded";

        // Act
        var error = WebhookErrors.RateLimited(reason);

        // Assert
        error.Code.Should().Be("Webhook.RateLimited");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void AllErrorFactories_ReturnNonNullErrorInstances()
    {
        // Act / Assert
        WebhookErrors.EndpointNotFound("ep").Should().NotBeNull();
        WebhookErrors.InvalidUrl("u").Should().NotBeNull();
        WebhookErrors.SigningSecretMissing("ep").Should().NotBeNull();
        WebhookErrors.ProviderUnreachable("r").Should().NotBeNull();
        WebhookErrors.RateLimited("r").Should().NotBeNull();
    }

    [Fact]
    public void AllErrorCodes_StartWithWebhookPrefix()
    {
        // Act
        var codes = new[]
        {
            WebhookErrors.EndpointNotFound("ep").Code,
            WebhookErrors.InvalidUrl("u").Code,
            WebhookErrors.SigningSecretMissing("ep").Code,
            WebhookErrors.ProviderUnreachable("r").Code,
            WebhookErrors.RateLimited("r").Code,
        };

        // Assert
        codes.Should().OnlyContain(c => c.StartsWith("Webhook.", StringComparison.Ordinal));
    }
}
