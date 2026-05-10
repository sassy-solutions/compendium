// -----------------------------------------------------------------------
// <copyright file="MarketingIntegrationEventsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.Tests.Domain.Events.Integration;

/// <summary>
/// Unit tests for marketing integration event records.
/// </summary>
public class MarketingIntegrationEventsTests
{
    [Fact]
    public void SubscriberConfirmedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var confirmedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriberConfirmedEvent("sub-1", "u@x.com", "list-1", "Newsletter", confirmedAt);

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.confirmed");
        evt.SubscriberId.Should().Be("sub-1");
        evt.ListId.Should().Be("list-1");
        evt.ListName.Should().Be("Newsletter");
        evt.ConfirmedAt.Should().Be(confirmedAt);
    }

    [Fact]
    public void SubscriberBouncedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var bouncedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriberBouncedEvent("sub-1", "u@x.com", "hard", "mailbox-full", bouncedAt, "camp-1");

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.bounced");
        evt.BounceType.Should().Be("hard");
        evt.BounceReason.Should().Be("mailbox-full");
        evt.BouncedAt.Should().Be(bouncedAt);
        evt.CampaignId.Should().Be("camp-1");
    }

    [Fact]
    public void SubscriberBouncedEvent_WithNullableNulls_AllowsNulls()
    {
        // Act
        var evt = new SubscriberBouncedEvent("sub-1", "u@x.com", "soft", BounceReason: null, DateTimeOffset.UtcNow, CampaignId: null);

        // Assert
        evt.BounceReason.Should().BeNull();
        evt.CampaignId.Should().BeNull();
    }

    [Fact]
    public void SubscriberComplainedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var complainedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriberComplainedEvent("sub-1", "u@x.com", complainedAt, "camp-1");

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.complained");
        evt.ComplainedAt.Should().Be(complainedAt);
        evt.CampaignId.Should().Be("camp-1");
    }

    [Fact]
    public void SubscriberComplainedEvent_WithoutCampaign_AllowsNull()
    {
        // Act
        var evt = new SubscriberComplainedEvent("sub-1", "u@x.com", DateTimeOffset.UtcNow, CampaignId: null);

        // Assert
        evt.CampaignId.Should().BeNull();
    }

    [Fact]
    public void SubscriberBlocklistedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var blocklistedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriberBlocklistedEvent("sub-1", "u@x.com", "spam", blocklistedAt);

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.blocklisted");
        evt.BlocklistReason.Should().Be("spam");
        evt.BlocklistedAt.Should().Be(blocklistedAt);
    }

    [Fact]
    public void SubscriberAttributesUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var attrs = new Dictionary<string, string?>
        {
            ["FirstName"] = "Alice",
            ["LastName"] = null
        };
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriberAttributesUpdatedEvent("sub-1", "u@x.com", attrs, updatedAt);

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.attributes_updated");
        evt.UpdatedAttributes.Should().BeEquivalentTo(attrs);
        evt.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void CampaignOpenedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var openedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new CampaignOpenedEvent("camp-1", "sub-1", "u@x.com", openedAt, "1.2.3.4", "Mozilla/5.0");

        // Assert
        evt.EventType.Should().Be("marketing.campaign.opened");
        evt.OpenedAt.Should().Be(openedAt);
        evt.IpAddress.Should().Be("1.2.3.4");
        evt.UserAgent.Should().Be("Mozilla/5.0");
    }

    [Fact]
    public void CampaignOpenedEvent_WithNullableNulls_AllowsNulls()
    {
        // Act
        var evt = new CampaignOpenedEvent("camp-1", "sub-1", "u@x.com", DateTimeOffset.UtcNow, IpAddress: null, UserAgent: null);

        // Assert
        evt.IpAddress.Should().BeNull();
        evt.UserAgent.Should().BeNull();
    }

    [Fact]
    public void CampaignLinkClickedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var clickedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new CampaignLinkClickedEvent("camp-1", "sub-1", "u@x.com", "https://example.com", clickedAt, "1.2.3.4");

        // Assert
        evt.EventType.Should().Be("marketing.campaign.link_clicked");
        evt.LinkUrl.Should().Be("https://example.com");
        evt.ClickedAt.Should().Be(clickedAt);
        evt.IpAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public void TransactionalEmailSentEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var sentAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TransactionalEmailSentEvent("tx-1", "tpl-1", "u@x.com", "Welcome", sentAt, "delivered");

        // Assert
        evt.EventType.Should().Be("marketing.transactional.sent");
        evt.TransactionalId.Should().Be("tx-1");
        evt.TemplateId.Should().Be("tpl-1");
        evt.RecipientEmail.Should().Be("u@x.com");
        evt.Subject.Should().Be("Welcome");
        evt.SentAt.Should().Be(sentAt);
        evt.Status.Should().Be("delivered");
    }

    [Fact]
    public void ListCreatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new ListCreatedEvent("list-1", "Friends", "Friends list", "private", createdAt);

        // Assert
        evt.EventType.Should().Be("marketing.list.created");
        evt.ListId.Should().Be("list-1");
        evt.Description.Should().Be("Friends list");
        evt.ListType.Should().Be("private");
        evt.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ListCreatedEvent_WithNullDescription_AllowsNull()
    {
        // Act
        var evt = new ListCreatedEvent("list-1", "Public", Description: null, "public", DateTimeOffset.UtcNow);

        // Assert
        evt.Description.Should().BeNull();
    }

    [Fact]
    public void ListDeletedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var deletedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new ListDeletedEvent("list-1", "Old List", 1234, deletedAt);

        // Assert
        evt.EventType.Should().Be("marketing.list.deleted");
        evt.SubscriberCount.Should().Be(1234);
        evt.DeletedAt.Should().Be(deletedAt);
    }
}
