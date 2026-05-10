// -----------------------------------------------------------------------
// <copyright file="LemonSqueezySubscriptionServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Abstractions.Billing;
using Compendium.Abstractions.Billing.Models;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Tests.Helpers;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.LemonSqueezy.Tests.Services;

/// <summary>
/// Unit tests for the internal <c>LemonSqueezySubscriptionService</c> exercised via the
/// public <see cref="ISubscriptionService"/> contract.
/// </summary>
public class LemonSqueezySubscriptionServiceTests
{
    private const string BaseUrl = "https://api.lemonsqueezy.com/v1/";

    private static LemonSqueezyOptions CreateOptions() => new()
    {
        ApiKey = "sk_test_subscription",
        StoreId = "store-9",
        BaseUrl = BaseUrl
    };

    private static string SubscriptionResponse(
        string id = "sub-1",
        string status = "active",
        bool? cancelled = null,
        bool paused = false) =>
        $$"""
        {
          "data": {
            "type": "subscriptions",
            "id": "{{id}}",
            "attributes": {
              "store_id": 1,
              "customer_id": 99,
              "product_id": 200,
              "variant_id": 300,
              "product_name": "Pro",
              "variant_name": "Monthly",
              "status": "{{status}}",
              "cancelled": {{(cancelled.HasValue ? cancelled.Value.ToString().ToLowerInvariant() : "null")}},
              {{(paused ? "\"pause\": { \"mode\": \"void\", \"resumes_at\": \"2026-12-31T00:00:00Z\" }," : "")}}
              "renews_at": "2026-12-01T00:00:00Z",
              "created_at": "2026-01-01T10:00:00Z",
              "updated_at": "2026-02-01T10:00:00Z"
            }
          }
        }
        """;

