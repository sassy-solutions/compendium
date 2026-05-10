// -----------------------------------------------------------------------
// <copyright file="ListmonkHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.Tests.TestSupport;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Listmonk.Tests.Http;

/// <summary>
/// Unit tests for the internal <c>ListmonkHttpClient</c>. The production services
/// (<see cref="IEmailService"/> and <see cref="INewsletterService"/>) cover most of the
/// happy-path surface; these tests target the endpoints that are public on the HTTP
/// client itself but are not invoked by either service (lists, templates, campaigns,
/// pagination edge cases, list membership add/remove) plus the
/// <see cref="HttpRequestException"/> branch in every helper method.
/// </summary>
public class ListmonkHttpClientTests
{
    private const string BaseUrl = "https://listmonk.test/api/";

    // ============================================================================
    // Auth header & base address (constructor side-effects)
    // ============================================================================

    [Fact]
    public void Ctor_WhenUsernameAndPasswordProvided_AddsBasicAuthHeader()
    {
        // Arrange
        var http = new HttpClient(new MockHttpMessageHandler());

        // Act
        _ = ListmonkReflectionHelpers.CreateHttpClient(
            new MockHttpMessageHandler(),
            new ListmonkOptions
            {
                BaseUrl = "https://listmonk.test",
                Username = "admin",
                Password = "secret"
            });

        // Assert — the handler-bound HttpClient was wrapped internally; we reach the same
        // code path by inspecting a freshly-created client with the same options through DI.
        // The reflection helper instantiates ListmonkHttpClient which mutates its HttpClient.
        // Verify by introspecting via the HttpClient passed in — we re-create one here to assert.
        var inspected = new HttpClient(new MockHttpMessageHandler());
        var optionsType = typeof(ListmonkOptions);
        var options = new ListmonkOptions
        {
            BaseUrl = "https://listmonk.test",
            Username = "admin",
            Password = "secret"
        };
        // Re-invoke ConfigureHttpClient indirectly via a new client instance.
        var client = ListmonkReflectionHelpers.CreateHttpClient(new MockHttpMessageHandler(), options);
        var httpField = client.GetType().GetField("_httpClient",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var http2 = (HttpClient)httpField.GetValue(client)!;
        http2.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        http2.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");
        var expected = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("admin:secret"));
        http2.DefaultRequestHeaders.Authorization.Parameter.Should().Be(expected);
        http2.BaseAddress.Should().Be(new Uri("https://listmonk.test/api/"));
    }

    [Fact]
    public void Ctor_WhenCredentialsMissing_DoesNotAddAuthorizationHeader()
    {
        // Arrange & Act
        var client = ListmonkReflectionHelpers.CreateHttpClient(
            new MockHttpMessageHandler(),
            new ListmonkOptions
            {
                BaseUrl = "https://listmonk.test/",
                Username = string.Empty,
                Password = string.Empty
            });

        // Assert
        var httpField = client.GetType().GetField("_httpClient",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var http = (HttpClient)httpField.GetValue(client)!;
        http.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public void Ctor_WhenHttpClientNull_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(new ListmonkOptions
        {
            BaseUrl = "https://listmonk.test"
        });
        var loggerField = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>)
            .MakeGenericType(ListmonkReflectionHelpers.HttpClientType)
            .GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        var logger = loggerField.GetValue(null)!;

        // Act
        var act = () => Activator.CreateInstance(
            ListmonkReflectionHelpers.HttpClientType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null, optionsWrapper, logger },
            culture: null);

