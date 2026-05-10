// -----------------------------------------------------------------------
// <copyright file="TemplatedEmailMessageTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests.Models;

public class TemplatedEmailMessageTests
{
    [Fact]
    public void TemplatedEmailMessage_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange / Act
        var message = new TemplatedEmailMessage
        {
            To = new[] { "alice@example.com" },
            TemplateId = "welcome",
        };

        // Assert
        message.To.Should().ContainSingle().Which.Should().Be("alice@example.com");
        message.TemplateId.Should().Be("welcome");
        message.Cc.Should().BeNull();
        message.Bcc.Should().BeNull();
        message.From.Should().BeNull();
        message.ReplyTo.Should().BeNull();
        message.TemplateData.Should().BeNull();
        message.Attachments.Should().BeNull();
        message.Metadata.Should().BeNull();
        message.Priority.Should().Be(EmailPriority.Normal);
    }

    [Fact]
    public void TemplatedEmailMessage_WithAllProperties_PreservesValues()
    {
        // Arrange
        var attachment = new EmailAttachment
        {
            Filename = "invoice.pdf",
            Content = new byte[] { 4, 5, 6 },
            ContentType = "application/pdf",
        };

        var templateData = new Dictionary<string, object>
        {
            ["firstName"] = "Alice",
            ["amount"] = 42.0m,
        };

        // Act
        var message = new TemplatedEmailMessage
        {
            To = new[] { "alice@example.com" },
            Cc = new[] { "carol@example.com" },
            Bcc = new[] { "dave@example.com" },
            From = "sender@example.com",
            ReplyTo = "noreply@example.com",
            TemplateId = "invoice-2026",
            TemplateData = templateData,
            Attachments = new[] { attachment },
            Priority = EmailPriority.High,
            Metadata = new Dictionary<string, object> { ["batch"] = "monthly" },
        };

        // Assert
        message.To.Should().ContainSingle();
        message.Cc.Should().ContainSingle().Which.Should().Be("carol@example.com");
        message.Bcc.Should().ContainSingle().Which.Should().Be("dave@example.com");
        message.From.Should().Be("sender@example.com");
        message.ReplyTo.Should().Be("noreply@example.com");
        message.TemplateId.Should().Be("invoice-2026");
        message.TemplateData.Should().BeSameAs(templateData);
        message.Attachments.Should().ContainSingle().Which.Should().BeSameAs(attachment);
        message.Priority.Should().Be(EmailPriority.High);
        message.Metadata.Should().ContainKey("batch");
    }

    [Fact]
    public void TemplatedEmailMessage_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new TemplatedEmailMessage
        {
            To = new[] { "a@b.co" },
            TemplateId = "welcome",
        };

        // Act
        var updated = original with { TemplateId = "goodbye" };

        // Assert
        updated.TemplateId.Should().Be("goodbye");
        original.TemplateId.Should().Be("welcome");
        updated.To.Should().BeSameAs(original.To);
    }

    [Theory]
    [InlineData(EmailPriority.Low)]
    [InlineData(EmailPriority.Normal)]
    [InlineData(EmailPriority.High)]
    public void TemplatedEmailMessage_PriorityOption_IsAcceptedAsInitValue(EmailPriority priority)
    {
        // Arrange / Act
        var message = new TemplatedEmailMessage
        {
            To = new[] { "a@b.co" },
            TemplateId = "t",
            Priority = priority,
        };

        // Assert
        message.Priority.Should().Be(priority);
    }
}
