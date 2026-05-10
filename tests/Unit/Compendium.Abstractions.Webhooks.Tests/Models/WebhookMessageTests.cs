// -----------------------------------------------------------------------
// <copyright file="WebhookMessageTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Webhooks.Tests.Models;

public class WebhookMessageTests
{
    [Fact]
    public void WebhookMessage_WithRequiredOnly_DefaultsTimestampToUtcNowAndLeavesEventIdNull()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var message = new WebhookMessage
        {
            Id = "msg-1",
            EventName = "order.created",
            Payload = new { orderId = "o-1" },
            TenantId = "tenant-1",
        };
        var after = DateTimeOffset.UtcNow;

        // Assert
        message.Id.Should().Be("msg-1");
        message.EventName.Should().Be("order.created");
        message.Payload.Should().NotBeNull();
        message.TenantId.Should().Be("tenant-1");
        message.EventId.Should().BeNull();
        message.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void WebhookMessage_WithExplicitTimestampAndEventId_PreservesValues()
    {
        // Arrange
        var ts = new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero);

        // Act
        var message = new WebhookMessage
        {
            Id = "msg-2",
            EventName = "user.invited",
            Payload = "raw-string-payload",
            TenantId = "tenant-9",
            EventId = "evt-42",
            Timestamp = ts,
        };

        // Assert
        message.EventId.Should().Be("evt-42");
        message.Timestamp.Should().Be(ts);
        message.Payload.Should().Be("raw-string-payload");
    }

    [Fact]
    public void WebhookMessage_RecordEquality_IdenticalMessages_AreEqual()
    {
        // Arrange
        var payload = new { foo = "bar" };
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var first = new WebhookMessage
        {
            Id = "id",
            EventName = "evt",
            Payload = payload,
            TenantId = "t",
            EventId = "e",
            Timestamp = ts,
        };
        var second = new WebhookMessage
        {
            Id = "id",
            EventName = "evt",
            Payload = payload,
            TenantId = "t",
            EventId = "e",
            Timestamp = ts,
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void WebhookMessage_RecordEquality_DifferingTenant_AreNotEqual()
    {
        // Arrange
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var payload = new { x = 1 };
        var first = new WebhookMessage
        {
            Id = "id",
            EventName = "evt",
            Payload = payload,
            TenantId = "a",
            Timestamp = ts,
        };
        var second = first with { TenantId = "b" };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void WebhookMessage_With_ProducesModifiedCopyAndLeavesOriginalUnchanged()
    {
        // Arrange
        var original = new WebhookMessage
        {
            Id = "id",
            EventName = "evt",
            Payload = new { a = 1 },
            TenantId = "t",
        };

        // Act
        var copy = original with { EventId = "e-99" };

        // Assert
        copy.EventId.Should().Be("e-99");
        original.EventId.Should().BeNull();
    }
}
