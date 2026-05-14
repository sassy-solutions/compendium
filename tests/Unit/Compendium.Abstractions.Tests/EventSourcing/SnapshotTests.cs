// -----------------------------------------------------------------------
// <copyright file="SnapshotTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;

namespace Compendium.Abstractions.Tests.EventSourcing;

public class SnapshotTests
{
    private sealed class FakeState
    {
        public string Name { get; init; } = string.Empty;
        public int Counter { get; init; }
    }

    [Fact]
    public void Snapshot_PositionalConstruction_ExposesAllProperties()
    {
        // Arrange
        var state = new FakeState { Name = "alpha", Counter = 7 };
        var createdAt = DateTimeOffset.Parse("2026-05-10T12:34:56Z");

        // Act
        var snapshot = new Snapshot<FakeState>(state, 42L, createdAt, "tenant-1");

        // Assert
        snapshot.State.Should().BeSameAs(state);
        snapshot.Version.Should().Be(42L);
        snapshot.CreatedAt.Should().Be(createdAt);
        snapshot.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void Snapshot_TenantIdOmitted_DefaultsToNull()
    {
        // Arrange
        var state = new FakeState { Name = "beta" };
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var snapshot = new Snapshot<FakeState>(state, 1L, createdAt);

        // Assert
        snapshot.TenantId.Should().BeNull();
    }

    [Fact]
    public void Snapshot_TwoEqualValues_AreEqualByRecordSemantics()
    {
        // Arrange
        var state = new FakeState { Name = "shared", Counter = 1 };
        var createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var a = new Snapshot<FakeState>(state, 5L, createdAt, "t-1");
        var b = new Snapshot<FakeState>(state, 5L, createdAt, "t-1");

        // Act / Assert
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Snapshot_DifferentVersions_AreNotEqual()
    {
        // Arrange
        var state = new FakeState();
        var createdAt = DateTimeOffset.UtcNow;
        var a = new Snapshot<FakeState>(state, 1L, createdAt);
        var b = new Snapshot<FakeState>(state, 2L, createdAt);

        // Act / Assert
        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Snapshot_DifferentTenantIds_AreNotEqual()
    {
        // Arrange
        var state = new FakeState();
        var createdAt = DateTimeOffset.UtcNow;
        var a = new Snapshot<FakeState>(state, 1L, createdAt, "t-1");
        var b = new Snapshot<FakeState>(state, 1L, createdAt, "t-2");

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Snapshot_WithExpression_ProducesNonDestructiveCopy()
    {
        // Arrange
        var state = new FakeState { Name = "original" };
        var createdAt = DateTimeOffset.UtcNow;
        var original = new Snapshot<FakeState>(state, 10L, createdAt, "t-1");

        // Act
        var copy = original with { Version = 11L };

        // Assert
        copy.Version.Should().Be(11L);
        copy.State.Should().BeSameAs(state);
        copy.CreatedAt.Should().Be(createdAt);
        copy.TenantId.Should().Be("t-1");
        original.Version.Should().Be(10L);
    }

    [Fact]
    public void Snapshot_ToString_ContainsTypeAndPropertyNames()
    {
        // Arrange
        var state = new FakeState { Name = "tostring" };
        var snapshot = new Snapshot<FakeState>(state, 3L, DateTimeOffset.UtcNow, "tenant-x");

        // Act
        var rendered = snapshot.ToString();

        // Assert
        rendered.Should().Contain("Snapshot");
        rendered.Should().Contain("Version");
        rendered.Should().Contain("TenantId");
    }
}
