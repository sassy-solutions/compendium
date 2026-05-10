// -----------------------------------------------------------------------
// <copyright file="StripeSubscriptionServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Billing;
using Compendium.Abstractions.Billing.Models;
using Compendium.Adapters.Stripe.Configuration;
using Compendium.Adapters.Stripe.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;

namespace Compendium.Adapters.Stripe.Tests.Services;

/// <summary>
/// Unit tests for the internal <c>StripeSubscriptionService</c>.
/// </summary>
[Collection(StripeGlobalStateCollection.Name)]
public sealed class StripeSubscriptionServiceTests : IDisposable
{
    private readonly IStripeClient? _previousClient = StripeConfiguration.StripeClient;
    private readonly StubStripeHttpClient _http = new();

    public StripeSubscriptionServiceTests()
    {
        // Arrange — install the stub IHttpClient on the shared singleton.
        StripeConfiguration.StripeClient = new StripeClient("sk_test_unit", httpClient: _http);
    }

    public void Dispose()
    {
        StripeConfiguration.StripeClient = _previousClient;
    }

    // ---------------------------------------------------------------------
    // GetSubscriptionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetSubscriptionAsync_Existing_ReturnsMappedSubscription()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_1",
            SubscriptionJson("sub_1", "cus_1", "price_1", "active"));

        // Act
        var result = await sut.GetSubscriptionAsync("sub_1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("sub_1");
        result.Value.CustomerId.Should().Be("cus_1");
        result.Value.VariantId.Should().Be("price_1");
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetSubscriptionAsync_NotFound_ReturnsSubscriptionNotFoundError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_missing",
            ErrorJson("invalid_request_error", "No such subscription."),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.GetSubscriptionAsync("sub_missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task GetSubscriptionAsync_StripeFailure_ReturnsGetSubscriptionFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_err",
            ErrorJson("api_error", "boom"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.GetSubscriptionAsync("sub_err", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.GetSubscriptionFailed");
    }

    [Fact]
    public async Task GetSubscriptionAsync_BlankId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.GetSubscriptionAsync(" ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // GetActiveSubscriptionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetActiveSubscriptionAsync_FoundOne_ReturnsMappedSubscription()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions",
            SubscriptionListJson(SubscriptionJson("sub_active", "cus_a", "price_a", "active")));

        // Act
        var result = await sut.GetActiveSubscriptionAsync("cus_a", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("sub_active");
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_NoneFound_ReturnsSuccessWithNull()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(HttpMethod.Get, "/v1/subscriptions", SubscriptionListJson());

        // Act
        var result = await sut.GetActiveSubscriptionAsync("cus_no_sub", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_StripeFailure_ReturnsActiveSubscriptionFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions",
            ErrorJson("api_error", "rate_limited"),
            System.Net.HttpStatusCode.TooManyRequests);

        // Act
        var result = await sut.GetActiveSubscriptionAsync("cus_x", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.GetActiveSubscriptionFailed");
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_BlankCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.GetActiveSubscriptionAsync("", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // ListSubscriptionsAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ListSubscriptionsAsync_MultipleResults_MapsAll()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions",
            SubscriptionListJson(
                SubscriptionJson("sub_a", "cus_x", "price_a", "active"),
                SubscriptionJson("sub_b", "cus_x", "price_b", "trialing")));

        // Act
        var result = await sut.ListSubscriptionsAsync("cus_x", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be("sub_a");
        result.Value[1].Status.Should().Be(BillingSubscriptionStatus.OnTrial);
    }

    [Fact]
    public async Task ListSubscriptionsAsync_Empty_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(HttpMethod.Get, "/v1/subscriptions", SubscriptionListJson());

        // Act
        var result = await sut.ListSubscriptionsAsync("cus_empty", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListSubscriptionsAsync_StripeFailure_ReturnsListSubscriptionsFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions",
            ErrorJson("api_error", "down"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.ListSubscriptionsAsync("cus_x", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.ListSubscriptionsFailed");
    }

    [Fact]
    public async Task ListSubscriptionsAsync_BlankCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.ListSubscriptionsAsync("", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // CancelSubscriptionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CancelSubscriptionAsync_ExistingSubscription_ReturnsSuccess()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Delete,
            "/v1/subscriptions/sub_cancel",
            SubscriptionJson("sub_cancel", "cus_y", "price_y", "canceled"));

        // Act
        var result = await sut.CancelSubscriptionAsync("sub_cancel", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_NotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Delete,
            "/v1/subscriptions/sub_missing",
            ErrorJson("invalid_request_error", "no_such_subscription"),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.CancelSubscriptionAsync("sub_missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_StripeFailure_ReturnsCancelSubscriptionFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Delete,
            "/v1/subscriptions/sub_err",
            ErrorJson("api_error", "down"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.CancelSubscriptionAsync("sub_err", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.CancelSubscriptionFailed");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_BlankId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.CancelSubscriptionAsync("", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // PauseSubscriptionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task PauseSubscriptionAsync_ExistingSubscription_ReturnsSuccessAndSendsPauseCollection()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_pause",
            SubscriptionJson("sub_pause", "cus_p", "price_p", "active"));

        // Act
        var result = await sut.PauseSubscriptionAsync("sub_pause", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _http.Recorded[0].Content!.Should().Contain("pause_collection[behavior]=mark_uncollectible");
    }

    [Fact]
    public async Task PauseSubscriptionAsync_NotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_missing",
            ErrorJson("invalid_request_error", "no_such_subscription"),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.PauseSubscriptionAsync("sub_missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task PauseSubscriptionAsync_StripeFailure_ReturnsPauseSubscriptionFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_err",
            ErrorJson("api_error", "down"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.PauseSubscriptionAsync("sub_err", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.PauseSubscriptionFailed");
    }

    [Fact]
    public async Task PauseSubscriptionAsync_BlankId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.PauseSubscriptionAsync(" ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // ResumeSubscriptionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ResumeSubscriptionAsync_ExistingSubscription_ClearsPauseCollection()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_resume",
            SubscriptionJson("sub_resume", "cus_r", "price_r", "active"));

        // Act
        var result = await sut.ResumeSubscriptionAsync("sub_resume", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // The service clears pause_collection by sending an empty raw value.
        _http.Recorded[0].Content!.Should().Contain("pause_collection=");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_NotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_missing",
            ErrorJson("invalid_request_error", "no_such_subscription"),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.ResumeSubscriptionAsync("sub_missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_StripeFailure_ReturnsResumeSubscriptionFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_err",
            ErrorJson("api_error", "down"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.ResumeSubscriptionAsync("sub_err", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.ResumeSubscriptionFailed");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_BlankId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.ResumeSubscriptionAsync("", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // UpdateSubscriptionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateSubscriptionAsync_HappyPath_UpdatesItemPriceAndReturnsMapped()
    {
        // Arrange — first the SDK reads the current subscription, then issues an UPDATE.
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_up",
            SubscriptionJson("sub_up", "cus_z", "price_old", "active", subscriptionItemId: "si_first"));
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_up",
            SubscriptionJson("sub_up", "cus_z", "price_new", "active", subscriptionItemId: "si_first"));

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub_up", "price_new", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VariantId.Should().Be("price_new");
        var updateBody = _http.Recorded[1].Content!;
        updateBody.Should().Contain("items[0][id]=si_first");
        updateBody.Should().Contain("items[0][price]=price_new");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_NoItems_ReturnsUpdateSubscriptionFailed()
    {
        // Arrange — current subscription has no items at all.
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_nope",
            SubscriptionJsonNoItems("sub_nope", "cus_z", "active"));

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub_nope", "price_new", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.UpdateSubscriptionFailed");
        result.Error.Message.Should().Contain("Subscription has no items");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_NotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_missing",
            ErrorJson("invalid_request_error", "no_such_subscription"),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub_missing", "price_new", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_StripeFailureOnUpdate_ReturnsUpdateSubscriptionFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/subscriptions/sub_up_err",
            SubscriptionJson("sub_up_err", "cus_e", "price_old", "active", subscriptionItemId: "si_a"));
        _http.Expect(
            HttpMethod.Post,
            "/v1/subscriptions/sub_up_err",
            ErrorJson("api_error", "down"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub_up_err", "price_new", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.UpdateSubscriptionFailed");
    }

    [Theory]
    [InlineData("", "price_new")]
    [InlineData("sub_a", "")]
    [InlineData(" ", "price_new")]
    [InlineData("sub_a", " ")]
    public async Task UpdateSubscriptionAsync_BlankInputs_ThrowsArgumentException(string subscriptionId, string variantId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.UpdateSubscriptionAsync(subscriptionId, variantId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------

    [Fact]
    public void Ctor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceType = ServiceType();

        // Act
        var act = () => Activator.CreateInstance(
            serviceType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null },
            culture: null);

        // Assert
        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>();
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static ISubscriptionService CreateSut()
    {
        var serviceType = ServiceType();
        var logger = Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(serviceType))!;

        var instance = Activator.CreateInstance(
            serviceType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { logger },
            culture: null)!;

        return (ISubscriptionService)instance;
    }

    private static Type ServiceType() => typeof(StripeOptions).Assembly
        .GetType("Compendium.Adapters.Stripe.Services.StripeSubscriptionService", throwOnError: true)!;

    private static string SubscriptionJson(
        string id,
        string customerId,
        string priceId,
        string status,
        string subscriptionItemId = "si_default") => $$"""
        {
          "id": "{{id}}",
          "object": "subscription",
          "customer": "{{customerId}}",
          "status": "{{status}}",
          "created": 1700000000,
          "items": {
            "object": "list",
            "data": [
              {
                "id": "{{subscriptionItemId}}",
                "object": "subscription_item",
                "current_period_start": 1700000000,
                "current_period_end": 1702592000,
                "price": {
                  "id": "{{priceId}}",
                  "object": "price",
                  "product": "prod_{{priceId}}",
                  "currency": "usd",
                  "unit_amount": 1500,
                  "recurring": { "interval": "month", "interval_count": 1 }
                }
              }
            ]
          },
          "metadata": {}
        }
        """;

    private static string SubscriptionJsonNoItems(string id, string customerId, string status) => $$"""
        {
          "id": "{{id}}",
          "object": "subscription",
          "customer": "{{customerId}}",
          "status": "{{status}}",
          "created": 1700000000,
          "items": { "object": "list", "data": [] },
          "metadata": {}
        }
        """;

    private static string SubscriptionListJson(params string[] subscriptionFragments)
    {
        var data = string.Join(",", subscriptionFragments);
        return $$"""
            {
              "object": "list",
              "url": "/v1/subscriptions",
              "has_more": false,
              "data": [{{data}}]
            }
            """;
    }

    private static string ErrorJson(string type, string message) => $$"""
        {
          "error": {
            "type": "{{type}}",
            "message": "{{message}}"
          }
        }
        """;
}
