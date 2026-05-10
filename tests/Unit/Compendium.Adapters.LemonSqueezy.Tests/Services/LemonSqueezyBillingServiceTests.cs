// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyBillingServiceTests.cs" company="Sassy Solutions">
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
/// Unit tests for the internal <c>LemonSqueezyBillingService</c> exercised via the public
/// <see cref="IBillingService"/> contract. HTTP traffic is mocked with
/// <see cref="MockHttpMessageHandler"/>.
/// </summary>
public class LemonSqueezyBillingServiceTests
{
    private const string BaseUrl = "https://api.lemonsqueezy.com/v1/";

    private static LemonSqueezyOptions CreateOptions(string storeId = "store-1") => new()
    {
        ApiKey = "sk_test_api_key",
        StoreId = storeId,
        BaseUrl = BaseUrl
    };

    // ============================================================================
    // CreateCheckoutSessionAsync
    // ============================================================================

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenSucceeds_ReturnsMappedSession()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "data": {
            "type": "checkouts",
            "id": "ck_123",
            "attributes": {
              "store_id": 42,
              "variant_id": 100,
              "url": "https://store.lemonsqueezy.com/checkout/ck_123",
              "created_at": "2026-01-01T10:00:00Z",
              "expires_at": "2026-01-02T10:00:00Z"
            }
          }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "checkouts")
            .Respond("application/vnd.api+json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());
        var request = new CreateCheckoutRequest { VariantId = "100", Email = "test@example.com" };

        // Act
        var result = await sut.CreateCheckoutSessionAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("ck_123");
        result.Value.CheckoutUrl.Should().Be("https://store.lemonsqueezy.com/checkout/ck_123");
        result.Value.StoreId.Should().Be("42");
        result.Value.VariantId.Should().Be("100");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenCustomDataProvided_ForwardsItToCheckout()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "data": {
            "type": "checkouts",
            "id": "ck_with_custom",
            "attributes": {
              "url": "https://example.com/checkout",
              "checkout_data": { "custom": { "tenant_id": "t-1" } }
            }
          }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "checkouts")
            .Respond("application/vnd.api+json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());
        var customData = new Dictionary<string, object> { ["tenant_id"] = "t-1" };
        var request = new CreateCheckoutRequest
        {
            VariantId = "200",
            Embed = true,
            DiscountCode = "DISC10",
            Name = "John",
            SuccessUrl = "https://example.com/success",
            CustomData = customData
        };

        // Act
        var result = await sut.CreateCheckoutSessionAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomData.Should().NotBeNull();
        result.Value.CustomData!.Should().ContainKey("tenant_id");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenApiReturnsBadRequest_ReturnsValidationError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "checkouts")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "Invalid variant");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCheckoutSessionAsync(
            new CreateCheckoutRequest { VariantId = "bad" }, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.BadRequest");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenApiReturnsEmptyBody_ReturnsEmptyResponseError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "checkouts")
            .Respond("application/vnd.api+json", "{\"data\":null}");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCheckoutSessionAsync(
            new CreateCheckoutRequest { VariantId = "1" }, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.EmptyResponse");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenHttpFails_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "checkouts")
            .Throw(new HttpRequestException("connection refused"));

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCheckoutSessionAsync(
            new CreateCheckoutRequest { VariantId = "1" }, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.HttpError");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenRequestNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var act = async () => await sut.CreateCheckoutSessionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // GetCustomerAsync
    // ============================================================================

    [Fact]
    public async Task GetCustomerAsync_WhenFound_ReturnsMappedCustomer()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "data": {
            "type": "customers",
            "id": "cust-77",
            "attributes": {
              "store_id": 42,
              "name": "Alice",
              "email": "alice@example.com",
              "city": "Paris",
              "region": "IDF",
              "country": "FR",
              "total_revenue_currency": 12345,
              "created_at": "2026-01-01T10:00:00Z",
              "updated_at": "2026-02-01T10:00:00Z"
            }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "customers/cust-77")
            .Respond("application/vnd.api+json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.GetCustomerAsync("cust-77", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cust-77");
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.Name.Should().Be("Alice");
        result.Value.City.Should().Be("Paris");
        result.Value.Country.Should().Be("FR");
        result.Value.TotalRevenueCents.Should().Be(12345);
    }

    [Fact]
    public async Task GetCustomerAsync_WhenNotFound_ReturnsCustomerNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "customers/missing")
            .Respond(HttpStatusCode.NotFound, "text/plain", "no customer");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.GetCustomerAsync("missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.CustomerNotFound");
    }

    [Fact]
    public async Task GetCustomerAsync_WhenApiReturnsServerError_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "customers/x")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "boom");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.GetCustomerAsync("x", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Error");
    }

    [Fact]
    public async Task GetCustomerAsync_WhenIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var act = async () => await sut.GetCustomerAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // GetCustomerByEmailAsync
    // ============================================================================

    [Fact]
    public async Task GetCustomerByEmailAsync_WhenFound_ReturnsFirstMatch()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "data": [
            {
              "type": "customers",
              "id": "cust-1",
              "attributes": {
                "email": "found@example.com",
                "name": "Found"
              }
            }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "customers*")
            .Respond("application/vnd.api+json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.GetCustomerByEmailAsync("found@example.com", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cust-1");
        result.Value.Email.Should().Be("found@example.com");
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_WhenNoMatches_ReturnsNotFoundByEmail()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "customers*")
            .Respond("application/vnd.api+json", "{\"data\":[]}");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.GetCustomerByEmailAsync("ghost@example.com", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.CustomerNotFoundByEmail");
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_WhenApiFails_ReturnsError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "customers*")
            .Respond(HttpStatusCode.Unauthorized, "text/plain", "no auth");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.GetCustomerByEmailAsync("e@example.com", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Unauthorized");
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_WhenEmailNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var act = async () => await sut.GetCustomerByEmailAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // UpsertCustomerAsync
    // ============================================================================

    [Fact]
    public async Task UpsertCustomerAsync_AlwaysReturnsUnsupportedOperation()
    {
        // Arrange — LemonSqueezy creates customers on checkout, not directly
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.UpsertCustomerAsync(
            new UpsertCustomerRequest { Email = "x@example.com" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.UnsupportedOperation");
    }

    [Fact]
    public async Task UpsertCustomerAsync_WhenRequestNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var act = async () => await sut.UpsertCustomerAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // CreateCustomerPortalUrlAsync
    // ============================================================================

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_WhenSubscriptionWithPortalUrlExists_ReturnsUrl()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "data": [
            {
              "type": "subscriptions",
              "id": "sub-1",
              "attributes": {
                "urls": { "customer_portal": "https://portal.lemonsqueezy.com/sub-1" }
              }
            }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cust-1", returnUrl: null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://portal.lemonsqueezy.com/sub-1");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_WhenNoActiveSubscription_ReturnsNoActiveSubscription()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", "{\"data\":[]}");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cust-empty", returnUrl: null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.NoActiveSubscription");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_WhenSubscriptionMissingPortalUrl_ReturnsNoActiveSubscription()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "data": [
            {
              "type": "subscriptions",
              "id": "sub-x",
              "attributes": { "status": "active" }
            }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cust-1", returnUrl: null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.NoActiveSubscription");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_WhenApiFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond(HttpStatusCode.Forbidden, "text/plain", "no");

        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cust-1", returnUrl: null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Forbidden");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_WhenCustomerIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateBillingService(mock, CreateOptions());

        // Act
        var act = async () => await sut.CreateCustomerPortalUrlAsync(null!, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
