// -----------------------------------------------------------------------
// <copyright file="SmsMessageTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class SmsMessageTests
{
    [Fact]
    public void SmsMessage_WithRequiredOnly_DefaultsMediaUrlsToNull()
    {
        // Arrange / Act
        var message = new SmsMessage
        {
            From = "+15551234567",
            To = "+15557654321",
            Body = "Hello",
            TenantId = "tenant-1",
        };

        // Assert
        message.From.Should().Be("+15551234567");
        message.To.Should().Be("+15557654321");
        message.Body.Should().Be("Hello");
        message.TenantId.Should().Be("tenant-1");
        message.MediaUrls.Should().BeNull();
    }

    [Fact]
    public void SmsMessage_WithMediaUrls_PreservesValues()
    {
        // Arrange
        var media = new[] { "https://cdn.example.com/a.jpg", "https://cdn.example.com/b.png" };

        // Act
        var message = new SmsMessage
        {
            From = "+15551234567",
            To = "+15557654321",
            Body = "See attached",
            TenantId = "tenant-1",
            MediaUrls = media,
        };

        // Assert
        message.MediaUrls.Should().BeEquivalentTo(media);
    }

    [Fact]
    public void SmsMessage_RecordEquality_TwoIdenticalMessages_AreEqual()
    {
        // Arrange
        var media = new[] { "https://cdn.example.com/a.jpg" };
        var first = new SmsMessage
        {
            From = "+15551234567",
            To = "+15557654321",
            Body = "B",
            TenantId = "t-1",
            MediaUrls = media,
        };
        var second = new SmsMessage
        {
            From = "+15551234567",
            To = "+15557654321",
            Body = "B",
            TenantId = "t-1",
            MediaUrls = media,
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void SmsMessage_RecordEquality_DifferingTo_AreNotEqual()
    {
        // Arrange
        var first = new SmsMessage { From = "+1", To = "+15557654321", Body = "B", TenantId = "t" };
        var second = new SmsMessage { From = "+1", To = "+15557654322", Body = "B", TenantId = "t" };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void SmsMessage_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new SmsMessage
        {
            From = "+15551234567",
            To = "+15557654321",
            Body = "Hello",
            TenantId = "tenant-1",
        };

        // Act
        var updated = original with { Body = "Bye" };

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Body.Should().Be("Bye");
        original.Body.Should().Be("Hello");
        updated.From.Should().Be(original.From);
        updated.To.Should().Be(original.To);
        updated.TenantId.Should().Be(original.TenantId);
    }
}
