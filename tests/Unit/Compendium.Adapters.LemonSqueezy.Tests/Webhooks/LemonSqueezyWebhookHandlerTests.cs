// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyWebhookHandlerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Tests.Helpers;
using FluentAssertions;

namespace Compendium.Adapters.LemonSqueezy.Tests.Webhooks;

/// <summary>
/// Unit tests for the internal <c>LemonSqueezyWebhookHandler</c> exercised via the public
/// <see cref="IPaymentWebhookHandler"/> contract. Webhook signatures use HMAC-SHA256 in
/// lowercase hex format (with optional <c>sha256=</c> prefix).
/// </summary>
public class LemonSqueezyWebhookHandlerTests
{
    private const string SigningSecret = "ls_whsec_unit_test_secret";

    private const string SamplePayload = """
    {"meta":{"event_name":"subscription_created","custom_data":{"tenant_id":"tenant-42"}},"data":{"type":"subscriptions","id":"sub_99","attributes":{"status":"active","customer_id":1,"product_id":2,"variant_id":3,"user_email":"alice@example.com"}}}
    """;

    private static LemonSqueezyOptions CreateOptions(string secret = SigningSecret) => new()
    {
        WebhookSigningSecret = secret
    };

    // ============================================================================
    // Signature validation
    // ============================================================================

