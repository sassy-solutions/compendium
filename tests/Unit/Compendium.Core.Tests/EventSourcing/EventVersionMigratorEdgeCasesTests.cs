// -----------------------------------------------------------------------
// <copyright file="EventVersionMigratorEdgeCasesTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing;

namespace Compendium.Core.Tests.EventSourcing;

/// <summary>
/// Edge-case coverage for <see cref="EventVersionMigrator"/>: re-registering an upcaster,
/// upcaster throwing, and circular-migration detection (max migrations exceeded).
/// </summary>
public sealed class EventVersionMigratorEdgeCasesTests
{
    private sealed class EdgeEventV1 : DomainEventBase
    {
        public EdgeEventV1(string aggregateId)
            : base(aggregateId, "EdgeEvent", 0, eventVersion: 1)
        {
        }
    }

    private sealed class EdgeEventV2 : DomainEventBase
    {
        public EdgeEventV2(string aggregateId)
            : base(aggregateId, "EdgeEvent", 0, eventVersion: 2)
        {
        }
    }

    private sealed class FirstUpcaster : EventUpcasterBase<EdgeEventV1, EdgeEventV2>
    {
        public override int SourceVersion => 1;

        public override int TargetVersion => 2;

        public override EdgeEventV2 Upcast(EdgeEventV1 sourceEvent) =>
            new(sourceEvent.AggregateId);
    }

    private sealed class ReplacementUpcaster : EventUpcasterBase<EdgeEventV1, EdgeEventV2>
    {
        public override int SourceVersion => 1;

        public override int TargetVersion => 2;

        public override EdgeEventV2 Upcast(EdgeEventV1 sourceEvent) =>
            new(sourceEvent.AggregateId);
    }

    private sealed class ThrowingUpcaster : EventUpcasterBase<EdgeEventV1, EdgeEventV2>
    {
        public override int SourceVersion => 1;

        public override int TargetVersion => 2;

        public override EdgeEventV2 Upcast(EdgeEventV1 sourceEvent) =>
            throw new InvalidOperationException("upcaster boom");
    }

    /// <summary>
    /// A perversely-circular upcaster: source and target are the same type/version, and Upcast
    /// returns an instance with EventVersion=1, so the migrator keeps applying it until it hits
    /// the maxMigrations safety net (100).
    /// </summary>
    private sealed class CircularUpcaster : EventUpcasterBase<EdgeEventV1, EdgeEventV1>
    {
        public override int SourceVersion => 1;

        public override int TargetVersion => 1;

        public override EdgeEventV1 Upcast(EdgeEventV1 sourceEvent) =>
            new(sourceEvent.AggregateId);
    }

    [Fact]
    public void RegisterUpcaster_TwiceForSameKey_LogsWarningAndReplaces()
    {
        // Arrange
        string? warning = null;
        var migrator = new EventVersionMigrator(logWarning: msg => warning = msg);
        migrator.RegisterUpcaster(new FirstUpcaster());

        // Act
        migrator.RegisterUpcaster(new ReplacementUpcaster());

        // Assert
        warning.Should().NotBeNull();
        warning.Should().Contain("already registered");
        // Only one (the replacement) should be in the registry — same key.
        migrator.GetRegisteredUpcasters().Should().HaveCount(1);
    }

    [Fact]
    public void MigrateToLatest_WithThrowingUpcaster_ReturnsFailureWithUpcastFailedCode()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new ThrowingUpcaster());

        // Act
        var result = migrator.MigrateToLatest(new EdgeEventV1("agg-1"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventVersionMigrator.UpcastFailed");
        result.Error.Message.Should().Contain("upcaster boom");
    }

    [Fact]
    public void MigrateToLatest_WithCircularUpcaster_ReturnsTooManyMigrationsFailure()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new CircularUpcaster());

        // Act
        var result = migrator.MigrateToLatest(new EdgeEventV1("agg-1"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventVersionMigrator.TooManyMigrations");
        result.Error.Message.Should().Contain("circular");
    }

    [Fact]
    public void RegisterUpcaster_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var migrator = new EventVersionMigrator();

        // Act
        var act = () => migrator.RegisterUpcaster(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MigrateToLatest_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var migrator = new EventVersionMigrator();

        // Act
        var act = () => migrator.MigrateToLatest(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetLatestVersion_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var migrator = new EventVersionMigrator();

        // Act
        var act = () => migrator.GetLatestVersion(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetLatestVersion_WithUnregisteredType_ReturnsNull()
    {
        // Arrange
        var migrator = new EventVersionMigrator();

        // Act
        var version = migrator.GetLatestVersion(typeof(EdgeEventV2));

        // Assert
        version.Should().BeNull();
    }

    [Fact]
    public void HasUpcasterForVersion_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var migrator = new EventVersionMigrator();

        // Act
        var act = () => migrator.HasUpcasterForVersion(null!, 1);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasUpcasterForVersion_WithUnregistered_ReturnsFalse()
    {
        // Arrange
        var migrator = new EventVersionMigrator();

        // Act
        var hasUpcaster = migrator.HasUpcasterForVersion(typeof(EdgeEventV1), 1);

        // Assert
        hasUpcaster.Should().BeFalse();
    }
}
