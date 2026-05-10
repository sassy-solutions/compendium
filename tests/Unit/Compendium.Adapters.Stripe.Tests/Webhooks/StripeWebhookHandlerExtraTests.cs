// -----------------------------------------------------------------------
// <copyright file="StripeWebhookHandlerExtraTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Compendium.Abstractions.Billing;
using Compendium.Adapters.Stripe.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Stripe.Tests.Webhooks;

/// <summary>
/// Additional webhook coverage exercising every branch of the resource-metadata
/// extractor: subscription, customer (already covered in the baseline test),
/// checkout_session, invoice, generic <c>IHasId+IHasObject</c>, and the empty
/// metadata path.
/// </summary>
public sealed class StripeWebhookHandlerExtraTests
{
    private const string SigningSecret = "whsec_test_secret_extra";

    [Fact]
    public async Task ProcessWebhookAsync_SubscriptionEvent_ExtractsSubscriptionResource()
    {
        // Arrange
        var payload = WrapEvent(
            "evt_sub_1",
            "customer.subscription.created",
            objectJson: """
                {
                  "id": "sub_xyz",
                  "object": "subscription",
                  "metadata": { "tenant_id": "t-sub", "campaign": "spring" }
                }
                """);
        var handler = CreateHandler(SigningSecret);

        // Act
        var result = await handler.ProcessWebhookAsync(payload, SignPayload(SigningSecret, payload));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("subscription");
        result.Value.ResourceId.Should().Be("sub_xyz");
        result.Value.TenantId.Should().Be("t-sub");
        result.Value.ExtractedData.Should().ContainKey("metadata_campaign");
        result.Value.ExtractedData!["metadata_campaign"].Should().Be("spring");
    }

    [Fact]
    public async Task ProcessWebhookAsync_CheckoutSessionEvent_ExtractsCheckoutSessionResource()
    {
        // Arrange
        var payload = WrapEvent(
            "evt_cs_1",
            "checkout.session.completed",
            objectJson: """
                {
                  "id": "cs_qq",
                  "object": "checkout.session",
                  "metadata": { "tenant_id": "t-cs" }
                }
                """);
        var handler = CreateHandler(SigningSecret);

        // Act
        var result = await handler.ProcessWebhookAsync(payload, SignPayload(SigningSecret, payload));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("checkout_session");
        result.Value.ResourceId.Should().Be("cs_qq");
        result.Value.TenantId.Should().Be("t-cs");
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvoiceEvent_ExtractsInvoiceResource()
    {
        // Arrange
        var payload = WrapEvent(
            "evt_inv_1",
            "invoice.paid",
            objectJson: """
                {
                  "id": "in_abc",
                  "object": "invoice",
                  "metadata": { "tenant_id": "t-inv" }
                }
                """);
        var handler = CreateHandler(SigningSecret);

        // Act
        var result = await handler.ProcessWebhookAsync(payload, SignPayload(SigningSecret, payload));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("invoice");
        result.Value.ResourceId.Should().Be("in_abc");
        result.Value.TenantId.Should().Be("t-inv");
    }

    [Fact]
    public async Task ProcessWebhookAsync_PaymentIntentEvent_FallsBackToIHasIdHasObjectExtractor()
    {
        // Arrange — payment_intent isn't one of the explicitly handled types, but
        // the SDK still deserialises into a class that implements IHasId+IHasObject,
        // so the resource type is taken from the "object" field.
        var payload = WrapEvent(
            "evt_pi_1",
            "payment_intent.succeeded",
            objectJson: """
                {
                  "id": "pi_abc",
                  "object": "payment_intent"
                }
                """);
        var handler = CreateHandler(SigningSecret);

        // Act
        var result = await handler.ProcessWebhookAsync(payload, SignPayload(SigningSecret, payload));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("payment_intent");
        result.Value.ResourceId.Should().Be("pi_abc");
        result.Value.TenantId.Should().BeNull();
        result.Value.ExtractedData.Should().BeNull();
    }

    [Fact]
    public async Task ProcessWebhookAsync_NoMetadataOnCustomer_ReturnsNullExtractedData()
    {
        // Arrange — customer event without metadata at all.
        var payload = WrapEvent(
            "evt_cus_no_meta",
            "customer.created",
            objectJson: """
                {
                  "id": "cus_no_meta",
                  "object": "customer"
                }
                """);
        var handler = CreateHandler(SigningSecret);

        // Act
        var result = await handler.ProcessWebhookAsync(payload, SignPayload(SigningSecret, payload));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("customer");
        result.Value.ExtractedData.Should().BeNull();
        result.Value.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task ProcessWebhookAsync_DevModeMissingSignatureSkipsValidationButStillExtractsResource()
    {
        // Arrange — dev mode (no signing secret), customer event.
        var handler = CreateHandler(string.Empty);
        var payload = WrapEvent(
            "evt_dev",
            "customer.created",
            objectJson: """
                {
                  "id": "cus_dev",
                  "object": "customer",
                  "metadata": { "tenant_id": "t-dev" }
                }
                """);

        // Act — passing a bogus signature should be ignored in dev mode.
        var result = await handler.ProcessWebhookAsync(payload, "ignored_in_dev");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("customer");
        result.Value.ResourceId.Should().Be("cus_dev");
        result.Value.TenantId.Should().Be("t-dev");
    }

    [Fact]
    public async Task ProcessWebhookAsync_NullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler(SigningSecret);

        // Act
        var act = () => handler.ProcessWebhookAsync(null!, "t=0,v1=00");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessWebhookAsync_NullSignature_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler(SigningSecret);

        // Act
        var act = () => handler.ProcessWebhookAsync("{}", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var handlerType = HandlerType();

        // Act
        var act = () => Activator.CreateInstance(
            handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null, NullLoggerInstance(handlerType) },
            culture: null);

        // Assert
        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var handlerType = HandlerType();
        var options = Options.Create(new StripeOptions { WebhookSigningSecret = "x" });

        // Act
        var act = () => Activator.CreateInstance(
            handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { options, null },
            culture: null);

        // Assert
        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>();
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static IPaymentWebhookHandler CreateHandler(string signingSecret)
    {
        var handlerType = HandlerType();
        var options = Options.Create(new StripeOptions { WebhookSigningSecret = signingSecret });
        var logger = NullLoggerInstance(handlerType);

        var instance = Activator.CreateInstance(
            handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { options, logger },
            culture: null)!;

        return (IPaymentWebhookHandler)instance;
    }

    private static Type HandlerType() => typeof(StripeOptions).Assembly
        .GetType("Compendium.Adapters.Stripe.Webhooks.StripeWebhookHandler", throwOnError: true)!;

    private static object NullLoggerInstance(Type handlerType) =>
        Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(handlerType))!;

    private static string WrapEvent(string eventId, string eventType, string objectJson) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2024-06-20",
          "type": "{{eventType}}",
          "data": { "object": {{objectJson}} }
        }
        """;

    private static string SignPayload(string secret, string payload, DateTimeOffset? timestamp = null)
    {
        var ts = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var signedPayload = $"{ts}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={ts},v1={sig}";
    }
}
