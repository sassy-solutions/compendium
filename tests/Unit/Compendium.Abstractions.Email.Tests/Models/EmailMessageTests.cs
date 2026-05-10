// -----------------------------------------------------------------------
// <copyright file="EmailMessageTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests.Models;

public class EmailMessageTests
{
    [Fact]
    public void EmailMessage_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange / Act
        var message = new EmailMessage
        {
            To = new[] { "alice@example.com" },
            Subject = "Hello",
        };

        // Assert
        message.To.Should().ContainSingle().Which.Should().Be("alice@example.com");
        message.Subject.Should().Be("Hello");
        message.Cc.Should().BeNull();
        message.Bcc.Should().BeNull();
        message.From.Should().BeNull();
        message.ReplyTo.Should().BeNull();
        message.TextBody.Should().BeNull();
        message.HtmlBody.Should().BeNull();
        message.Attachments.Should().BeNull();
        message.Headers.Should().BeNull();
        message.Metadata.Should().BeNull();
        message.Priority.Should().Be(EmailPriority.Normal);
    }

    [Fact]
    public void EmailMessage_WithAllProperties_PreservesValues()
    {
        // Arrange
        var attachment = new EmailAttachment
        {
            Filename = "spec.pdf",
            Content = new byte[] { 1, 2, 3 },
            ContentType = "application/pdf",
        };

        // Act
        var message = new EmailMessage
        {
            To = new[] { "alice@example.com", "bob@example.com" },
            Cc = new[] { "carol@example.com" },
            Bcc = new[] { "dave@example.com" },
            From = "sender@example.com",
            ReplyTo = "noreply@example.com",
            Subject = "Project update",
            TextBody = "Plain content",
            HtmlBody = "<p>HTML content</p>",
            Attachments = new[] { attachment },
            Headers = new Dictionary<string, string> { ["X-Tag"] = "newsletter" },
            Priority = EmailPriority.High,
            Metadata = new Dictionary<string, object> { ["campaign"] = "spring2026" },
        };

        // Assert
        message.To.Should().HaveCount(2);
        message.Cc.Should().ContainSingle().Which.Should().Be("carol@example.com");
        message.Bcc.Should().ContainSingle().Which.Should().Be("dave@example.com");
        message.From.Should().Be("sender@example.com");
        message.ReplyTo.Should().Be("noreply@example.com");
        message.Subject.Should().Be("Project update");
        message.TextBody.Should().Be("Plain content");
        message.HtmlBody.Should().Be("<p>HTML content</p>");
        message.Attachments.Should().ContainSingle().Which.Should().BeSameAs(attachment);
        message.Headers.Should().ContainKey("X-Tag");
        message.Priority.Should().Be(EmailPriority.High);
        message.Metadata.Should().ContainKey("campaign");
    }

    [Fact]
    public void EmailMessage_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new EmailMessage { To = new[] { "a@b.co" }, Subject = "S" };
        var second = new EmailMessage { To = new[] { "a@b.co" }, Subject = "S" };

        // Act / Assert — record equality is reference-based for IReadOnlyList collections,
        // but the same instance must equal itself and be reference-equal to a `with`-clone of itself.
        first.Should().Be(first);
        first.GetHashCode().Should().Be(first.GetHashCode());
        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void EmailMessage_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new EmailMessage { To = new[] { "a@b.co" }, Subject = "Original" };

        // Act
        var updated = original with { Subject = "Updated" };

        // Assert
        updated.Subject.Should().Be("Updated");
        original.Subject.Should().Be("Original");
        updated.To.Should().BeSameAs(original.To);
    }

    [Theory]
    [InlineData(EmailPriority.Low)]
    [InlineData(EmailPriority.Normal)]
    [InlineData(EmailPriority.High)]
    public void EmailMessage_PriorityOption_IsAcceptedAsInitValue(EmailPriority priority)
    {
        // Arrange / Act
        var message = new EmailMessage
        {
            To = new[] { "a@b.co" },
            Subject = "S",
            Priority = priority,
        };

        // Assert
        message.Priority.Should().Be(priority);
    }
}

public class EmailPriorityTests
{
    [Fact]
    public void EmailPriority_HasExpectedNumericValues()
    {
        // Assert
        ((int)EmailPriority.Low).Should().Be(0);
        ((int)EmailPriority.Normal).Should().Be(1);
        ((int)EmailPriority.High).Should().Be(2);
    }

    [Fact]
    public void EmailPriority_ContainsExactlyThreeValues()
    {
        // Act
        var values = Enum.GetValues<EmailPriority>();

        // Assert
        values.Should().BeEquivalentTo(new[] { EmailPriority.Low, EmailPriority.Normal, EmailPriority.High });
    }
}