        // Assert — the inner ArgumentNullException is wrapped by Activator.CreateInstance.
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenOptionsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var http = new HttpClient(new MockHttpMessageHandler());
        var loggerField = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>)
            .MakeGenericType(ListmonkReflectionHelpers.HttpClientType)
            .GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        var logger = loggerField.GetValue(null)!;

        // Act
        var act = () => Activator.CreateInstance(
            ListmonkReflectionHelpers.HttpClientType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { http, null, logger },
            culture: null);

        // Assert
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenLoggerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var http = new HttpClient(new MockHttpMessageHandler());
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(new ListmonkOptions
        {
            BaseUrl = "https://listmonk.test"
        });

        // Act
        var act = () => Activator.CreateInstance(
            ListmonkReflectionHelpers.HttpClientType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { http, optionsWrapper, null },
            culture: null);

        // Assert
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<ArgumentNullException>();
    }

    // ============================================================================
    // Subscriber endpoints (those not exercised through INewsletterService)
    // ============================================================================

    [Fact]
    public async Task GetSubscriberAsync_ById_OnSuccess_ReturnsSubscriber()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}subscribers/42")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":42,\"email\":\"u@example.com\",\"status\":\"enabled\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetSubscriberAsync", 42, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetSubscriberAsync_ById_WhenEmptyResponseBody_ReturnsEmptyResponseError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}subscribers/42")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetSubscriberAsync", 42, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.EmptyResponse");
    }

    [Fact]
    public async Task GetSubscriberAsync_ById_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{BaseUrl}subscribers/42")
            .Throw(new HttpRequestException("connection refused"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetSubscriberAsync", 42, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task UpdateSubscriberAsync_ById_OnSuccess_ReturnsSubscriber()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Put, $"{BaseUrl}subscribers/7")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":7,\"email\":\"a@b.c\",\"status\":\"enabled\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkUpdateSubscriberRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "UpdateSubscriberAsync", 7, request, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UpdateSubscriberAsync_WhenEmptyBody_ReturnsEmptyResponseError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Put, $"{BaseUrl}subscribers/7")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":null}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkUpdateSubscriberRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "UpdateSubscriberAsync", 7, request, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.EmptyResponse");
    }

    [Fact]
    public async Task UpdateSubscriberAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{BaseUrl}subscribers/7")
            .Throw(new HttpRequestException("net down"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkUpdateSubscriberRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "UpdateSubscriberAsync", 7, request, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task DeleteSubscriberAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Delete, $"{BaseUrl}subscribers/7")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "DeleteSubscriberAsync", 7, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DeleteSubscriberAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{BaseUrl}subscribers/7")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"missing\"}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "DeleteSubscriberAsync", 7, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.NotFound");
    }

    [Fact]
    public async Task DeleteSubscriberAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{BaseUrl}subscribers/7")
            .Throw(new HttpRequestException("dns fail"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "DeleteSubscriberAsync", 7, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task ListSubscribersAsync_WithoutQuery_BuildsBasicUrl()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}subscribers?page=1&per_page=50")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[],\"total\":0,\"page\":1,\"per_page\":50}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "ListSubscribersAsync", 1, 50, null, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListSubscribersAsync_WithQuery_AppendsUrlEncodedQuery()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}subscribers")
            .WithQueryString("query", "name='alice'")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[],\"total\":0,\"page\":1,\"per_page\":50}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "ListSubscribersAsync", 1, 50, "name='alice'", CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListSubscribersAsync_WhenPaginatedDataMissing_ReturnsEmptyContainer()
    {
        // Arrange — wrapper is null, helper returns a fresh paginated container.
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}subscribers?page=1&per_page=50")
            .Respond(HttpStatusCode.OK, "application/json", "null");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "ListSubscribersAsync", 1, 50, null, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
    }

    [Fact]
    public async Task ListSubscribersAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{BaseUrl}subscribers*")
            .Throw(new HttpRequestException("net down"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "ListSubscribersAsync", 1, 50, null, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task AddSubscriberToListsAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Put, $"{BaseUrl}subscribers/lists")
            .WithPartialContent("\"action\":\"add\"")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client,
            "AddSubscriberToListsAsync",
            5,
            new List<int> { 1, 2 },
            CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AddSubscriberToListsAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{BaseUrl}subscribers/lists")
            .Throw(new HttpRequestException("net down"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client,
            "AddSubscriberToListsAsync",
            5,
            new List<int> { 1 },
            CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task RemoveSubscriberFromListsAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Put, $"{BaseUrl}subscribers/lists")
            .WithPartialContent("\"action\":\"remove\"")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client,
            "RemoveSubscriberFromListsAsync",
            5,
            new List<int> { 9 },
            CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    // ============================================================================
    // List endpoints (mailing lists)
    // ============================================================================

    [Fact]
    public async Task CreateListAsync_OnSuccess_ReturnsList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, $"{BaseUrl}lists")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":1,\"name\":\"My list\",\"type\":\"public\",\"optin\":\"single\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateListRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "CreateListAsync", request, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task CreateListAsync_WhenEmptyBody_ReturnsEmptyResponseError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, $"{BaseUrl}lists")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateListRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "CreateListAsync", request, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.EmptyResponse");
    }

    [Fact]
    public async Task CreateListAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{BaseUrl}lists")
            .Throw(new HttpRequestException("offline"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateListRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "CreateListAsync", request, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task GetListAsync_OnSuccess_ReturnsList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}lists/3")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":3,\"name\":\"L\",\"type\":\"private\",\"optin\":\"double\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetListAsync", 3, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
    }

    [Fact]
    public async Task DeleteListAsync_WhenForbidden_ReturnsForbiddenError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{BaseUrl}lists/3")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"denied\"}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "DeleteListAsync", 3, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.Forbidden");
    }

    [Fact]
    public async Task DeleteListAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Delete, $"{BaseUrl}lists/3")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "DeleteListAsync", 3, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
    }

    [Fact]
    public async Task DeleteListAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{BaseUrl}lists/3")
            .Throw(new HttpRequestException("offline"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "DeleteListAsync", 3, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    // ============================================================================
    // Template endpoints
    // ============================================================================

    [Fact]
    public async Task GetTemplatesAsync_OnSuccess_ReturnsTemplateList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}templates")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":[{\"id\":1,\"name\":\"Welcome\",\"type\":\"campaign\",\"body\":\"<p>hi</p>\",\"is_default\":true}]}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetTemplatesAsync", CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
    }

    [Fact]
    public async Task GetTemplatesAsync_WhenWrapperNull_ReturnsEmptyList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}templates")
            .Respond(HttpStatusCode.OK, "application/json", "null");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetTemplatesAsync", CancellationToken.None);

        // Assert — empty list is success.
        AssertResultIsSuccess(result!);
    }

    [Fact]
    public async Task GetTemplatesAsync_WhenForbidden_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{BaseUrl}templates")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"denied\"}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetTemplatesAsync", CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.Forbidden");
    }

    [Fact]
    public async Task GetTemplatesAsync_WhenHttpRequestExceptionThrown_ReturnsHttpError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{BaseUrl}templates")
            .Throw(new HttpRequestException("dns fail"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetTemplatesAsync", CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.HttpError");
    }

    [Fact]
    public async Task GetTemplateAsync_OnSuccess_ReturnsTemplate()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}templates/9")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":9,\"name\":\"T\",\"type\":\"campaign\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetTemplateAsync", 9, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
    }

    // ============================================================================
    // Campaign endpoints
    // ============================================================================

    [Fact]
    public async Task CreateCampaignAsync_OnSuccess_ReturnsCampaign()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, $"{BaseUrl}campaigns")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":50,\"uuid\":\"c-50\",\"name\":\"Newsletter\",\"subject\":\"Hi\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateCampaignRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "CreateCampaignAsync", request, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task CreateCampaignAsync_WhenRateLimited_ReturnsRateLimitedError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{BaseUrl}campaigns")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", "{\"message\":\"slow down\"}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateCampaignRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "CreateCampaignAsync", request, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.RateLimited");
    }

    [Fact]
    public async Task CreateCampaignAsync_WhenErrorBodyNotJson_FallsBackToReadAsString()
    {
        // Arrange — return a plain text body so the JSON deserializer throws and the
        // catch branch reads the raw string instead.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{BaseUrl}campaigns")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "service unavailable text");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateCampaignRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "CreateCampaignAsync", request, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.Error");
        var error = result!.GetType().GetProperty("Error")!.GetValue(result)!;
        var message = (string)error.GetType().GetProperty("Message")!.GetValue(error)!;
        message.Should().Contain("service unavailable text");
    }

    [Fact]
    public async Task GetCampaignAsync_OnSuccess_ReturnsCampaign()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}campaigns/77")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":77,\"name\":\"C\",\"subject\":\"S\",\"from_email\":\"f@x.y\"," +
                "\"status\":\"draft\",\"type\":\"regular\",\"body\":\"b\",\"altbody\":\"a\"," +
                "\"send_at\":\"2026-01-01T00:00:00Z\",\"started_at\":\"2026-01-02T00:00:00Z\"," +
                "\"to_send\":10,\"sent\":3,\"lists\":[],\"tags\":[\"t\"],\"template_id\":1," +
                "\"created_at\":\"2026-01-01T00:00:00Z\",\"updated_at\":\"2026-01-02T00:00:00Z\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetCampaignAsync", 77, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
    }

    [Fact]
    public async Task StartCampaignAsync_OnSuccess_ReturnsRunningCampaign()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Put, $"{BaseUrl}campaigns/77/status")
            .WithPartialContent("\"status\":\"running\"")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":77,\"status\":\"running\"}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "StartCampaignAsync", 77, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task StartCampaignAsync_WhenEmptyBody_ReturnsEmptyResponseError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Put, $"{BaseUrl}campaigns/77/status")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":null}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "StartCampaignAsync", 77, CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.EmptyResponse");
    }

    [Fact]
    public async Task ListListsAsync_OnSuccess_ReturnsPaginatedData()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, $"{BaseUrl}lists?page=2&per_page=25")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[],\"total\":0,\"page\":2,\"per_page\":25}}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "ListListsAsync", 2, 25, CancellationToken.None);

        // Assert
        AssertResultIsSuccess(result!);
        mock.VerifyNoOutstandingExpectation();
    }

    // ============================================================================
    // GetSubscriberByEmailAsync — paginated lookup edge case (failure leg)
    // ============================================================================

    [Fact]
    public async Task GetSubscriberByEmailAsync_WhenLookupFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{BaseUrl}subscribers*")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"nope\"}");

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "GetSubscriberByEmailAsync", "x@example.com", CancellationToken.None);

        // Assert
        AssertResultIsFailure(result!, "Listmonk.Unauthorized");
    }

    // ============================================================================
    // SendTransactionalAsync — HttpRequestException branch
    // ============================================================================

    [Fact]
    public async Task SendTransactionalAsync_WhenHttpRequestExceptionThrown_ReturnsSendFailedError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{BaseUrl}tx")
            .Throw(new HttpRequestException("connection reset"));

        var client = ListmonkReflectionHelpers.CreateHttpClient(mock);
        var request = ListmonkReflectionHelpers.CreateRequest(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkTransactionalRequest");

        // Act
        var result = await ListmonkReflectionHelpers.InvokeAsync(
            client, "SendTransactionalAsync", request, CancellationToken.None);

        // Assert — SendTransactionalAsync wraps HttpRequestException in EmailErrors.SendFailed.
        AssertResultIsFailure(result!, "Email.SendFailed");
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private static void AssertResultIsSuccess(object result)
    {
        var isSuccess = (bool)result.GetType().GetProperty("IsSuccess")!.GetValue(result)!;
        isSuccess.Should().BeTrue("the Result should report success but reported failure");
    }

    private static void AssertResultIsFailure(object result, string expectedCode)
    {
        var isFailure = (bool)result.GetType().GetProperty("IsFailure")!.GetValue(result)!;
        isFailure.Should().BeTrue("the Result should report failure");
        var error = result.GetType().GetProperty("Error")!.GetValue(result)!;
        var code = (string)error.GetType().GetProperty("Code")!.GetValue(error)!;
        code.Should().Be(expectedCode);
    }
}
