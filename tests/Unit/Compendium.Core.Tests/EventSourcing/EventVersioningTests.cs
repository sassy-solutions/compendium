// -----------------------------------------------------------------------
// <copyright file="EventVersioningTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing;

namespace Compendium.Core.Tests.EventSourcing;

/// <summary>
/// Unit tests for event versioning and migration functionality.
/// Tests IEventUpcaster, EventUpcasterBase, and EventVersionMigrator.
/// </summary>
public sealed class EventVersioningTests
{
    [Fact]
    public void EventVersionMigrator_RegisterUpcaster_AddsUpcasterToRegistry()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        var upcaster = new TestEventV1ToV2Upcaster();

        // Act
        migrator.RegisterUpcaster(upcaster);

        // Assert
        var registeredUpcasters = migrator.GetRegisteredUpcasters();
        Assert.Single(registeredUpcasters);
        Assert.Contains(upcaster, registeredUpcasters);
    }

    [Fact]
    public void EventVersionMigrator_MigrateToLatest_WithNoUpcastersRegistered_ReturnsOriginalEvent()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        var @event = new TestEventV1("test-id", "Test data");

        // Act
        var result = migrator.MigrateToLatest(@event);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Same(@event, result.Value);
    }

    [Fact]
    public void EventVersionMigrator_MigrateToLatest_WithSingleUpcaster_MigratesCorrectly()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());

        var v1Event = new TestEventV1("test-id", "Test data");

        // Act
        var result = migrator.MigrateToLatest(v1Event);

        // Assert
        Assert.True(result.IsSuccess);
        var v2Event = Assert.IsType<TestEventV2>(result.Value);
        Assert.Equal(2, v2Event.EventVersion);
        Assert.Equal(v1Event.AggregateId, v2Event.AggregateId);
        Assert.Equal(v1Event.Data, v2Event.Data);
        Assert.Equal("default-category", v2Event.Category); // New field
    }

    [Fact]
    public void EventVersionMigrator_MigrateToLatest_WithMultipleUpcasters_AppliesChainCorrectly()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());
        migrator.RegisterUpcaster(new TestEventV2ToV3Upcaster());

        var v1Event = new TestEventV1("test-id", "Test data");

        // Act
        var result = migrator.MigrateToLatest(v1Event);

        // Assert
        Assert.True(result.IsSuccess);
        var v3Event = Assert.IsType<TestEventV3>(result.Value);
        Assert.Equal(3, v3Event.EventVersion);
        Assert.Equal(v1Event.AggregateId, v3Event.AggregateId);
        Assert.Equal(v1Event.Data, v3Event.Data);
        Assert.Equal("default-category", v3Event.Category);
    }

    [Fact]
    public void EventVersionMigrator_MigrateToLatest_WithIncompleteChain_MigratesToMaximumVersion()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        // Only register V1->V2, but skip V2->V3
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());
        // V2->V3 is NOT registered
        migrator.RegisterUpcaster(new TestEventV3ToV4Upcaster()); // Register V3->V4 (won't be reached)

        var v1Event = new TestEventV1("test-id", "Test data");

        // Act
        var result = migrator.MigrateToLatest(v1Event);

        // Assert: Should migrate to V2 and stop (can't go further)
        Assert.True(result.IsSuccess);
        var v2Event = Assert.IsType<TestEventV2>(result.Value);
        Assert.Equal(2, v2Event.EventVersion);
    }

    [Fact]
    public void EventVersionMigrator_GetLatestVersion_WithRegisteredUpcasters_ReturnsCorrectVersion()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());
        migrator.RegisterUpcaster(new TestEventV2ToV3Upcaster());

        // Act
        var latestVersion = migrator.GetLatestVersion(typeof(TestEventV3));

        // Assert
        Assert.NotNull(latestVersion);
        Assert.Equal(3, latestVersion.Value);
    }

    [Fact]
    public void EventVersionMigrator_HasUpcasterForVersion_WithRegisteredUpcaster_ReturnsTrue()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());

        // Act
        var hasUpcaster = migrator.HasUpcasterForVersion(typeof(TestEventV1), 1);

        // Assert
        Assert.True(hasUpcaster);
    }

    [Fact]
    public void EventVersionMigrator_MigrateToLatest_WithEventAlreadyAtLatestVersion_ReturnsOriginalEvent()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());

        var v2Event = new TestEventV2("test-id", "Test data", "custom-category");

        // Act
        var result = migrator.MigrateToLatest(v2Event);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Same(v2Event, result.Value);
    }

    [Fact]
    public void EventVersionMigrator_MigrateToLatest_WithLargeEventBatch_CompletesSuccessfully()
    {
        // Arrange
        var migrator = new EventVersionMigrator();
        migrator.RegisterUpcaster(new TestEventV1ToV2Upcaster());
        migrator.RegisterUpcaster(new TestEventV2ToV3Upcaster());

        var events = Enumerable.Range(0, 1000).Select(i =>
            new TestEventV1($"test-id-{i}", $"Test data {i}")).ToList();

        // Act
        var results = events.Select(e => migrator.MigrateToLatest(e)).ToList();

        // Assert
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.All(results, r => Assert.IsType<TestEventV3>(r.Value));
        Assert.All(results, r => Assert.Equal(3, r.Value.EventVersion));
    }
}

