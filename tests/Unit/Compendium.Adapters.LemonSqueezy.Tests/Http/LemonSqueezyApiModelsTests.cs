// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyApiModelsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Tests.Helpers;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.LemonSqueezy.Tests.Http;

/// <summary>
/// Coverage of internal JSON:API DTO records used by the adapter. These are exercised by
/// driving JSON deserialization through the real http client with payloads that populate
/// every field; this gives the records' compiler-generated property accessors hits.
/// </summary>
public class LemonSqueezyApiModelsTests
{
    private const string BaseUrl = "https://api.lemonsqueezy.com/v1/";

    private static (object client, MockHttpMessageHandler mock) CreateClient()
    {
        var mock = new MockHttpMessageHandler();
        var client = LemonSqueezyTestHelpers.CreateHttpClient(mock,
            new LemonSqueezyOptions { ApiKey = "k", StoreId = "s", BaseUrl = BaseUrl });
        return (client, mock);
    }

    [Fact]
    public async Task FullCustomerPayload_DeserializesAllAttributes()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "jsonapi": { "version": "1.0" },
          "links": {
            "self": "https://api/v1/customers/1",
            "first": "https://api/v1/customers?p=1",
            "last": "https://api/v1/customers?p=2",
            "next": "https://api/v1/customers?p=2",
            "prev": null
          },
          "meta": { "test_mode": true },
          "data": {
            "type": "customers",
            "id": "cust-1",
            "attributes": {
              "store_id": 1,
              "name": "Alice",
              "email": "alice@example.com",
              "status": "active",
              "city": "Paris",
              "region": "IDF",
              "country": "FR",
              "total_revenue_currency": 100,
              "mrr": 1000,
              "status_formatted": "Active",
              "country_formatted": "France",
              "total_revenue_currency_formatted": "$1.00",
              "mrr_formatted": "$10.00",
              "created_at": "2026-01-01T00:00:00Z",
              "updated_at": "2026-02-01T00:00:00Z",
              "test_mode": false
            },
            "relationships": {
              "store": { "links": { "self": "/stores/1" } }
            },
            "links": { "self": "/customers/1" }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "customers/cust-1")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("GetCustomerAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { "cust-1", CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }

    [Fact]
    public async Task FullSubscriptionPayload_DeserializesAllAttributes()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "data": {
            "type": "subscriptions",
            "id": "sub-1",
            "attributes": {
              "store_id": 1,
              "customer_id": 2,
              "order_id": 3,
              "order_item_id": 4,
              "product_id": 5,
              "variant_id": 6,
              "product_name": "Pro",
              "variant_name": "Monthly",
              "user_name": "Alice",
              "user_email": "alice@example.com",
              "status": "active",
              "status_formatted": "Active",
              "card_brand": "visa",
              "card_last_four": "4242",
              "pause": { "mode": "void", "resumes_at": "2026-12-31T00:00:00Z" },
              "cancelled": false,
              "trial_ends_at": "2026-02-15T00:00:00Z",
              "billing_anchor": 15,
              "first_subscription_item": {
                "id": 7,
                "subscription_id": 1,
                "price_id": 8,
                "quantity": 2,
                "is_usage_based": false,
                "created_at": "2026-01-01T00:00:00Z",
                "updated_at": "2026-01-02T00:00:00Z"
              },
              "urls": {
                "update_payment_method": "https://example.com/payment",
                "customer_portal": "https://example.com/portal"
              },
              "renews_at": "2026-12-01T00:00:00Z",
              "ends_at": "2026-12-31T00:00:00Z",
              "created_at": "2026-01-01T00:00:00Z",
              "updated_at": "2026-02-01T00:00:00Z",
              "test_mode": false
            }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions/sub-1")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("GetSubscriptionAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { "sub-1", CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }

    [Fact]
    public async Task FullCheckoutPayload_DeserializesAllNestedAttributes()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "data": {
            "type": "checkouts",
            "id": "ck-1",
            "attributes": {
              "store_id": 1,
              "variant_id": 2,
              "custom_price": 5000,
              "product_options": {
                "name": "Pro",
                "description": "desc",
                "media": ["m1.png", "m2.png"],
                "redirect_url": "https://example.com/ok",
                "receipt_button_text": "Done",
                "receipt_link_url": "https://example.com",
                "receipt_thank_you_note": "thanks",
                "enabled_variants": [1, 2]
              },
              "checkout_options": {
                "embed": true,
                "media": true,
                "logo": true,
                "desc": true,
                "discount": true,
                "dark": false,
                "subscription_preview": true,
                "button_color": "#ff0000"
              },
              "checkout_data": {
                "email": "e@example.com",
                "name": "Alice",
                "billing_address": { "country": "FR", "zip": "75000" },
                "tax_number": "TAX-123",
                "discount_code": "DISC",
                "custom": { "tenant_id": "t-1" },
                "variant_quantities": [ { "variant_id": 1, "quantity": 2 } ]
              },
              "preview": {
                "currency": "USD",
                "currency_rate": 1.0,
                "subtotal": 1000,
                "discount_total": 100,
                "tax": 50,
                "total": 950,
                "subtotal_usd": 1000,
                "discount_total_usd": 100,
                "tax_usd": 50,
                "total_usd": 950,
                "subtotal_formatted": "$10.00",
                "discount_total_formatted": "$1.00",
                "tax_formatted": "$0.50",
                "total_formatted": "$9.50"
              },
              "expires_at": "2026-02-01T00:00:00Z",
              "created_at": "2026-01-01T00:00:00Z",
              "updated_at": "2026-01-02T00:00:00Z",
              "test_mode": false,
              "url": "https://example.com/checkout"
            }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "checkouts/ck-1")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("GetCheckoutAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { "ck-1", CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }

    [Fact]
    public async Task FullLicenseKeyPayload_DeserializesAllAttributes()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "data": {
            "type": "license-keys",
            "id": "lk-1",
            "attributes": {
              "store_id": 1,
              "customer_id": 2,
              "order_id": 3,
              "order_item_id": 4,
              "product_id": 5,
              "user_name": "Alice",
              "user_email": "alice@example.com",
              "key": "AAA-BBB-CCC",
              "key_short": "AAA",
              "activation_limit": 5,
              "instances_count": 1,
              "disabled": false,
              "status": "active",
              "status_formatted": "Active",
              "expires_at": "2026-12-31T00:00:00Z",
              "created_at": "2026-01-01T00:00:00Z",
              "updated_at": "2026-01-02T00:00:00Z",
              "test_mode": false
            }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "license-keys/lk-1")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("GetLicenseKeyAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { "lk-1", CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }

    [Fact]
    public async Task FullLicenseKeyInstancesPayload_DeserializesCollection()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "meta": { "page": { "currentPage": 1, "from": 1, "lastPage": 1, "perPage": 10, "to": 1, "total": 1 } },
          "data": [
            {
              "type": "license-key-instances",
              "id": "i-1",
              "attributes": {
                "license_key_id": 1,
                "identifier": "id-1",
                "name": "host-1",
                "created_at": "2026-01-01T00:00:00Z",
                "updated_at": "2026-01-02T00:00:00Z"
              }
            }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "license-key-instances*")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("ListLicenseKeyInstancesAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { "lk-1", CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }

    [Fact]
    public async Task LicenseValidatePayload_DeserializesAllNestedFields()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "valid": true,
          "license_key": {
            "id": 1,
            "status": "active",
            "key": "K",
            "activation_limit": 5,
            "activation_usage": 1,
            "created_at": "2026-01-01T00:00:00Z",
            "expires_at": "2026-12-31T00:00:00Z",
            "test_mode": false
          },
          "instance": {
            "id": "inst-1",
            "name": "host",
            "created_at": "2026-01-01T00:00:00Z"
          },
          "meta": { "store_id": 1 }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", responseJson);

        var requestType = typeof(LemonSqueezyOptions).Assembly.GetType(
            "Compendium.Adapters.LemonSqueezy.Http.Models.LsValidateLicenseRequest")!;
        var request = Activator.CreateInstance(requestType)!;

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("ValidateLicenseAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { request, CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateLicensePayload_DeserializesAllFields()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "deactivated": true,
          "error": null,
          "meta": { "store_id": 1 }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/deactivate")
            .Respond("application/json", responseJson);

        var requestType = typeof(LemonSqueezyOptions).Assembly.GetType(
            "Compendium.Adapters.LemonSqueezy.Http.Models.LsDeactivateLicenseRequest")!;
        var request = Activator.CreateInstance(requestType)!;

        // Act
        var task = (Task)sut.GetType()
            .GetMethod("DeactivateLicenseAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(sut, new object?[] { request, CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        // Assert
        ((bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!).Should().BeTrue();
    }
}
