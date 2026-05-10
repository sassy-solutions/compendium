// -----------------------------------------------------------------------
// <copyright file="SubscriberContextTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Tests.Models;

public class SubscriberContextTests
{
    [Fact]
    public void Ctor_WithUserAndTenant_DefaultsInfoToNull()
    {
        // Arrange / Act
        var ctx = new SubscriberContext("user-1", "tenant-1");

        // Assert
        ctx.UserId.Should().Be("user-1");
        ctx.TenantId.Should().Be("tenant-1");
        ctx.Info.Should().BeNull();
    }

    [Fact]
    public void Ctor_WithInfo_AssignsInfoReference()
    {
        // Arrange
        var info = new Dictionary<string, object> { ["name"] = "Ada" };

        // Act
        var ctx = new SubscriberContext("user-1", "tenant-1", info);

        // Assert
        ctx.Info.Should().BeSameAs(info);
        ctx.Info!["name"].Should().Be("Ada");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var a = new SubscriberContext("user-1", "tenant-1");
        var b = new SubscriberContext("user-1", "tenant-1");

        // Act / Assert
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentUserId_AreNotEqual()
    {
        // Arrange
        var a = new SubscriberContext("user-1", "tenant-1");
        var b = new SubscriberContext("user-2", "tenant-1");

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void With_RebindsTenantIdImmutably()
    {
        // Arrange
        var original = new SubscriberContext("user-1", "tenant-1");

        // Act
        var rebound = original with { TenantId = "tenant-2" };

        // Assert
        original.TenantId.Should().Be("tenant-1");
        rebound.TenantId.Should().Be("tenant-2");
        rebound.UserId.Should().Be("user-1");
    }
}