// Test event classes
internal sealed class TestEventV1 : DomainEventBase
{
    public TestEventV1(string aggregateId, string data)
        : base(aggregateId, "TestAggregate", 0, eventVersion: 1)
    {
        Data = data;
    }

    public string Data { get; private init; }
}

internal sealed class TestEventV2 : DomainEventBase
{
    public TestEventV2(string aggregateId, string data, string category)
        : base(aggregateId, "TestAggregate", 0, eventVersion: 2)
    {
        Data = data;
        Category = category;
    }

    public string Data { get; private init; }
    public string Category { get; private init; }
}

internal sealed class TestEventV3 : DomainEventBase
{
    public TestEventV3(string aggregateId, string data, string category, DateTime timestamp)
        : base(aggregateId, "TestAggregate", 0, eventVersion: 3)
    {
        Data = data;
        Category = category;
        Timestamp = timestamp;
    }

    public string Data { get; private init; }
    public string Category { get; private init; }
    public DateTime Timestamp { get; private init; }
}

internal sealed class TestEventV4 : DomainEventBase
{
    public TestEventV4(string aggregateId, string data, string category, DateTime timestamp, int priority)
        : base(aggregateId, "TestAggregate", 0, eventVersion: 4)
    {
        Data = data;
        Category = category;
        Timestamp = timestamp;
        Priority = priority;
    }

    public string Data { get; private init; }
    public string Category { get; private init; }
    public DateTime Timestamp { get; private init; }
    public int Priority { get; private init; }
}

// Test upcasters
internal sealed class TestEventV1ToV2Upcaster : EventUpcasterBase<TestEventV1, TestEventV2>
{
    public override int SourceVersion => 1;
    public override int TargetVersion => 2;

    public override TestEventV2 Upcast(TestEventV1 sourceEvent)
    {
        return new TestEventV2(
            sourceEvent.AggregateId,
            sourceEvent.Data,
            "default-category");
    }
}

internal sealed class TestEventV2ToV3Upcaster : EventUpcasterBase<TestEventV2, TestEventV3>
{
    public override int SourceVersion => 2;
    public override int TargetVersion => 3;

    public override TestEventV3 Upcast(TestEventV2 sourceEvent)
    {
        return new TestEventV3(
            sourceEvent.AggregateId,
            sourceEvent.Data,
            sourceEvent.Category,
            DateTime.UtcNow);
    }
}

internal sealed class TestEventV3ToV4Upcaster : EventUpcasterBase<TestEventV3, TestEventV4>
{
    public override int SourceVersion => 3;
    public override int TargetVersion => 4;

    public override TestEventV4 Upcast(TestEventV3 sourceEvent)
    {
        return new TestEventV4(
            sourceEvent.AggregateId,
            sourceEvent.Data,
            sourceEvent.Category,
            sourceEvent.Timestamp,
            priority: 0);
    }
}
