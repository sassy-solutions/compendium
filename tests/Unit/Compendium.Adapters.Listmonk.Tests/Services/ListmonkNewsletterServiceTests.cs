// -----------------------------------------------------------------------
// <copyright file="ListmonkNewsletterServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.Tests.TestSupport;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Listmonk.Tests.Services;

/// <summary>
/// Unit tests for <c>ListmonkNewsletterService</c>, exercised through the public
/// <see cref="INewsletterService"/> interface and the production DI registration.
/// </summary>
public class ListmonkNewsletterServiceTests
{
    // ============================================================================
    // SubscribeAsync
    // ============================================================================

    [Fact]
    public async Task SubscribeAsync_WhenRequestNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.NewsletterService.SubscribeAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SubscribeAsync_OnSuccess_ReturnsMappedSubscriber()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "https://listmonk.test/api/subscribers")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":42,\"uuid\":\"u-42\",\"email\":\"alice@example.com\"," +
                "\"name\":\"Alice\",\"status\":\"enabled\"," +
                "\"lists\":[{\"id\":1,\"name\":\"News\"}]," +
                "\"attribs\":{\"city\":\"Paris\"}," +
                "\"created_at\":\"2025-01-02T03:04:05Z\",\"updated_at\":\"2025-01-03T03:04:05Z\"}}");

        using var host = new ListmonkTestHost(mock);
        var request = new SubscribeRequest
        {
            Email = "alice@example.com",
            Name = "Alice",
            ListIds = new[] { "1", "not-an-int", "2" },
            Attributes = new Dictionary<string, object> { ["city"] = "Paris" },
            RequireConfirmation = false
        };

        // Act
        var result = await host.NewsletterService.SubscribeAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("42");
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.Name.Should().Be("Alice");
        result.Value.Status.Should().Be(SubscriptionStatus.Confirmed);
        result.Value.ListIds.Should().BeEquivalentTo(new[] { "1" });
        result.Value.Attributes.Should().NotBeNull().And.ContainKey("city");
        result.Value.ConfirmedAt.Should().NotBeNull();
        result.Value.CreatedAt.Should().NotBe(DateTimeOffset.MinValue);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SubscribeAsync_WhenNoListIdsAndDefaultListConfigured_UsesDefaultListId()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "https://listmonk.test/api/subscribers")
            .WithPartialContent("\"lists\":[1]")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":1,\"email\":\"x@example.com\",\"status\":\"enabled\"}}");

        using var host = new ListmonkTestHost(mock); // DefaultListId = 1
        var request = new SubscribeRequest { Email = "x@example.com" };

        // Act
        var result = await host.NewsletterService.SubscribeAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SubscribeAsync_WhenNoListIdsAndNoDefault_OmitsListsButStillSucceeds()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "https://listmonk.test/api/subscribers")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":7,\"email\":\"x@example.com\",\"status\":\"enabled\"}}");

        var options = new ListmonkOptions
        {
            BaseUrl = "https://listmonk.test/",
            Username = "admin",
            Password = "secret",
            DefaultListId = null
        };
        using var host = new ListmonkTestHost(mock, options);
        var request = new SubscribeRequest { Email = "x@example.com" };

        // Act
        var result = await host.NewsletterService.SubscribeAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SubscribeAsync_WhenListmonkReturnsConflict_ReturnsConflictError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://listmonk.test/api/subscribers")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"message\":\"duplicate\"}");

        using var host = new ListmonkTestHost(mock);
        var request = new SubscribeRequest { Email = "dup@example.com" };

        // Act
        var result = await host.NewsletterService.SubscribeAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.Conflict");
    }

    // ============================================================================
    // GetSubscriberAsync (by email)
    // ============================================================================

    [Fact]
    public async Task GetSubscriberAsync_WhenEmailNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.NewsletterService.GetSubscriberAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetSubscriberAsync_OnSuccess_ReturnsMappedSubscriber()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":11,\"email\":\"bob@example.com\"," +
                "\"name\":\"Bob\",\"status\":\"disabled\"}],\"total\":1,\"page\":1,\"per_page\":1}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.GetSubscriberAsync("bob@example.com", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("11");
        result.Value.Status.Should().Be(SubscriptionStatus.Unsubscribed);
        result.Value.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetSubscriberAsync_WhenNotFound_ReturnsSubscriberNotFoundError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[],\"total\":0,\"page\":1,\"per_page\":1}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.GetSubscriberAsync("ghost@example.com", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.SubscriberNotFound");
        result.Error.Message.Should().Contain("ghost@example.com");
    }

    [Fact]
    public async Task GetSubscriberAsync_WhenListmonkReturnsUnauthorized_ReturnsUnauthorizedError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"nope\"}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.GetSubscriberAsync("x@example.com", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.Unauthorized");
    }

    // ============================================================================
    // UnsubscribeAsync
    // ============================================================================

    [Fact]
    public async Task UnsubscribeAsync_WhenEmailNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.NewsletterService.UnsubscribeAsync(null!, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UnsubscribeAsync_WhenSubscriberLookupFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[],\"total\":0,\"page\":1,\"per_page\":1}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UnsubscribeAsync("ghost@example.com", null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.SubscriberNotFound");
    }

    [Fact]
    public async Task UnsubscribeAsync_WhenListIdProvided_RemovesSubscriberFromThatList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":99,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/lists")
            .WithPartialContent("\"action\":\"remove\"")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UnsubscribeAsync("u@example.com", "5", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UnsubscribeAsync_WhenListIdProvidedButRemoveFails_ReturnsFailureFromHttp()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":99,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/lists")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"message\":\"bad\"}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UnsubscribeAsync("u@example.com", "5", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.BadRequest");
    }

    [Fact]
    public async Task UnsubscribeAsync_WhenListIdNull_DisablesSubscriber()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":99,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/99")
            .WithPartialContent("\"status\":\"disabled\"")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":99,\"email\":\"u@example.com\",\"status\":\"disabled\"}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UnsubscribeAsync("u@example.com", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UnsubscribeAsync_WhenListIdProvidedButNotInteger_FallsBackToDisable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":99,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/99")
            .WithPartialContent("\"status\":\"disabled\"")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":99,\"email\":\"u@example.com\",\"status\":\"disabled\"}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UnsubscribeAsync("u@example.com", "not-an-int", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UnsubscribeAsync_WhenDisableUpdateFails_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":99,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/99")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"gone\"}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UnsubscribeAsync("u@example.com", null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.NotFound");
    }

    // ============================================================================
    // UpdateSubscriberAttributesAsync
    // ============================================================================

    [Fact]
    public async Task UpdateSubscriberAttributesAsync_WhenEmailNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.NewsletterService.UpdateSubscriberAttributesAsync(
            null!, new Dictionary<string, object>(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateSubscriberAttributesAsync_WhenAttributesNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.NewsletterService.UpdateSubscriberAttributesAsync(
            "x@example.com", null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateSubscriberAttributesAsync_WhenSubscriberLookupFails_ReturnsLookupError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[],\"total\":0,\"page\":1,\"per_page\":1}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UpdateSubscriberAttributesAsync(
            "ghost@example.com",
            new Dictionary<string, object> { ["k"] = "v" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.SubscriberNotFound");
    }

    [Fact]
    public async Task UpdateSubscriberAttributesAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":12,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/12")
            .WithPartialContent("\"attribs\":{")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"id\":12,\"email\":\"u@example.com\",\"status\":\"enabled\"}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UpdateSubscriberAttributesAsync(
            "u@example.com",
            new Dictionary<string, object> { ["plan"] = "gold" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UpdateSubscriberAttributesAsync_WhenUpdateFails_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/subscribers*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[{\"id\":12,\"email\":\"u@example.com\",\"status\":\"enabled\"}]," +
                "\"total\":1,\"page\":1,\"per_page\":1}}");

        mock.Expect(HttpMethod.Put, "https://listmonk.test/api/subscribers/12")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"denied\"}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.UpdateSubscriberAttributesAsync(
            "u@example.com",
            new Dictionary<string, object> { ["x"] = 1 },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.Forbidden");
    }

    // ============================================================================
    // ListMailingListsAsync
    // ============================================================================

    [Fact]
    public async Task ListMailingListsAsync_OnSuccess_ReturnsMappedLists()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/lists*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":[" +
                "{\"id\":1,\"uuid\":\"abc\",\"name\":\"Public list\",\"type\":\"public\",\"optin\":\"single\",\"description\":\"d\",\"subscriber_count\":5,\"created_at\":\"2025-01-01T00:00:00Z\"}," +
                "{\"id\":2,\"name\":\"Private list\",\"type\":\"private\",\"optin\":\"double\"}" +
                "],\"total\":2,\"page\":1,\"per_page\":100}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.ListMailingListsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be("1");
        result.Value[0].Slug.Should().Be("abc");
        result.Value[0].IsPublic.Should().BeTrue();
        result.Value[0].IsSingleOptIn.Should().BeTrue();
        result.Value[0].SubscriberCount.Should().Be(5);
        result.Value[1].Slug.Should().Be("2");
        result.Value[1].IsPublic.Should().BeFalse();
        result.Value[1].IsSingleOptIn.Should().BeFalse();
    }

    [Fact]
    public async Task ListMailingListsAsync_WhenResultsNull_ReturnsEmptyList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Get, "https://listmonk.test/api/lists*")
            .Respond(HttpStatusCode.OK, "application/json",
                "{\"data\":{\"results\":null,\"total\":0,\"page\":1,\"per_page\":100}}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.ListMailingListsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMailingListsAsync_WhenHttpFails_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://listmonk.test/api/lists*")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"nope\"}");

        using var host = new ListmonkTestHost(mock);

        // Act
        var result = await host.NewsletterService.ListMailingListsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.Unauthorized");
    }

    // ============================================================================
    // ConfirmSubscriptionAsync
    // ============================================================================

    [Fact]
    public async Task ConfirmSubscriptionAsync_AlwaysReturnsSuccessBecauseListmonkHandlesItDirectly()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var result = await host.NewsletterService.ConfirmSubscriptionAsync(
            "x@example.com", "token-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ============================================================================
    // Subscriber status mapping (via SubscribeAsync round-trip)
    // ============================================================================

    [Theory]
    [InlineData("enabled", SubscriptionStatus.Confirmed)]
    [InlineData("disabled", SubscriptionStatus.Unsubscribed)]
    [InlineData("blocklisted", SubscriptionStatus.Blocked)]
    [InlineData("unknown-state", SubscriptionStatus.Pending)]
    [InlineData(null, SubscriptionStatus.Pending)]
    public async Task SubscribeAsync_MapsSubscriberStatus(string? listmonkStatus, SubscriptionStatus expected)
    {
        // Arrange
        var statusJson = listmonkStatus is null ? "null" : $"\"{listmonkStatus}\"";
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "https://listmonk.test/api/subscribers")
            .Respond(HttpStatusCode.OK, "application/json",
                $"{{\"data\":{{\"id\":1,\"email\":\"e@example.com\",\"status\":{statusJson}}}}}");

        using var host = new ListmonkTestHost(mock);
        var request = new SubscribeRequest { Email = "e@example.com" };

        // Act
        var result = await host.NewsletterService.SubscribeAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
    }
}
