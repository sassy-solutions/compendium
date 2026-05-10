// -----------------------------------------------------------------------
// <copyright file="ListmonkEmailServiceTests.cs" company="Sassy Solutions">
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
/// Unit tests for <c>ListmonkEmailService</c>, exercised through the public
/// <see cref="IEmailService"/> interface via the production DI registration.
/// </summary>
public class ListmonkEmailServiceTests
{
    [Fact]
    public async Task SendAsync_Always_ReturnsSendFailedBecauseListmonkRequiresTemplates()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());
        var message = new EmailMessage
        {
            To = new[] { "user@example.com" },
            Subject = "Hello",
            HtmlBody = "<p>Hi</p>"
        };

        // Act
        var result = await host.EmailService.SendAsync(message, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.SendFailed");
        result.Error.Message.Should().Contain("templated");
    }

    [Fact]
    public async Task SendAsync_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.EmailService.SendAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendTemplatedAsync_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.EmailService.SendTemplatedAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendTemplatedAsync_WhenRecipientsEmpty_ReturnsInvalidRecipient()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());
        var message = new TemplatedEmailMessage
        {
            To = Array.Empty<string>(),
            TemplateId = "42"
        };

        // Act
        var result = await host.EmailService.SendTemplatedAsync(message, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidRecipient");
    }

    [Fact]
    public async Task SendTemplatedAsync_WhenTemplateIdNotInteger_ReturnsTemplateNotFound()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());
        var message = new TemplatedEmailMessage
        {
            To = new[] { "user@example.com" },
            TemplateId = "not-an-int"
        };

        // Act
        var result = await host.EmailService.SendTemplatedAsync(message, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.TemplateNotFound");
            result.Error.Message.Should().Contain("not-an-int");
    }

    [Fact]
    public async Task SendTemplatedAsync_OnSuccess_ReturnsSentResultWithMessageId()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "https://listmonk.test/api/tx")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        using var host = new ListmonkTestHost(mock);
        var message = new TemplatedEmailMessage
        {
            To = new[] { "user@example.com" },
            TemplateId = "7",
            From = "sender@example.com",
            TemplateData = new Dictionary<string, object> { ["name"] = "Alice" }
        };

        // Act
        var result = await host.EmailService.SendTemplatedAsync(message, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MessageId.Should().NotBeNullOrEmpty();
        result.Value.Status.Should().Be(EmailStatus.Sent);
        result.Value.SentAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendTemplatedAsync_WhenFromOmitted_FallsBackToDefaultFromEmail()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "https://listmonk.test/api/tx")
            .WithPartialContent("\"from_email\":\"noreply@example.com\"")
            .Respond(HttpStatusCode.OK, "application/json", "{\"data\":true}");

        using var host = new ListmonkTestHost(mock);
        var message = new TemplatedEmailMessage
        {
            To = new[] { "user@example.com" },
            TemplateId = "1"
        };

        // Act
        var result = await host.EmailService.SendTemplatedAsync(message, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendTemplatedAsync_WhenListmonkReturnsBadRequest_ReturnsValidationError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://listmonk.test/api/tx")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"message\":\"invalid template\"}");

        using var host = new ListmonkTestHost(mock);
        var message = new TemplatedEmailMessage
        {
            To = new[] { "user@example.com" },
            TemplateId = "1"
        };

        // Act
        var result = await host.EmailService.SendTemplatedAsync(message, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.BadRequest");
        result.Error.Message.Should().Contain("invalid template");
    }

    [Fact]
    public async Task SendBatchAsync_WhenMessagesIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var act = async () => await host.EmailService.SendBatchAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendBatchAsync_ReturnsFailureForEachMessageBecauseListmonkRequiresTemplates()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());
        var messages = new[]
        {
            new EmailMessage { To = new[] { "a@example.com" }, Subject = "s1" },
            new EmailMessage { To = new[] { "b@example.com" }, Subject = "s2" },
            new EmailMessage { To = Array.Empty<string>(), Subject = "s3" }
        };

        // Act
        var result = await host.EmailService.SendBatchAsync(messages, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
        result.Value.SuccessCount.Should().Be(0);
        result.Value.Results.Should().HaveCount(3);
        result.Value.Results.Should().AllSatisfy(r =>
        {
            r.Success.Should().BeFalse();
            r.ErrorMessage.Should().Contain("templated");
        });
        result.Value.Results[0].To.Should().Be("a@example.com");
        result.Value.Results[2].To.Should().Be("unknown");
    }

    [Fact]
    public async Task GetMessageStatusAsync_AlwaysReturnsSentBecauseListmonkDoesNotTrackStatus()
    {
        // Arrange
        using var host = new ListmonkTestHost(new MockHttpMessageHandler());

        // Act
        var result = await host.EmailService.GetMessageStatusAsync("any-id", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(EmailStatus.Sent);
    }

    [Fact]
    public async Task SendTemplatedAsync_WhenListmonkReturnsServerError_ReturnsFailureFromDefaultBranch()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://listmonk.test/api/tx")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"boom\"}");

        using var host = new ListmonkTestHost(mock);
        var message = new TemplatedEmailMessage
        {
            To = new[] { "user@example.com" },
            TemplateId = "1"
        };

        // Act
        var result = await host.EmailService.SendTemplatedAsync(message, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listmonk.Error");
        result.Error.Message.Should().Contain("boom");
    }
}
