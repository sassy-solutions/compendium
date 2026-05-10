// -----------------------------------------------------------------------
// <copyright file="StripeBillingServiceTests.cs" company="Sassy Solutions">
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
using Microsoft.Extensions.Options;
using Stripe;

namespace Compendium.Adapters.Stripe.Tests.Services;

/// <summary>
/// Unit tests for the internal <c>StripeBillingService</c>. They drive the
/// service through its <see cref="IBillingService"/> contract while replacing
/// the global <see cref="StripeConfiguration.StripeClient"/> with a stub
/// <see cref="StubStripeHttpClient"/> so HTTP calls are intercepted without
/// hitting the real Stripe API.
/// </summary>
[Collection(StripeGlobalStateCollection.Name)]
public sealed class StripeBillingServiceTests : IDisposable
{
    private readonly IStripeClient? _previousClient = StripeConfiguration.StripeClient;
    private readonly StubStripeHttpClient _http = new();

    public StripeBillingServiceTests()
    {
        // Arrange — install the stub IHttpClient on the shared StripeConfiguration
        // singleton; every SDK service constructed during this test instance routes
        // through it.
        StripeConfiguration.StripeClient = new StripeClient("sk_test_unit", httpClient: _http);
    }

    public void Dispose()
    {
        StripeConfiguration.StripeClient = _previousClient;
    }

