// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Reflection;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Tests.Helpers;
using Compendium.Core.Results;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.LemonSqueezy.Tests.Http;

/// <summary>
/// Unit tests for the internal <c>LemonSqueezyHttpClient</c>. Reflection is used to invoke
/// methods that are not exposed through the public services (e.g. GetCheckoutAsync, license
/// key endpoints) and to exercise the HttpRequestException error paths in each helper.
/// </summary>
public class LemonSqueezyHttpClientTests
{
    private const string BaseUrl = "https://api.lemonsqueezy.com/v1/";

    private static LemonSqueezyOptions CreateOptions(string storeId = "store-1") => new()
    {
        ApiKey = "sk_test_http_client",
        StoreId = storeId,
        BaseUrl = BaseUrl
    };

    private static (object client, MockHttpMessageHandler mock) CreateClient(
        LemonSqueezyOptions? options = null)
    {
        var mock = new MockHttpMessageHandler();
        var client = LemonSqueezyTestHelpers.CreateHttpClient(mock, options ?? CreateOptions());
        return (client, mock);
    }

    private static async Task<object> InvokeAsync(object instance, string methodName, params object?[] args)
    {
        var method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)method.Invoke(instance, args)!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static bool GetIsSuccess(object result) =>
        (bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!;

    private static bool GetIsFailure(object result) =>
        (bool)result.GetType().GetProperty("IsFailure")!.GetValue(result)!;

    private static Error GetError(object result) =>
        (Error)result.GetType().GetProperty("Error")!.GetValue(result)!;

    // ============================================================================
    // GetCheckoutAsync — exposed only on the http client, not the service
    // ============================================================================

    [Fact]
    public async Task GetCheckoutAsync_WhenFound_ReturnsResource()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "data": {
            "type": "checkouts",
            "id": "ck-1",
            "attributes": { "url": "https://store.lemonsqueezy.com/checkout/ck-1" }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "checkouts/ck-1")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var result = await InvokeAsync(sut, "GetCheckoutAsync", "ck-1", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    [Fact]
    public async Task GetCheckoutAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "checkouts/missing")
            .Respond(HttpStatusCode.NotFound, "text/plain", "missing");

        // Act
        var result = await InvokeAsync(sut, "GetCheckoutAsync", "missing", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.NotFound");
    }

    // ============================================================================
    // GetLicenseKeyAsync / ListLicenseKeyInstancesAsync — only on the http client
    // ============================================================================

    [Fact]
    public async Task GetLicenseKeyAsync_WhenFound_ReturnsResource()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "data": {
            "type": "license-keys",
            "id": "lk-1",
            "attributes": { "key": "AAA-BBB", "status": "active" }
          }
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "license-keys/lk-1")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var result = await InvokeAsync(sut, "GetLicenseKeyAsync", "lk-1", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    [Fact]
    public async Task ListLicenseKeyInstancesAsync_WhenInstancesExist_ReturnsCollection()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = """
        {
          "data": [
            { "type": "license-key-instances", "id": "i-1", "attributes": { "name": "host-1" } },
            { "type": "license-key-instances", "id": "i-2", "attributes": { "name": "host-2" } }
          ]
        }
        """;
        mock.When(HttpMethod.Get, BaseUrl + "license-key-instances*")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var result = await InvokeAsync(sut, "ListLicenseKeyInstancesAsync", "lk-1", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    // ============================================================================
    // ListCustomersAsync — without and with email filter
    // ============================================================================

    [Fact]
    public async Task ListCustomersAsync_WhenEmailNull_RequestsCustomersWithoutFilter()
    {
        // Arrange — no filter[email] in URL because email argument is null
        var (sut, mock) = CreateClient();
        var responseJson = "{\"data\":[]}";
        mock.When(HttpMethod.Get, BaseUrl + "customers")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var result = await InvokeAsync(sut, "ListCustomersAsync", (string?)null, CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    [Fact]
    public async Task ListCustomersAsync_WhenEmailProvided_AppendsFilterToUrl()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var responseJson = "{\"data\":[]}";
        mock.When(HttpMethod.Get, BaseUrl + "customers?filter%5Bemail%5D=alice%40example.com")
            .Respond("application/vnd.api+json", responseJson);

        // Act
        var result = await InvokeAsync(sut, "ListCustomersAsync", "alice@example.com", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    // ============================================================================
    // ListSubscriptionsAsync filter combinations
    // ============================================================================

    [Fact]
    public async Task ListSubscriptionsAsync_WhenAllFiltersNull_RequestsBaseUrlWithStoreId()
    {
        // Arrange — store id always added; the call should still succeed
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions*")
            .Respond("application/vnd.api+json", "{\"data\":[]}");

        // Act
        var result = await InvokeAsync(
            sut, "ListSubscriptionsAsync", (string?)null, (string?)null, CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    [Fact]
    public async Task ListSubscriptionsAsync_WithoutStoreId_DoesNotAddStoreFilter()
    {
        // Arrange — empty StoreId means the store-id branch is skipped
        var (sut, mock) = CreateClient(new LemonSqueezyOptions { ApiKey = "k", StoreId = string.Empty, BaseUrl = BaseUrl });
        mock.When(HttpMethod.Get, BaseUrl + "subscriptions")
            .Respond("application/vnd.api+json", "{\"data\":[]}");

        // Act
        var result = await InvokeAsync(
            sut, "ListSubscriptionsAsync", (string?)null, (string?)null, CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    // ============================================================================
    // Error mapping coverage — the exhaustive switch in HandleErrorResponseAsync
    // ============================================================================

    [Fact]
    public async Task GetCustomerAsync_OnTooManyRequests_ReturnsRateLimitExceeded()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "customers/x")
            .Respond(HttpStatusCode.TooManyRequests, "text/plain", "rate");

        // Act
        var result = await InvokeAsync(sut, "GetCustomerAsync", "x", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("Billing.RateLimitExceeded");
    }

    [Fact]
    public async Task GetCustomerAsync_OnServiceUnavailable_ReturnsProviderUnavailable()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "customers/x")
            .Respond(HttpStatusCode.ServiceUnavailable, "text/plain", "down");

        // Act
        var result = await InvokeAsync(sut, "GetCustomerAsync", "x", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("Billing.ProviderUnavailable");
    }

    [Fact]
    public async Task GetCustomerAsync_OnConflict_ReturnsConflictError()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "customers/x")
            .Respond(HttpStatusCode.Conflict, "text/plain", "conflict");

        // Act
        var result = await InvokeAsync(sut, "GetCustomerAsync", "x", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.Conflict");
    }

    [Fact]
    public async Task GetCustomerAsync_OnEmptyErrorBody_FallsBackToReasonPhrase()
    {
        // Arrange — empty body forces HandleErrorResponseAsync to use ReasonPhrase
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "customers/x")
            .Respond(req =>
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    ReasonPhrase = "Bad Gateway",
                    Content = new StringContent(string.Empty)
                };
                return resp;
            });

        // Act
        var result = await InvokeAsync(sut, "GetCustomerAsync", "x", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.Error");
        GetError(result).Message.Should().NotBeNullOrEmpty();
    }

    // ============================================================================
    // HttpRequestException paths — every helper has its own catch block
    // ============================================================================

    [Fact]
    public async Task GetCollectionAsync_OnHttpRequestException_ReturnsHttpError()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Get, BaseUrl + "customers*")
            .Throw(new HttpRequestException("connection lost"));

        // Act
        var result = await InvokeAsync(sut, "ListCustomersAsync", "alice@example.com", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.HttpError");
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_OnHttpRequestException_ReturnsHttpError()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Delete, BaseUrl + "subscriptions/sub-x")
            .Throw(new HttpRequestException("network error"));

        // Act
        var result = await InvokeAsync(sut, "DeleteSubscriptionAsync", "sub-x", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.HttpError");
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_OnHttpRequestException_ReturnsHttpError()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Patch, BaseUrl + "subscriptions/sub-x")
            .Throw(new HttpRequestException("patch failed"));

        var requestType = typeof(LemonSqueezyOptions).Assembly.GetType(
            "Compendium.Adapters.LemonSqueezy.Http.Models.LsUpdateSubscriptionRequest")!;
        var request = Activator.CreateInstance(requestType)!;

        // Act
        var result = await InvokeAsync(sut, "UpdateSubscriptionAsync", "sub-x", request, CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.HttpError");
    }

    [Fact]
    public async Task ValidateLicenseAsync_OnHttpRequestException_ReturnsHttpError()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Throw(new HttpRequestException("fail"));

        var requestType = typeof(LemonSqueezyOptions).Assembly.GetType(
            "Compendium.Adapters.LemonSqueezy.Http.Models.LsValidateLicenseRequest")!;
        var request = Activator.CreateInstance(requestType)!;

        // Act
        var result = await InvokeAsync(sut, "ValidateLicenseAsync", request, CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.HttpError");
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_OnEmptyBodyError_FallsBackToReasonPhrase()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Delete, BaseUrl + "subscriptions/x")
            .Respond(req => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Server Error",
                Content = new StringContent(string.Empty)
            });

        // Act
        var result = await InvokeAsync(sut, "DeleteSubscriptionAsync", "x", CancellationToken.None);

        // Assert
        GetIsFailure(result).Should().BeTrue();
        GetError(result).Code.Should().Be("LemonSqueezy.Error");
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        mock.When(HttpMethod.Delete, BaseUrl + "subscriptions/sub-ok")
            .Respond(HttpStatusCode.NoContent);

        // Act
        var result = await InvokeAsync(sut, "DeleteSubscriptionAsync", "sub-ok", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    // ============================================================================
    // Authorization header & Accept header
    // ============================================================================

    [Fact]
    public async Task HttpClient_SendsBearerAuthorizationHeaderWhenApiKeySet()
    {
        // Arrange
        var (sut, mock) = CreateClient();
        var captured = mock.When(HttpMethod.Get, BaseUrl + "customers/cust-1")
            .Respond(req =>
            {
                req.Headers.Authorization.Should().NotBeNull();
                req.Headers.Authorization!.Scheme.Should().Be("Bearer");
                req.Headers.Authorization.Parameter.Should().Be("sk_test_http_client");
                req.Headers.Accept.ToString().Should().Contain("application/vnd.api+json");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"data\":{\"id\":\"cust-1\",\"type\":\"customers\",\"attributes\":{\"email\":\"e\"}}}",
                        System.Text.Encoding.UTF8,
                        "application/vnd.api+json")
                };
            });

        // Act
        var result = await InvokeAsync(sut, "GetCustomerAsync", "cust-1", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }

    [Fact]
    public async Task HttpClient_WhenApiKeyEmpty_DoesNotSendAuthorizationHeader()
    {
        // Arrange — exercises the !IsNullOrEmpty branch in ConfigureHttpClient
        var options = new LemonSqueezyOptions { ApiKey = string.Empty, StoreId = "s", BaseUrl = BaseUrl };
        var (sut, mock) = CreateClient(options);
        mock.When(HttpMethod.Get, BaseUrl + "customers/x")
            .Respond(req =>
            {
                req.Headers.Authorization.Should().BeNull();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"data\":{\"id\":\"x\",\"type\":\"customers\",\"attributes\":{\"email\":\"e\"}}}",
                        System.Text.Encoding.UTF8,
                        "application/vnd.api+json")
                };
            });

        // Act
        var result = await InvokeAsync(sut, "GetCustomerAsync", "x", CancellationToken.None);

        // Assert
        GetIsSuccess(result).Should().BeTrue();
    }
}