    // ============================================================================
    // GetSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task GetSubscriptionAsync_WhenFound_ReturnsMappedSubscription()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-1")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-1", status: "active"));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetSubscriptionAsync("sub-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("sub-1");
        result.Value.CustomerId.Should().Be("99");
        result.Value.ProductId.Should().Be("200");
        result.Value.VariantId.Should().Be("300");
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenNotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/missing")
            .Respond(HttpStatusCode.NotFound, "text/plain", "no sub");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetSubscriptionAsync("missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenServerError_PropagatesGenericError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/x")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "boom");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetSubscriptionAsync("x", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Error");
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.GetSubscriptionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // GetActiveSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task GetActiveSubscriptionAsync_WhenActiveExists_ReturnsSubscription()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var listJson = """
        {
          "data": [
            {
              "type": "subscriptions",
              "id": "sub-active",
              "attributes": {
                "customer_id": 1,
                "status": "active",
                "product_id": 2,
                "variant_id": 3,
                "created_at": "2026-01-01T00:00:00Z"
              }
            }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", listJson);

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetActiveSubscriptionAsync("1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("sub-active");
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_WhenNoActive_ReturnsSuccessWithNull()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", "{\"data\":[]}");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetActiveSubscriptionAsync("nobody", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_WhenApiFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond(HttpStatusCode.TooManyRequests, "text/plain", "rate limited");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetActiveSubscriptionAsync("1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.RateLimitExceeded");
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_WhenCustomerIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.GetActiveSubscriptionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // ListSubscriptionsAsync
    // ============================================================================

    [Fact]
    public async Task ListSubscriptionsAsync_WhenSubscriptionsExist_ReturnsMappedList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var listJson = """
        {
          "data": [
            {
              "type": "subscriptions",
              "id": "sub-1",
              "attributes": { "customer_id": 1, "status": "active", "product_id": 2, "variant_id": 3 }
            },
            {
              "type": "subscriptions",
              "id": "sub-2",
              "attributes": { "customer_id": 1, "status": "cancelled", "cancelled": true, "product_id": 2, "variant_id": 3 }
            }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", listJson);

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ListSubscriptionsAsync("1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be("sub-1");
        result.Value[1].Status.Should().Be(BillingSubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task ListSubscriptionsAsync_WhenApiFails_ReturnsError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond(HttpStatusCode.ServiceUnavailable, "text/plain", "down");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ListSubscriptionsAsync("1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.ProviderUnavailable");
    }

    [Fact]
    public async Task ListSubscriptionsAsync_WhenCustomerIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.ListSubscriptionsAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // CancelSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task CancelSubscriptionAsync_WhenActive_DeletesSuccessfully()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-1")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-1", status: "active"));
        mock.When(HttpMethod.Delete, BaseUrl + "subscriptions/sub-1")
            .Respond(HttpStatusCode.NoContent);

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.CancelSubscriptionAsync("sub-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenAlreadyCancelled_ReturnsAlreadyCanceledError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-cx")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-cx", status: "cancelled", cancelled: true));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.CancelSubscriptionAsync("sub-cx", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionAlreadyCanceled");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenNotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/ghost")
            .Respond(HttpStatusCode.NotFound, "text/plain", "missing");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.CancelSubscriptionAsync("ghost", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenGetFailsNonNotFound_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/forbidden")
            .Respond(HttpStatusCode.Forbidden, "text/plain", "denied");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.CancelSubscriptionAsync("forbidden", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Forbidden");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenDeleteFails_ReturnsErrorFromDelete()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-1")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-1", status: "active"));
        mock.When(HttpMethod.Delete, BaseUrl + "subscriptions/sub-1")
            .Respond(HttpStatusCode.UnprocessableEntity, "text/plain", "validation");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.CancelSubscriptionAsync("sub-1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.UnprocessableEntity");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.CancelSubscriptionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // PauseSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task PauseSubscriptionAsync_WhenSucceeds_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-pause")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-pause", status: "active", paused: true));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.PauseSubscriptionAsync("sub-pause", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PauseSubscriptionAsync_WhenApiFails_ReturnsError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-x")
            .Respond(HttpStatusCode.Conflict, "text/plain", "conflict");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.PauseSubscriptionAsync("sub-x", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Conflict");
    }

    [Fact]
    public async Task PauseSubscriptionAsync_WhenIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.PauseSubscriptionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // ResumeSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task ResumeSubscriptionAsync_WhenPaused_ResumesSuccessfully()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-paused")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-paused", status: "paused", paused: true));
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-paused")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-paused", status: "active"));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ResumeSubscriptionAsync("sub-paused", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_WhenNotPaused_ReturnsSubscriptionNotPaused()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-active")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-active", status: "active"));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ResumeSubscriptionAsync("sub-active", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotPaused");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_WhenNotFound_ReturnsSubscriptionNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/ghost")
            .Respond(HttpStatusCode.NotFound, "text/plain", "missing");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ResumeSubscriptionAsync("ghost", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.SubscriptionNotFound");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_WhenGetFailsNonNotFound_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/forbidden")
            .Respond(HttpStatusCode.Forbidden, "text/plain", "no");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ResumeSubscriptionAsync("forbidden", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Forbidden");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_WhenPatchFails_ReturnsError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-paused")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-paused", status: "paused", paused: true));
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-paused")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "bad request");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.ResumeSubscriptionAsync("sub-paused", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.BadRequest");
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_WhenIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.ResumeSubscriptionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // UpdateSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenValidVariant_ReturnsUpdatedSubscription()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-1")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-1", status: "active"));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub-1", "777", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("sub-1");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenVariantIdNotInteger_ReturnsVariantNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub-1", "not-numeric", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.VariantNotFound");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenApiFails_ReturnsError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-1")
            .Respond(HttpStatusCode.UnprocessableEntity, "text/plain", "bad");

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.UpdateSubscriptionAsync("sub-1", "888", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.UnprocessableEntity");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenSubscriptionIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.UpdateSubscriptionAsync(null!, "1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenVariantIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var act = async () => await sut.UpdateSubscriptionAsync("sub-1", null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // Status mapping (covers MapSubscriptionStatus switch arms)
    // ============================================================================

    [Theory]
    [InlineData("on_trial", BillingSubscriptionStatus.OnTrial)]
    [InlineData("active", BillingSubscriptionStatus.Active)]
    [InlineData("paused", BillingSubscriptionStatus.Paused)]
    [InlineData("past_due", BillingSubscriptionStatus.PastDue)]
    [InlineData("unpaid", BillingSubscriptionStatus.Unpaid)]
    [InlineData("cancelled", BillingSubscriptionStatus.Cancelled)]
    [InlineData("expired", BillingSubscriptionStatus.Expired)]
    [InlineData("unknown_value", BillingSubscriptionStatus.Active)]
    public async Task GetSubscriptionAsync_MapsLemonSqueezyStatusToBillingStatus(
        string apiStatus, BillingSubscriptionStatus expected)
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-status")
            .Respond("application/vnd.api+json", SubscriptionResponse(id: "sub-status", status: apiStatus));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetSubscriptionAsync("sub-status", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenCancelledFlagTrue_PrioritizesCancelledStatus()
    {
        // Arrange — cancelled flag should take precedence over active status
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-c")
            .Respond("application/vnd.api+json",
                SubscriptionResponse(id: "sub-c", status: "active", cancelled: true));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetSubscriptionAsync("sub-c", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Cancelled);
        result.Value.CanceledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenPaused_MapsToPausedAndPopulatesPausedAt()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-p")
            .Respond("application/vnd.api+json",
                SubscriptionResponse(id: "sub-p", status: "active", paused: true));

        var sut = LemonSqueezyTestHelpers.CreateSubscriptionService(mock, CreateOptions());

        // Act
        var result = await sut.GetSubscriptionAsync("sub-p", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Paused);
        result.Value.PausedAt.Should().NotBeNull();
        result.Value.ResumesAt.Should().NotBeNull();
    }
}
