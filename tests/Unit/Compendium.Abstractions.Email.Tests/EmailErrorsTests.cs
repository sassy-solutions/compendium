// -----------------------------------------------------------------------
// <copyright file="EmailErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests;

public class EmailErrorsTests
{
    [Fact]
    public void SubscriberNotFound_WithEmail_ReturnsNotFoundError()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        var error = EmailErrors.SubscriberNotFound(email);

        // Assert
        error.Code.Should().Be("Email.SubscriberNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a@b.co")]
    [InlineData("user+tag@example.com")]
    public void SubscriberNotFound_WithVariousEmails_EmbedsEmailInMessage(string email)
    {
        // Act
        var error = EmailErrors.SubscriberNotFound(email);

        // Assert
        error.Code.Should().Be("Email.SubscriberNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain($"'{email}'");
    }

    [Fact]
    public void SubscriberAlreadyExists_WithEmail_ReturnsConflictError()
    {
        // Arrange
        const string email = "duplicate@example.com";

        // Act
        var error = EmailErrors.SubscriberAlreadyExists(email);

        // Assert
        error.Code.Should().Be("Email.SubscriberAlreadyExists");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(email);
    }

    [Fact]
    public void MailingListNotFound_WithListId_ReturnsNotFoundError()
    {
        // Arrange
        const string listId = "list-abc-123";

        // Act
        var error = EmailErrors.MailingListNotFound(listId);

        // Assert
        error.Code.Should().Be("Email.MailingListNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(listId);
    }

    [Fact]
    public void TemplateNotFound_WithTemplateId_ReturnsNotFoundError()
    {
        // Arrange
        const string templateId = "welcome-email";

        // Act
        var error = EmailErrors.TemplateNotFound(templateId);

        // Assert
        error.Code.Should().Be("Email.TemplateNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(templateId);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("plain text")]
    [InlineData("")]
    public void InvalidEmailFormat_WithVariousEmails_ReturnsValidationError(string email)
    {
        // Act
        var error = EmailErrors.InvalidEmailFormat(email);

        // Assert
        error.Code.Should().Be("Email.InvalidEmailFormat");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{email}'");
    }

    [Fact]
    public void InvalidRecipient_WithReason_ReturnsValidationErrorWithReasonAsMessage()
    {
        // Arrange
        const string reason = "Recipient list cannot be empty.";

        // Act
        var error = EmailErrors.InvalidRecipient(reason);

        // Assert
        error.Code.Should().Be("Email.InvalidRecipient");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Be(reason);
    }

    [Fact]
    public void SendFailed_WithReason_ReturnsFailureErrorWithReasonEmbedded()
    {
        // Arrange
        const string reason = "SMTP connection refused";

        // Act
        var error = EmailErrors.SendFailed(reason);

        // Assert
        error.Code.Should().Be("Email.SendFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Message.Should().Contain(reason);
        error.Message.Should().StartWith("Failed to send email:");
    }

    [Fact]
    public void ProviderUnavailable_IsStaticReadonlyUnavailableError()
    {
        // Act
        var error = EmailErrors.ProviderUnavailable;

        // Assert
        error.Code.Should().Be("Email.ProviderUnavailable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ProviderUnavailable_AccessedTwice_ReturnsSameInstance()
    {
        // Act
        var first = EmailErrors.ProviderUnavailable;
        var second = EmailErrors.ProviderUnavailable;

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void RateLimitExceeded_IsStaticReadonlyTooManyRequestsError()
    {
        // Act
        var error = EmailErrors.RateLimitExceeded;

        // Assert
        error.Code.Should().Be("Email.RateLimitExceeded");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("Rate limit");
    }

    [Fact]
    public void MessageNotFound_WithMessageId_ReturnsNotFoundError()
    {
        // Arrange
        const string messageId = "msg-9876";

        // Act
        var error = EmailErrors.MessageNotFound(messageId);

        // Assert
        error.Code.Should().Be("Email.MessageNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(messageId);
    }

    [Fact]
    public void InvalidConfirmationToken_IsStaticReadonlyValidationError()
    {
        // Act
        var error = EmailErrors.InvalidConfirmationToken;

        // Assert
        error.Code.Should().Be("Email.InvalidConfirmationToken");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("confirmation token");
    }

    [Fact]
    public void TenantContextRequired_IsStaticReadonlyValidationError()
    {
        // Act
        var error = EmailErrors.TenantContextRequired;

        // Assert
        error.Code.Should().Be("Email.TenantContextRequired");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("Tenant context");
    }

    [Fact]
    public void AllErrorFactories_ReturnNonNullErrorInstances()
    {
        // Act / Assert
        EmailErrors.SubscriberNotFound("a@b.co").Should().NotBeNull();
        EmailErrors.SubscriberAlreadyExists("a@b.co").Should().NotBeNull();
        EmailErrors.MailingListNotFound("id").Should().NotBeNull();
        EmailErrors.TemplateNotFound("id").Should().NotBeNull();
        EmailErrors.InvalidEmailFormat("x").Should().NotBeNull();
        EmailErrors.InvalidRecipient("x").Should().NotBeNull();
        EmailErrors.SendFailed("x").Should().NotBeNull();
        EmailErrors.MessageNotFound("id").Should().NotBeNull();
        EmailErrors.ProviderUnavailable.Should().NotBeNull();
        EmailErrors.RateLimitExceeded.Should().NotBeNull();
        EmailErrors.InvalidConfirmationToken.Should().NotBeNull();
        EmailErrors.TenantContextRequired.Should().NotBeNull();
    }

    [Fact]
    public void AllErrorCodes_StartWithEmailPrefix()
    {
        // Act
        var codes = new[]
        {
            EmailErrors.SubscriberNotFound("a@b.co").Code,
            EmailErrors.SubscriberAlreadyExists("a@b.co").Code,
            EmailErrors.MailingListNotFound("id").Code,
            EmailErrors.TemplateNotFound("id").Code,
            EmailErrors.InvalidEmailFormat("x").Code,
            EmailErrors.InvalidRecipient("x").Code,
            EmailErrors.SendFailed("x").Code,
            EmailErrors.MessageNotFound("id").Code,
            EmailErrors.ProviderUnavailable.Code,
            EmailErrors.RateLimitExceeded.Code,
            EmailErrors.InvalidConfirmationToken.Code,
            EmailErrors.TenantContextRequired.Code,
        };

        // Assert
        codes.Should().OnlyContain(c => c.StartsWith("Email.", StringComparison.Ordinal));
    }
}
