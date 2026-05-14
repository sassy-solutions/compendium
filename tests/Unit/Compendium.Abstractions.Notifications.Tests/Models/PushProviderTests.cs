// -----------------------------------------------------------------------
// <copyright file="PushProviderTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class PushProviderTests
{
    [Theory]
    [InlineData(PushProvider.FCM, 0)]
    [InlineData(PushProvider.APNS, 1)]
    [InlineData(PushProvider.WebPush, 2)]
    public void PushProvider_HasStableUnderlyingValues(PushProvider provider, int expected)
    {
        // Act
        var value = (int)provider;

        // Assert
        value.Should().Be(expected);
    }

    [Fact]
    public void PushProvider_DefinesExactlyThreeMembers()
    {
        // Act
        var members = Enum.GetValues<PushProvider>();

        // Assert
        members.Should().BeEquivalentTo(new[]
        {
            PushProvider.FCM,
            PushProvider.APNS,
            PushProvider.WebPush,
        });
    }

    [Theory]
    [InlineData("FCM", PushProvider.FCM)]
    [InlineData("APNS", PushProvider.APNS)]
    [InlineData("WebPush", PushProvider.WebPush)]
    public void PushProvider_Parse_ReturnsExpectedMember(string name, PushProvider expected)
    {
        // Act
        var parsed = Enum.Parse<PushProvider>(name);

        // Assert
        parsed.Should().Be(expected);
    }
}