    // ---------------------------------------------------------------------
    // CreateCheckoutSessionAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateCheckoutSessionAsync_ValidRequest_ReturnsMappedSession()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/checkout/sessions",
            CheckoutSessionJson("cs_test_1", "price_abc"));

        var request = new CreateCheckoutRequest
        {
            VariantId = "price_abc",
            SuccessUrl = "https://example.com/ok",
            CancelUrl = "https://example.com/cancel",
            Email = "buyer@example.com",
            UserId = "u-1",
            CustomData = new Dictionary<string, object> { ["plan"] = "pro" }
        };

        // Act
        var result = await sut.CreateCheckoutSessionAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cs_test_1");
        result.Value.VariantId.Should().Be("price_abc");
        result.Value.CheckoutUrl.Should().Be("https://stripe.test/co/cs_test_1");

        var body = _http.Recorded[0].Content!;
        body.Should().Contain("mode=subscription");
        body.Should().Contain("line_items[0][price]=price_abc");
        body.Should().Contain("customer_email=buyer%40example.com");
        body.Should().Contain("metadata[user_id]=u-1");
        body.Should().Contain("metadata[plan]=pro");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WithDiscountCode_AppendsPromotionCodeToBody()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/checkout/sessions",
            CheckoutSessionJson("cs_test_2", "price_xyz"));

        var request = new CreateCheckoutRequest
        {
            VariantId = "price_xyz",
            SuccessUrl = "https://e.com/s",
            CancelUrl = "https://e.com/c",
            Email = "x@e.com",
            DiscountCode = "promo_50"
        };

        // Act
        var result = await sut.CreateCheckoutSessionAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _http.Recorded[0].Content!.Should().Contain("discounts[0][promotion_code]=promo_50");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_StripeFailure_ReturnsCheckoutFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/checkout/sessions",
            ErrorJson("invalid_request_error", "Bad price."),
            System.Net.HttpStatusCode.BadRequest);

        // Act
        var result = await sut.CreateCheckoutSessionAsync(
            new CreateCheckoutRequest
            {
                VariantId = "price_bad",
                SuccessUrl = "https://e.com/s",
                CancelUrl = "https://e.com/c",
                Email = "x@e.com"
            },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.CheckoutFailed");
        result.Error.Message.Should().Contain("Bad price.");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.CreateCheckoutSessionAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ---------------------------------------------------------------------
    // GetCustomerAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetCustomerAsync_ExistingCustomer_ReturnsMappedBillingCustomer()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers/cus_123",
            CustomerJson("cus_123", "alice@example.com", name: "Alice"));

        // Act
        var result = await sut.GetCustomerAsync("cus_123", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cus_123");
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetCustomerAsync_NotFound_ReturnsCustomerNotFoundError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers/cus_missing",
            ErrorJson("invalid_request_error", "No such customer.", "resource_missing"),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.GetCustomerAsync("cus_missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.CustomerNotFound");
    }

    [Fact]
    public async Task GetCustomerAsync_StripeFailure_ReturnsGetCustomerFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers/cus_err",
            ErrorJson("api_error", "Boom"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.GetCustomerAsync("cus_err", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.GetCustomerFailed");
        result.Error.Message.Should().Contain("Boom");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCustomerAsync_WhitespaceCustomerId_ThrowsArgumentException(string customerId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.GetCustomerAsync(customerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // GetCustomerByEmailAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetCustomerByEmailAsync_FoundOne_ReturnsMappedCustomer()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers",
            CustomerListJson(CustomerJsonFragment("cus_email", "found@example.com")));

        // Act
        var result = await sut.GetCustomerByEmailAsync("found@example.com", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cus_email");
        result.Value.Email.Should().Be("found@example.com");
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_EmptyResult_ReturnsCustomerNotFoundByEmail()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(HttpMethod.Get, "/v1/customers", CustomerListJson());

        // Act
        var result = await sut.GetCustomerByEmailAsync("absent@example.com", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.CustomerNotFoundByEmail");
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_StripeFailure_ReturnsGetCustomerByEmailFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers",
            ErrorJson("api_error", "Throttled"),
            System.Net.HttpStatusCode.TooManyRequests);

        // Act
        var result = await sut.GetCustomerByEmailAsync("rate@example.com", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.GetCustomerByEmailFailed");
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_BlankEmail_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.GetCustomerByEmailAsync("  ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // UpsertCustomerAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpsertCustomerAsync_CustomerExists_UpdatesAndReturnsMapped()
    {
        // Arrange — list returns one customer; SDK then issues an UPDATE on that id.
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers",
            CustomerListJson(CustomerJsonFragment("cus_existing", "u@example.com")));
        _http.Expect(
            HttpMethod.Post,
            "/v1/customers/cus_existing",
            CustomerJson("cus_existing", "u@example.com", name: "Updated"));

        // Act
        var result = await sut.UpsertCustomerAsync(
            new UpsertCustomerRequest
            {
                Email = "u@example.com",
                Name = "Updated",
                City = "Paris",
                Region = "IDF",
                Country = "FR",
                TenantId = "t-1",
                UserId = "u-1",
                CustomData = new Dictionary<string, object> { ["seg"] = "vip" }
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cus_existing");
        result.Value.Name.Should().Be("Updated");

        var update = _http.Recorded[1];
        update.Content!.Should().Contain("name=Updated");
        update.Content!.Should().Contain("metadata[tenant_id]=t-1");
        update.Content!.Should().Contain("metadata[seg]=vip");
        update.Content!.Should().Contain("address[city]=Paris");
    }

    [Fact]
    public async Task UpsertCustomerAsync_NoMatch_CreatesNewCustomer()
    {
        // Arrange — empty list triggers CREATE path.
        var sut = CreateSut();
        _http.Expect(HttpMethod.Get, "/v1/customers", CustomerListJson());
        _http.Expect(
            HttpMethod.Post,
            "/v1/customers",
            CustomerJson("cus_new", "new@example.com", name: "New"));

        // Act
        var result = await sut.UpsertCustomerAsync(
            new UpsertCustomerRequest { Email = "new@example.com", Name = "New" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("cus_new");
        _http.Recorded.Should().HaveCount(2);
        _http.Recorded[1].Method.Should().Be(HttpMethod.Post);
        _http.Recorded[1].Uri.AbsolutePath.Should().EndWith("/v1/customers");
    }

    [Fact]
    public async Task UpsertCustomerAsync_NoAddressFields_OmitsAddressOptions()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(HttpMethod.Get, "/v1/customers", CustomerListJson());
        _http.Expect(
            HttpMethod.Post,
            "/v1/customers",
            CustomerJson("cus_no_addr", "noaddr@example.com"));

        // Act
        var result = await sut.UpsertCustomerAsync(
            new UpsertCustomerRequest { Email = "noaddr@example.com" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _http.Recorded[1].Content!.Should().NotContain("address[");
    }

    [Fact]
    public async Task UpsertCustomerAsync_NoMetadata_OmitsMetadataParameters()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(HttpMethod.Get, "/v1/customers", CustomerListJson());
        _http.Expect(
            HttpMethod.Post,
            "/v1/customers",
            CustomerJson("cus_no_meta", "nometa@example.com"));

        // Act
        var result = await sut.UpsertCustomerAsync(
            new UpsertCustomerRequest { Email = "nometa@example.com" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _http.Recorded[1].Content!.Should().NotContain("metadata[");
    }

    [Fact]
    public async Task UpsertCustomerAsync_StripeFailure_ReturnsUpsertCustomerFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Get,
            "/v1/customers",
            ErrorJson("api_error", "boom"),
            System.Net.HttpStatusCode.BadGateway);

        // Act
        var result = await sut.UpsertCustomerAsync(
            new UpsertCustomerRequest { Email = "fail@example.com" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.UpsertCustomerFailed");
    }

    [Fact]
    public async Task UpsertCustomerAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.UpsertCustomerAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpsertCustomerAsync_BlankEmail_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.UpsertCustomerAsync(
            new UpsertCustomerRequest { Email = "  " },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // CreateCustomerPortalUrlAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_HappyPath_ReturnsUrl()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/billing_portal/sessions",
            BillingPortalSessionJson("bps_1", "https://billing.stripe.test/p/bps_1"));

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync(
            "cus_123",
            "https://example.com/back",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://billing.stripe.test/p/bps_1");
        _http.Recorded[0].Content!.Should().Contain("customer=cus_123");
        _http.Recorded[0].Content!.Should().Contain("return_url=https%3A%2F%2Fexample.com%2Fback");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_NullReturnUrl_UsesFallback()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/billing_portal/sessions",
            BillingPortalSessionJson("bps_2", "https://billing.stripe.test/p/bps_2"));

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cus_456", returnUrl: null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _http.Recorded[0].Content!.Should().Contain("return_url=https%3A%2F%2Fexample.com%2Freturn");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_NotFound_ReturnsCustomerNotFound()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/billing_portal/sessions",
            ErrorJson("invalid_request_error", "No such customer."),
            System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cus_missing", returnUrl: null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.CustomerNotFound");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_StripeFailure_ReturnsPortalSessionFailed()
    {
        // Arrange
        var sut = CreateSut();
        _http.Expect(
            HttpMethod.Post,
            "/v1/billing_portal/sessions",
            ErrorJson("api_error", "service_down"),
            System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.CreateCustomerPortalUrlAsync("cus_err", "https://e.com/back", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.Stripe.PortalSessionFailed");
    }

    [Fact]
    public async Task CreateCustomerPortalUrlAsync_BlankCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.CreateCustomerPortalUrlAsync("", "https://e.com/back", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ---------------------------------------------------------------------
    // Constructor null-guards
    // ---------------------------------------------------------------------

    [Fact]
    public void Ctor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var handlerType = ServiceType();

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
        var handlerType = ServiceType();
        var options = Options.Create(new StripeOptions { SecretKey = "sk_test_x" });

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

    private static IBillingService CreateSut()
    {
        var handlerType = ServiceType();
        var options = Options.Create(new StripeOptions { SecretKey = "sk_test_unit" });
        var logger = NullLoggerInstance(handlerType);

        var instance = Activator.CreateInstance(
            handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { (object)options, logger },
            culture: null)!;

        return (IBillingService)instance;
    }

    private static Type ServiceType() => typeof(StripeOptions).Assembly
        .GetType("Compendium.Adapters.Stripe.Services.StripeBillingService", throwOnError: true)!;

    private static object NullLoggerInstance(Type serviceType) =>
        Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(serviceType))!;

    private static string CheckoutSessionJson(string id, string priceId) => $$"""
        {
          "id": "{{id}}",
          "object": "checkout.session",
          "url": "https://stripe.test/co/{{id}}",
          "created": 1700000000,
          "expires_at": 1700003600,
          "line_items": {
            "object": "list",
            "data": [
              {
                "id": "li_1",
                "object": "item",
                "price": { "id": "{{priceId}}", "object": "price" }
              }
            ]
          },
          "metadata": {}
        }
        """;

    private static string CustomerJson(string id, string email, string? name = null) => $$"""
        {
          "id": "{{id}}",
          "object": "customer",
          "email": "{{email}}",
          "name": {{(name is null ? "null" : $"\"{name}\"")}},
          "created": 1700000000,
          "metadata": {}
        }
        """;

    private static string CustomerJsonFragment(string id, string email) => $$"""
        {
          "id": "{{id}}",
          "object": "customer",
          "email": "{{email}}",
          "created": 1700000000,
          "metadata": {}
        }
        """;

    private static string CustomerListJson(params string[] customerJsonFragments)
    {
        var data = string.Join(",", customerJsonFragments);
        return $$"""
            {
              "object": "list",
              "url": "/v1/customers",
              "has_more": false,
              "data": [{{data}}]
            }
            """;
    }

    private static string BillingPortalSessionJson(string id, string url) => $$"""
        {
          "id": "{{id}}",
          "object": "billing_portal.session",
          "url": "{{url}}",
          "customer": "cus_test"
        }
        """;

    private static string ErrorJson(string type, string message, string? code = null)
    {
        var codeFragment = code is null ? string.Empty : $",\"code\":\"{code}\"";
        return $$"""
            {
              "error": {
                "type": "{{type}}",
                "message": "{{message}}"{{codeFragment}}
              }
            }
            """;
    }
}
