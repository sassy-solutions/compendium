// -----------------------------------------------------------------------
// <copyright file="BillingErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Tests;

public class BillingErrorsTests
{
    [Fact]
    public void CustomerNotFound_WithCustomerId_ReturnsNotFoundErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string customerId = "cust-42";

        // Act
        var error = BillingErrors.CustomerNotFound(customerId);

        // Assert
        error.Code.Should().Be("Billing.CustomerNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(customerId);
    }

    [Fact]
    public void CustomerNotFoundByEmail_WithEmail_ReturnsNotFoundErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        var error = BillingErrors.CustomerNotFoundByEmail(email);

        // Assert
        error.Code.Should().Be("Billing.CustomerNotFoundByEmail");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(email);
    }

    [Fact]
    public void SubscriptionNotFound_WithSubscriptionId_ReturnsNotFoundErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string subscriptionId = "sub-1";

        // Act
        var error = BillingErrors.SubscriptionNotFound(subscriptionId);

        // Assert
        error.Code.Should().Be("Billing.SubscriptionNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(subscriptionId);
    }

    [Fact]
    public void NoActiveSubscription_WithCustomerId_ReturnsNotFoundErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string customerId = "cust-77";

        // Act
        var error = BillingErrors.NoActiveSubscription(customerId);

        // Assert
        error.Code.Should().Be("Billing.NoActiveSubscription");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(customerId);
    }

    [Fact]
    public void SubscriptionAlreadyCanceled_WithSubscriptionId_ReturnsConflictErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string subscriptionId = "sub-99";

        // Act
        var error = BillingErrors.SubscriptionAlreadyCanceled(subscriptionId);

        // Assert
        error.Code.Should().Be("Billing.SubscriptionAlreadyCanceled");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(subscriptionId);
    }

    [Fact]
    public void SubscriptionNotPausable_WithSubscriptionId_ReturnsConflictErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string subscriptionId = "sub-12";

        // Act
        var error = BillingErrors.SubscriptionNotPausable(subscriptionId);

        // Assert
        error.Code.Should().Be("Billing.SubscriptionNotPausable");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(subscriptionId);
    }

    [Fact]
    public void SubscriptionNotPaused_WithSubscriptionId_ReturnsConflictErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string subscriptionId = "sub-13";

        // Act
        var error = BillingErrors.SubscriptionNotPaused(subscriptionId);

        // Assert
        error.Code.Should().Be("Billing.SubscriptionNotPaused");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(subscriptionId);
    }

    [Fact]
    public void InvalidLicense_WithLicenseKey_ReturnsValidationErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string licenseKey = "LIC-ABCD";

        // Act
        var error = BillingErrors.InvalidLicense(licenseKey);

        // Assert
        error.Code.Should().Be("Billing.InvalidLicense");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(licenseKey);
    }

    [Fact]
    public void LicenseExpired_WithLicenseKey_ReturnsValidationErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string licenseKey = "LIC-EXP";

        // Act
        var error = BillingErrors.LicenseExpired(licenseKey);

        // Assert
        error.Code.Should().Be("Billing.LicenseExpired");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(licenseKey);
    }

    [Fact]
    public void LicenseActivationLimitReached_WithLicenseKey_ReturnsConflictErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string licenseKey = "LIC-LIMIT";

        // Act
        var error = BillingErrors.LicenseActivationLimitReached(licenseKey);

        // Assert
        error.Code.Should().Be("Billing.LicenseActivationLimitReached");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(licenseKey);
    }

    [Fact]
    public void LicenseInstanceNotFound_WithInstanceId_ReturnsNotFoundErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string instanceId = "inst-1";

        // Act
        var error = BillingErrors.LicenseInstanceNotFound(instanceId);

        // Assert
        error.Code.Should().Be("Billing.LicenseInstanceNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(instanceId);
    }

    [Fact]
    public void VariantNotFound_WithVariantId_ReturnsNotFoundErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string variantId = "var-1";

        // Act
        var error = BillingErrors.VariantNotFound(variantId);

        // Assert
        error.Code.Should().Be("Billing.VariantNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(variantId);
    }

    [Fact]
    public void InvalidWebhookSignature_StaticInstance_IsUnauthorizedErrorWithCorrectCode()
    {
        // Act
        var error = BillingErrors.InvalidWebhookSignature;

        // Assert
        error.Code.Should().Be("Billing.InvalidWebhookSignature");
        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Contain("signature");
    }

    [Fact]
    public void WebhookProcessingFailed_WithReason_ReturnsFailureErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        const string reason = "Bad payload";

        // Act
        var error = BillingErrors.WebhookProcessingFailed(reason);

        // Assert
        error.Code.Should().Be("Billing.WebhookProcessingFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void ProviderUnavailable_StaticInstance_IsUnavailableErrorWithCorrectCode()
    {
        // Act
        var error = BillingErrors.ProviderUnavailable;

        // Assert
        error.Code.Should().Be("Billing.ProviderUnavailable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().Contain("unavailable");
    }

    [Fact]
    public void RateLimitExceeded_StaticInstance_IsTooManyRequestsErrorWithCorrectCode()
    {
        // Act
        var error = BillingErrors.RateLimitExceeded;

        // Assert
        error.Code.Should().Be("Billing.RateLimitExceeded");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("Rate limit");
    }

    [Fact]
    public void TenantContextRequired_StaticInstance_IsValidationErrorWithCorrectCode()
    {
        // Act
        var error = BillingErrors.TenantContextRequired;

        // Assert
        error.Code.Should().Be("Billing.TenantContextRequired");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("Tenant");
    }

    [Theory]
    [InlineData("with-special-chars-!@#$")]
    [InlineData("very-long-id-1234567890-abcdefghij")]
    [InlineData("a")]
    public void CustomerNotFound_WithVariousIds_AlwaysIncludesIdInMessage(string customerId)
    {
        // Act
        var error = BillingErrors.CustomerNotFound(customerId);

        // Assert
        error.Code.Should().Be("Billing.CustomerNotFound");
        error.Message.Should().Contain(customerId);
    }

    [Fact]
    public void CustomerNotFound_WithEmptyId_StillReturnsCorrectErrorCode()
    {
        // Act
        var error = BillingErrors.CustomerNotFound(string.Empty);

        // Assert
        error.Code.Should().Be("Billing.CustomerNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void StaticReadonlyErrors_AreReferenceEqualOnRepeatedAccess()
    {
        // Act
        var first = BillingErrors.InvalidWebhookSignature;
        var second = BillingErrors.InvalidWebhookSignature;

        // Assert
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void StaticReadonlyErrors_TenantContextRequired_AreReferenceEqualOnRepeatedAccess()
    {
        // Act
        var first = BillingErrors.TenantContextRequired;
        var second = BillingErrors.TenantContextRequired;

        // Assert
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void StaticReadonlyErrors_ProviderUnavailable_AreReferenceEqualOnRepeatedAccess()
    {
        // Act
        var first = BillingErrors.ProviderUnavailable;
        var second = BillingErrors.ProviderUnavailable;

        // Assert
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void StaticReadonlyErrors_RateLimitExceeded_AreReferenceEqualOnRepeatedAccess()
    {
        // Act
        var first = BillingErrors.RateLimitExceeded;
        var second = BillingErrors.RateLimitExceeded;

        // Assert
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void Result_FromBillingError_FlowsThroughResultPattern()
    {
        // Arrange
        var error = BillingErrors.SubscriptionNotFound("sub-x");

        // Act
        Result result = Result.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(error);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
