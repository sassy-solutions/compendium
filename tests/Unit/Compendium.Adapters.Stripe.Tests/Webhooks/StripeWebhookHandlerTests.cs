// -----------------------------------------------------------------------
// <copyright file="StripeWebhookHandlerTests.cs" company="Sassy Solutions">
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
/// Unit tests for <see cref="Compendium.Adapters.Stripe.Webhooks.StripeWebhookHandler"/>.
/// </summary>
public class StripeWebhookHandlerTests
{
    private const string SigningSecret = "whsec_test_secret_fixture";

    private const string SamplePayload =
        "{\"id\":\"evt_test_123\",\"object\":\"event\",\"api_version\":\"2024-06-20\","
        + "\"type\":\"customer.created\","
        + "\"data\":{\"object\":{\"id\":\"cus_test_456\",\"object\":\"customer\",\"email\":\"test@example.com\","
        + "\"metadata\":{\"tenant_id\":\"tenant-abc\"}}}}";

    [Fact]
    public async Task ProcessWebhookAsync_InvalidSignature_ReturnsInvalidSignatureError()
    {
        // Arrange
        var handler = CreateHandler(SigningSecret);

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, "t=0,v1=deadbeef");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidWebhookSignature");
    }

    [Fact]
    public async Task ProcessWebhookAsync_ValidSignature_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandler(SigningSecret);
        var timestamp = DateTimeOffset.UtcNow;
        var signatureHeader = SignPayload(SigningSecret, SamplePayload, timestamp);

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, signatureHeader);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var webhook = result.Value;
        webhook.Processed.Should().BeTrue();
        webhook.EventId.Should().Be("evt_test_123");
        webhook.EventType.Should().Be("customer.created");
        webhook.ResourceType.Should().Be("customer");
        webhook.ResourceId.Should().Be("cus_test_456");
        webhook.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public async Task ProcessWebhookAsync_EmptySigningSecret_SkipsValidation()
    {
        // Arrange — dev mode: no signing secret configured
        var handler = CreateHandler(signingSecret: string.Empty);

        // Act
        var result = await handler.ProcessWebhookAsync(SamplePayload, signature: string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("customer.created");
    }

    [Fact]
    public async Task ProcessWebhookAsync_MalformedPayloadInDevMode_ReturnsProcessingFailed()
    {
        // Arrange — dev mode, but payload is unparseable JSON
        var handler = CreateHandler(signingSecret: string.Empty);

        // Act
        var result = await handler.ProcessWebhookAsync("{not-json", signature: string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.WebhookProcessingFailed");
    }

    private static IPaymentWebhookHandler CreateHandler(string signingSecret)
    {
        var adapterAssembly = typeof(StripeOptions).Assembly;
        var handlerType = adapterAssembly.GetType(
            "Compendium.Adapters.Stripe.Webhooks.StripeWebhookHandler",
            throwOnError: true)!;

        var options = Options.Create(new StripeOptions { WebhookSigningSecret = signingSecret });
        var logger = Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(handlerType))!;

        var instance = Activator.CreateInstance(
            handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { options, logger },
            culture: null)!;

        return (IPaymentWebhookHandler)instance;
    }

    // Replicates Stripe's webhook signing algorithm:
    //   signed_payload = timestamp + "." + payload
    //   v1 = HMAC-SHA256(secret, signed_payload) as lowercase hex
    //   header = "t=<timestamp>,v1=<v1>"
    private static string SignPayload(string secret, string payload, DateTimeOffset timestamp)
    {
        var ts = timestamp.ToUnixTimeSeconds();
        var signedPayload = $"{ts}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={ts},v1={sig}";
    }
}
