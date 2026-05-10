// -----------------------------------------------------------------------
// <copyright file="AnalyticsEventTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Analytics.Tests.Models;

public class AnalyticsEventTests
{
    [Fact]
    public void AnalyticsEvent_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
        var properties = new Dictionary<string, object>
        {
            ["plan"] = "pro",
            ["seats"] = 5,
        };

        // Act
        var evt = new AnalyticsEvent(
            Name: "subscription_upgraded",
            DistinctId: "user-42",
            TenantId: "tenant-acme",
            Timestamp: timestamp,
            Properties: properties);

        // Assert
        evt.Name.Should().Be("subscription_upgraded");
        evt.DistinctId.Should().Be("user-42");
        evt.TenantId.Should().Be("tenant-acme");
        evt.Timestamp.Should().Be(timestamp);
        evt.Properties.Should().BeSameAs(properties);
    }

    [Fact]
    public void AnalyticsEvent_Equality_ShouldHoldForSameValues()
    {
        // Arrange
        var timestamp = DateTimeOffset.UnixEpoch;
        var properties = new Dictionary<string, object> { ["k"] = "v" };
        var a = new AnalyticsEvent("evt", "did", "tid", timestamp, properties);
        var b = new AnalyticsEvent("evt", "did", "tid", timestamp, properties);

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void AnalyticsEvent_Equality_ShouldDifferWhenNameChanges()
    {
        // Arrange
        var timestamp = DateTimeOffset.UnixEpoch;
        var properties = new Dictionary<string, object> { ["k"] = "v" };
        var a = new AnalyticsEvent("evt-a", "did", "tid", timestamp, properties);
        var b = new AnalyticsEvent("evt-b", "did", "tid", timestamp, properties);

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void AnalyticsEvent_With_ShouldProduceModifiedCopy()
    {
        // Arrange
        var timestamp = DateTimeOffset.UnixEpoch;
        var properties = new Dictionary<string, object> { ["k"] = "v" };
        var original = new AnalyticsEvent("evt", "did", "tid", timestamp, properties);

        // Act
        var modified = original with { TenantId = "other-tenant" };

        // Assert
        modified.TenantId.Should().Be("other-tenant");
        original.TenantId.Should().Be("tid");
        modified.Should().NotBe(original);
    }

    [Fact]
    public void AnalyticsEvent_Properties_ShouldSupportEmptyDictionary()
    {
        // Arrange
        IReadOnlyDictionary<string, object> properties =
            new Dictionary<string, object>();

        // Act
        var evt = new AnalyticsEvent(
            "evt",
            "did",
            "tid",
            DateTimeOffset.UnixEpoch,
            properties);

        // Assert
        evt.Properties.Should().BeEmpty();
    }
}
