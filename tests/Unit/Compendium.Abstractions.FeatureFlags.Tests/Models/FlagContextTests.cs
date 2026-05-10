// -----------------------------------------------------------------------
// <copyright file="FlagContextTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.FeatureFlags.Tests.Models;

public class FlagContextTests
{
    [Fact]
    public void Constructor_WithTenantOnly_DefaultsUserAndAttributes()
    {
        // Act
        var ctx = new FlagContext("tenant-1");

        // Assert
        ctx.TenantId.Should().Be("tenant-1");
        ctx.UserId.Should().BeNull();
        ctx.Attributes.Should().NotBeNull();
        ctx.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAllArguments_AssignsEveryProperty()
    {
        // Arrange
        var attributes = new Dictionary<string, object>
        {
            ["plan"] = "pro",
            ["country"] = "FR",
        };

        // Act
        var ctx = new FlagContext("tenant-9", "user-42", attributes);

        // Assert
        ctx.TenantId.Should().Be("tenant-9");
        ctx.UserId.Should().Be("user-42");
        ctx.Attributes.Should().BeSameAs(attributes);
        ctx.Attributes.Should().HaveCount(2);
        ctx.Attributes["plan"].Should().Be("pro");
    }

    [Fact]
    public void Constructor_WithNullAttributes_FallsBackToEmptyDictionary()
    {
        // Act
        var ctx = new FlagContext("tenant-1", "user-1", attributes: null);

        // Assert
        ctx.Attributes.Should().NotBeNull();
        ctx.Attributes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithBlankTenantId_Throws(string? tenantId)
    {
        // Act
        var act = () => new FlagContext(tenantId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("tenantId");
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        // Arrange
        var attributes = new Dictionary<string, object> { ["k"] = "v" };

        // Act
        var a = new FlagContext("tenant-1", "user-1", attributes);
        var b = new FlagContext("tenant-1", "user-1", attributes);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Records_WithDifferentTenant_AreNotEqual()
    {
        // Act
        var a = new FlagContext("tenant-1");
        var b = new FlagContext("tenant-2");

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Records_WithDifferentUser_AreNotEqual()
    {
        // Act
        var a = new FlagContext("tenant-1", "user-1");
        var b = new FlagContext("tenant-1", "user-2");

        // Assert
        a.Should().NotBe(b);
    }
}
