// -----------------------------------------------------------------------
// <copyright file="PresenceMemberTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Tests.Models;

public class PresenceMemberTests
{
    [Fact]
    public void Ctor_AssignsIdAndInfo()
    {
        // Arrange
        var info = new Dictionary<string, object> { ["status"] = "online" };

        // Act
        var member = new PresenceMember("user-1", info);

        // Assert
        member.Id.Should().Be("user-1");
        member.Info.Should().BeSameAs(info);
    }

    [Fact]
    public void Equality_SameIdAndInfo_AreEqual()
    {
        // Arrange
        var info = new Dictionary<string, object> { ["k"] = 1 };
        var a = new PresenceMember("user-1", info);
        var b = new PresenceMember("user-1", info);

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentId_AreNotEqual()
    {
        // Arrange
        var info = new Dictionary<string, object>();
        var a = new PresenceMember("user-1", info);
        var b = new PresenceMember("user-2", info);

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void With_RebindsIdImmutably()
    {
        // Arrange
        var info = new Dictionary<string, object>();
        var original = new PresenceMember("user-1", info);

        // Act
        var rebound = original with { Id = "user-2" };

        // Assert
        original.Id.Should().Be("user-1");
        rebound.Id.Should().Be("user-2");
        rebound.Info.Should().BeSameAs(info);
    }
}
