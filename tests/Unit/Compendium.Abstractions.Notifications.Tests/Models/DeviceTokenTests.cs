// -----------------------------------------------------------------------
// <copyright file="DeviceTokenTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class DeviceTokenTests
{
    [Fact]
    public void DeviceToken_WithRequiredOnly_LeavesUserIdNull()
    {
        // Arrange / Act
        var token = new DeviceToken
        {
            Provider = PushProvider.FCM,
            Token = "abc",
            TenantId = "tenant-1",
        };

        // Assert
        token.Provider.Should().Be(PushProvider.FCM);
        token.Token.Should().Be("abc");
        token.TenantId.Should().Be("tenant-1");
        token.UserId.Should().BeNull();
    }

    [Theory]
    [InlineData(PushProvider.FCM)]
    [InlineData(PushProvider.APNS)]
    [InlineData(PushProvider.WebPush)]
    public void DeviceToken_WithEachProvider_PreservesProvider(PushProvider provider)
    {
        // Arrange / Act
        var token = new DeviceToken
        {
            Provider = provider,
            Token = "tok",
            TenantId = "t",
            UserId = "u",
        };

        // Assert
        token.Provider.Should().Be(provider);
        token.UserId.Should().Be("u");
    }

    [Fact]
    public void DeviceToken_RecordEquality_TwoIdenticalTokens_AreEqual()
    {
        // Arrange
        var first = new DeviceToken { Provider = PushProvider.APNS, Token = "t", TenantId = "tn", UserId = "u" };
        var second = new DeviceToken { Provider = PushProvider.APNS, Token = "t", TenantId = "tn", UserId = "u" };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void DeviceToken_RecordEquality_DifferingTenant_AreNotEqual()
    {
        // Arrange
        var first = new DeviceToken { Provider = PushProvider.FCM, Token = "t", TenantId = "a" };
        var second = new DeviceToken { Provider = PushProvider.FCM, Token = "t", TenantId = "b" };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void DeviceToken_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new DeviceToken { Provider = PushProvider.FCM, Token = "t", TenantId = "tn" };

        // Act
        var updated = original with { UserId = "u-1" };

        // Assert
        updated.UserId.Should().Be("u-1");
        original.UserId.Should().BeNull();
    }
}
