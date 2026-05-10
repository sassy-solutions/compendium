// -----------------------------------------------------------------------
// <copyright file="SmsStatusTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class SmsStatusTests
{
    [Theory]
    [InlineData(SmsStatus.Queued, 0)]
    [InlineData(SmsStatus.Sent, 1)]
    [InlineData(SmsStatus.Delivered, 2)]
    [InlineData(SmsStatus.Failed, 3)]
    [InlineData(SmsStatus.Undelivered, 4)]
    public void SmsStatus_HasStableUnderlyingValues(SmsStatus status, int expected)
    {
        // Act
        var value = (int)status;

        // Assert
        value.Should().Be(expected);
    }

    [Fact]
    public void SmsStatus_DefinesExactlyFiveMembers()
    {
        // Act
        var members = Enum.GetValues<SmsStatus>();

        // Assert
        members.Should().BeEquivalentTo(new[]
        {
            SmsStatus.Queued,
            SmsStatus.Sent,
            SmsStatus.Delivered,
            SmsStatus.Failed,
            SmsStatus.Undelivered,
        });
    }

    [Theory]
    [InlineData("Queued", SmsStatus.Queued)]
    [InlineData("Sent", SmsStatus.Sent)]
    [InlineData("Delivered", SmsStatus.Delivered)]
    [InlineData("Failed", SmsStatus.Failed)]
    [InlineData("Undelivered", SmsStatus.Undelivered)]
    public void SmsStatus_Parse_ReturnsExpectedMember(string name, SmsStatus expected)
    {
        // Act
        var parsed = Enum.Parse<SmsStatus>(name);

        // Assert
        parsed.Should().Be(expected);
    }
}
