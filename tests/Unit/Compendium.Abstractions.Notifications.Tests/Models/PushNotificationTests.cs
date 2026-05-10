// -----------------------------------------------------------------------
// <copyright file="PushNotificationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class PushNotificationTests
{
    [Fact]
    public void PushNotification_WithRequiredOnly_DefaultsOptionalProperties()
    {
        // Arrange / Act
        var notification = new PushNotification
        {
            Title = "Hello",
            Body = "World",
        };

        // Assert
        notification.Title.Should().Be("Hello");
        notification.Body.Should().Be("World");
        notification.Data.Should().NotBeNull().And.BeEmpty();
        notification.Sound.Should().BeNull();
        notification.Badge.Should().BeNull();
        notification.ClickAction.Should().BeNull();
    }

    [Fact]
    public void PushNotification_WithAllProperties_PreservesValues()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["orderId"] = "ord-1",
            ["deepLink"] = "app://orders/1",
        };

        // Act
        var notification = new PushNotification
        {
            Title = "Order shipped",
            Body = "Your order is on its way.",
            Data = data,
            Sound = "default",
            Badge = 3,
            ClickAction = "OPEN_ORDER",
        };

        // Assert
        notification.Title.Should().Be("Order shipped");
        notification.Body.Should().Be("Your order is on its way.");
        notification.Data.Should().BeEquivalentTo(data);
        notification.Sound.Should().Be("default");
        notification.Badge.Should().Be(3);
        notification.ClickAction.Should().Be("OPEN_ORDER");
    }

    [Fact]
    public void PushNotification_RecordEquality_TwoIdenticalNotifications_AreEqual()
    {
        // Arrange — share the Data dictionary so reference equality holds for that property
        var data = new Dictionary<string, string> { ["k"] = "v" };
        var first = new PushNotification { Title = "T", Body = "B", Data = data };
        var second = new PushNotification { Title = "T", Body = "B", Data = data };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void PushNotification_RecordEquality_DifferingTitle_AreNotEqual()
    {
        // Arrange
        var data = new Dictionary<string, string>();
        var first = new PushNotification { Title = "A", Body = "B", Data = data };
        var second = new PushNotification { Title = "X", Body = "B", Data = data };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void PushNotification_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new PushNotification { Title = "Hello", Body = "World", Badge = 1 };

        // Act
        var updated = original with { Badge = 5 };

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Badge.Should().Be(5);
        original.Badge.Should().Be(1);
        updated.Title.Should().Be(original.Title);
        updated.Body.Should().Be(original.Body);
    }
}
