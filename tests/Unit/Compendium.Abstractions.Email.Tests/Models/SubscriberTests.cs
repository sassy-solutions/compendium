// -----------------------------------------------------------------------
// <copyright file="SubscriberTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests.Models;

public class SubscriberTests
{
    [Fact]
    public void Subscriber_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange / Act
        var subscriber = new Subscriber
        {
            Id = "sub-1",
            Email = "alice@example.com",
            Status = SubscriptionStatus.Pending,
        };

        // Assert
        subscriber.Id.Should().Be("sub-1");
        subscriber.Email.Should().Be("alice@example.com");
        subscriber.Status.Should().Be(SubscriptionStatus.Pending);
        subscriber.Name.Should().BeNull();
        subscriber.ListIds.Should().BeNull();
        subscriber.Attributes.Should().BeNull();
        subscriber.CreatedAt.Should().Be(default);
        subscriber.UpdatedAt.Should().BeNull();
        subscriber.ConfirmedAt.Should().BeNull();
        subscriber.UnsubscribedAt.Should().BeNull();
    }

    [Fact]
    public void Subscriber_WithAllProperties_PreservesValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var confirmedAt = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var unsubscribedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var attributes = new Dictionary<string, object> { ["plan"] = "pro", ["age"] = 30 };

        // Act
        var subscriber = new Subscriber
        {
            Id = "sub-2",
            Email = "bob@example.com",
            Name = "Bob",
            Status = SubscriptionStatus.Confirmed,
            ListIds = new[] { "list-1", "list-2" },
            Attributes = attributes,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ConfirmedAt = confirmedAt,
            UnsubscribedAt = unsubscribedAt,
        };

        // Assert
        subscriber.Id.Should().Be("sub-2");
        subscriber.Email.Should().Be("bob@example.com");
        subscriber.Name.Should().Be("Bob");
        subscriber.Status.Should().Be(SubscriptionStatus.Confirmed);
        subscriber.ListIds.Should().HaveCount(2);
        subscriber.Attributes.Should().BeSameAs(attributes);
        subscriber.CreatedAt.Should().Be(createdAt);
        subscriber.UpdatedAt.Should().Be(updatedAt);
        subscriber.ConfirmedAt.Should().Be(confirmedAt);
        subscriber.UnsubscribedAt.Should().Be(unsubscribedAt);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Pending)]
    [InlineData(SubscriptionStatus.Confirmed)]
    [InlineData(SubscriptionStatus.Unsubscribed)]
    [InlineData(SubscriptionStatus.Blocked)]
    [InlineData(SubscriptionStatus.Bounced)]
    public void Subscriber_AllStatuses_AreAcceptedAsInitValues(SubscriptionStatus status)
    {
        // Arrange / Act
        var subscriber = new Subscriber
        {
            Id = "id",
            Email = "x@y.co",
            Status = status,
        };

        // Assert
        subscriber.Status.Should().Be(status);
    }

    [Fact]
    public void Subscriber_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new Subscriber
        {
            Id = "sub-1",
            Email = "alice@example.com",
            Status = SubscriptionStatus.Pending,
        };

        // Act
        var updated = original with { Status = SubscriptionStatus.Confirmed };

        // Assert
        updated.Status.Should().Be(SubscriptionStatus.Confirmed);
        original.Status.Should().Be(SubscriptionStatus.Pending);
        updated.Email.Should().Be(original.Email);
    }
}

public class SubscribeRequestTests
{
    [Fact]
    public void SubscribeRequest_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange / Act
        var request = new SubscribeRequest { Email = "alice@example.com" };

        // Assert
        request.Email.Should().Be("alice@example.com");
        request.Name.Should().BeNull();
        request.ListIds.Should().BeNull();
        request.Attributes.Should().BeNull();
        request.RequireConfirmation.Should().BeTrue();
    }

    [Fact]
    public void SubscribeRequest_WithAllProperties_PreservesValues()
    {
        // Arrange
        var attributes = new Dictionary<string, object> { ["source"] = "homepage" };

        // Act
        var request = new SubscribeRequest
        {
            Email = "bob@example.com",
            Name = "Bob",
            ListIds = new[] { "list-1" },
            Attributes = attributes,
            RequireConfirmation = false,
        };

        // Assert
        request.Email.Should().Be("bob@example.com");
        request.Name.Should().Be("Bob");
        request.ListIds.Should().ContainSingle().Which.Should().Be("list-1");
        request.Attributes.Should().BeSameAs(attributes);
        request.RequireConfirmation.Should().BeFalse();
    }

    [Fact]
    public void SubscribeRequest_RecordEquality_IsValueBasedForScalarFields()
    {
        // Arrange
        var first = new SubscribeRequest { Email = "alice@example.com", Name = "Alice" };
        var second = new SubscribeRequest { Email = "alice@example.com", Name = "Alice" };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SubscribeRequest_RequireConfirmation_AcceptsBothValues(bool requireConfirmation)
    {
        // Arrange / Act
        var request = new SubscribeRequest
        {
            Email = "x@y.co",
            RequireConfirmation = requireConfirmation,
        };

        // Assert
        request.RequireConfirmation.Should().Be(requireConfirmation);
    }
}

public class SubscriptionStatusTests
{
    [Fact]
    public void SubscriptionStatus_HasExpectedNumericValues()
    {
        // Assert
        ((int)SubscriptionStatus.Pending).Should().Be(0);
        ((int)SubscriptionStatus.Confirmed).Should().Be(1);
        ((int)SubscriptionStatus.Unsubscribed).Should().Be(2);
        ((int)SubscriptionStatus.Blocked).Should().Be(3);
        ((int)SubscriptionStatus.Bounced).Should().Be(4);
    }

    [Fact]
    public void SubscriptionStatus_DefinesAllFiveStates()
    {
        // Act
        var values = Enum.GetValues<SubscriptionStatus>();

        // Assert
        values.Should().HaveCount(5);
    }
}
