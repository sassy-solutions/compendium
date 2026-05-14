// -----------------------------------------------------------------------
// <copyright file="CrmEventTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Tests.Models;

public class CrmEventTests
{
    [Fact]
    public void CrmEvent_Construct_AssignsAllProperties()
    {
        // Arrange
        var when = new DateTimeOffset(2026, 5, 11, 10, 0, 0, TimeSpan.Zero);
        var props = new Dictionary<string, object> { ["plan"] = "pro" };

        // Act
        var evt = new CrmEvent(
            Name: "trial_started",
            ContactExternalId: "ext-1",
            Properties: props,
            Timestamp: when,
            TenantId: "tenant-1");

        // Assert
        evt.Name.Should().Be("trial_started");
        evt.ContactExternalId.Should().Be("ext-1");
        evt.Properties.Should().BeSameAs(props);
        evt.Timestamp.Should().Be(when);
        evt.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void CrmEvent_Construct_AllowsNullProperties()
    {
        // Arrange
        var when = DateTimeOffset.UtcNow;

        // Act
        var evt = new CrmEvent("event", "ext-1", null, when, "tenant-1");

        // Assert
        evt.Properties.Should().BeNull();
    }

    [Fact]
    public void CrmEvent_Equality_IsValueBased()
    {
        // Arrange
        var when = new DateTimeOffset(2026, 5, 11, 10, 0, 0, TimeSpan.Zero);
        var a = new CrmEvent("ev", "ext-1", null, when, "tenant-1");
        var b = new CrmEvent("ev", "ext-1", null, when, "tenant-1");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void CrmEvent_With_ChangesTimestampWithoutMutating()
    {
        // Arrange
        var when = DateTimeOffset.UtcNow;
        var evt = new CrmEvent("ev", "ext-1", null, when, "tenant-1");
        var later = when.AddMinutes(5);

        // Act
        var copy = evt with { Timestamp = later };

        // Assert
        evt.Timestamp.Should().Be(when);
        copy.Timestamp.Should().Be(later);
    }
}
