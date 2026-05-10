// -----------------------------------------------------------------------
// <copyright file="EmailAttachmentTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests.Models;

public class EmailAttachmentTests
{
    [Fact]
    public void EmailAttachment_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4 };

        // Act
        var attachment = new EmailAttachment
        {
            Filename = "report.pdf",
            Content = content,
            ContentType = "application/pdf",
        };

        // Assert
        attachment.Filename.Should().Be("report.pdf");
        attachment.Content.Should().BeSameAs(content);
        attachment.ContentType.Should().Be("application/pdf");
        attachment.IsInline.Should().BeFalse();
        attachment.ContentId.Should().BeNull();
    }

    [Fact]
    public void EmailAttachment_AsInline_IncludesContentId()
    {
        // Arrange / Act
        var attachment = new EmailAttachment
        {
            Filename = "logo.png",
            Content = new byte[] { 0xFF },
            ContentType = "image/png",
            IsInline = true,
            ContentId = "logo@example",
        };

        // Assert
        attachment.IsInline.Should().BeTrue();
        attachment.ContentId.Should().Be("logo@example");
    }

    [Fact]
    public void EmailAttachment_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new EmailAttachment
        {
            Filename = "old.txt",
            Content = new byte[] { 1 },
            ContentType = "text/plain",
        };

        // Act
        var updated = original with { Filename = "new.txt" };

        // Assert
        updated.Filename.Should().Be("new.txt");
        original.Filename.Should().Be("old.txt");
        updated.Content.Should().BeSameAs(original.Content);
        updated.ContentType.Should().Be(original.ContentType);
    }

    [Theory]
    [InlineData("a.txt", "text/plain")]
    [InlineData("img.png", "image/png")]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("data.bin", "application/octet-stream")]
    public void EmailAttachment_AcceptsCommonMediaTypes(string filename, string contentType)
    {
        // Arrange / Act
        var attachment = new EmailAttachment
        {
            Filename = filename,
            Content = Array.Empty<byte>(),
            ContentType = contentType,
        };

        // Assert
        attachment.Filename.Should().Be(filename);
        attachment.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void EmailAttachment_EmptyContent_IsAllowed()
    {
        // Act
        var attachment = new EmailAttachment
        {
            Filename = "empty.dat",
            Content = Array.Empty<byte>(),
            ContentType = "application/octet-stream",
        };

        // Assert
        attachment.Content.Should().BeEmpty();
    }
}