    [Fact]
    public async Task ProcessWebhookAsync_ValidSignature_ReturnsSuccess()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions());
        var signature = LemonSqueezyTestHelpers.ComputeWebhookSignature(SigningSecret, SamplePayload);

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, signature, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Processed.Should().BeTrue();
        result.Value.EventType.Should().Be("subscription_created");
        result.Value.ResourceType.Should().Be("subscriptions");
        result.Value.ResourceId.Should().Be("sub_99");
        result.Value.TenantId.Should().Be("tenant-42");
    }

    [Fact]
    public async Task ProcessWebhookAsync_ValidSignatureWithSha256Prefix_ReturnsSuccess()
    {
        // Arrange — LemonSqueezy supports both raw hex and "sha256=<hex>" headers
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions());
        var rawSig = LemonSqueezyTestHelpers.ComputeWebhookSignature(SigningSecret, SamplePayload);
        var signature = "sha256=" + rawSig;

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, signature, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvalidSignature_ReturnsInvalidSignatureError()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions());

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, "deadbeef", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidWebhookSignature");
    }

    [Fact]
    public async Task ProcessWebhookAsync_SignatureWithDifferentSecret_ReturnsInvalidSignatureError()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions());
        var wrongSig = LemonSqueezyTestHelpers.ComputeWebhookSignature("other_secret", SamplePayload);

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, wrongSig, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidWebhookSignature");
    }

    [Fact]
    public async Task ProcessWebhookAsync_EmptySigningSecret_BypassesValidation()
    {
        // Arrange — dev mode: no secret means we accept anything
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, "irrelevant", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("subscription_created");
    }

    // ============================================================================
    // Payload parsing & validation
    // ============================================================================

    [Fact]
    public async Task ProcessWebhookAsync_PayloadNull_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions());

        // Act
        var act = async () => await handler.ProcessWebhookAsync(null!, "sig", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessWebhookAsync_SignatureNull_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions());

        // Act
        var act = async () => await handler.ProcessWebhookAsync(SamplePayload, null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessWebhookAsync_MalformedJson_ReturnsProcessingFailedError()
    {
        // Arrange — dev mode lets the malformed payload reach the deserializer
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync("{not-json", string.Empty, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.WebhookProcessingFailed");
    }

    [Fact]
    public async Task ProcessWebhookAsync_MissingMeta_ReturnsProcessingFailedError()
    {
        // Arrange — JSON parses but lacks the required meta object
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(
            "{\"data\":{\"type\":\"subscriptions\",\"id\":\"x\"}}",
            string.Empty,
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.WebhookProcessingFailed");
    }

    [Fact]
    public async Task ProcessWebhookAsync_MissingEventName_ReturnsUnknownEventType()
    {
        // Arrange — empty meta still produces a successful result with EventName = "unknown"
        var payload = "{\"meta\":{},\"data\":{\"type\":\"sub\",\"id\":\"1\"}}";
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(payload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("unknown");
    }

    [Fact]
    public async Task ProcessWebhookAsync_NullData_ReturnsSuccessWithoutResourceFields()
    {
        // Arrange
        var payload = "{\"meta\":{\"event_name\":\"order_created\"}}";
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(payload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("order_created");
        result.Value.ResourceId.Should().BeNull();
        result.Value.ResourceType.Should().BeNull();
        result.Value.ExtractedData.Should().BeNull();
    }

    // ============================================================================
    // Event type dispatch (covers every switch arm)
    // ============================================================================

    [Theory]
    [InlineData("subscription_created")]
    [InlineData("subscription_updated")]
    [InlineData("subscription_cancelled")]
    [InlineData("subscription_resumed")]
    [InlineData("subscription_expired")]
    [InlineData("subscription_paused")]
    [InlineData("subscription_unpaused")]
    [InlineData("subscription_payment_success")]
    [InlineData("subscription_payment_failed")]
    [InlineData("subscription_payment_recovered")]
    [InlineData("order_created")]
    [InlineData("order_refunded")]
    [InlineData("license_key_created")]
    [InlineData("license_key_updated")]
    [InlineData("totally_unknown_event")]
    public async Task ProcessWebhookAsync_AcknowledgesAllSupportedAndUnknownEvents(string eventName)
    {
        // Arrange
        var payload = "{\"meta\":{\"event_name\":\"" + eventName + "\"},\"data\":{\"type\":\"x\",\"id\":\"1\"}}";
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(payload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be(eventName);
    }

    // ============================================================================
    // Data extraction
    // ============================================================================

    [Fact]
    public async Task ProcessWebhookAsync_FullAttributesPayload_ExtractsKnownFields()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExtractedData.Should().NotBeNull();
        var data = result.Value.ExtractedData!;
        data.Should().ContainKey("resource_id");
        data.Should().ContainKey("resource_type");
        data.Should().ContainKey("status");
        data.Should().ContainKey("customer_id");
        data.Should().ContainKey("product_id");
        data.Should().ContainKey("variant_id");
        data.Should().ContainKey("user_email");
        data["status"].Should().Be("active");
        data["user_email"].Should().Be("alice@example.com");
    }

    [Fact]
    public async Task ProcessWebhookAsync_PayloadWithCustomData_PrefixesCustomKeys()
    {
        // Arrange
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExtractedData.Should().NotBeNull();
        result.Value.ExtractedData!.Should().ContainKey("custom_tenant_id");
    }

    [Fact]
    public async Task ProcessWebhookAsync_NoTenantIdInCustomData_ReturnsNullTenantId()
    {
        // Arrange
        var payload = "{\"meta\":{\"event_name\":\"order_created\"},\"data\":{\"type\":\"orders\",\"id\":\"o-1\"}}";
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(payload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task ProcessWebhookAsync_DataMissingTypeAndId_ReturnsNullResourceFields()
    {
        // Arrange
        var payload = "{\"meta\":{\"event_name\":\"order_created\"},\"data\":{}}";
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(payload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().BeNull();
        result.Value.ResourceId.Should().BeNull();
    }

    [Fact]
    public async Task ProcessWebhookAsync_DataIsNotObject_StillSucceedsWithoutResourceFields()
    {
        // Arrange — data is a string instead of an object; must not throw
        var payload = "{\"meta\":{\"event_name\":\"order_created\"},\"data\":\"not-an-object\"}";
        var handler = LemonSqueezyTestHelpers.CreateWebhookHandler(CreateOptions(secret: string.Empty));

        // Act
        var result = await handler.ProcessWebhookAsync(payload, string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceId.Should().BeNull();
        result.Value.ResourceType.Should().BeNull();
    }
}
