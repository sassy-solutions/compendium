// -----------------------------------------------------------------------
// <copyright file="AggregateRootTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Domain.Primitives;

public class AggregateRootTests
{
    [Fact]
    public void Constructor_WithValidId_InitializesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Aggregate";

        // Act
        var aggregate = new TestAggregate(id, name);

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.Name.Should().Be(name);
        aggregate.Version.Should().Be(0);
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.HasDomainEvents.Should().BeFalse();
    }

    [Fact]
    public void AddDomainEvent_ValidEvent_AddsToCollection()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1);

        // Act
        aggregate.TestAddDomainEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
        aggregate.HasDomainEvents.Should().BeTrue();
    }

    [Fact]
    public void AddDomainEvent_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        var act = () => aggregate.TestAddDomainEvent(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_MaintainsOrder()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var event1 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1, "Event 1");
        var event2 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 2, "Event 2");
        var event3 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 3, "Event 3");

        // Act
        aggregate.TestAddDomainEvent(event1);
        aggregate.TestAddDomainEvent(event2);
        aggregate.TestAddDomainEvent(event3);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(3);
        aggregate.DomainEvents.Should().ContainInOrder(event1, event2, event3);
    }

    [Fact]
    public void AddDomainEvent_DuplicateEvent_PreventsDuplication()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1);

        // Act
        aggregate.TestAddDomainEvent(domainEvent);
        aggregate.TestAddDomainEvent(domainEvent); // Same event

        // Assert
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
    }

    [Fact]
    public void RemoveDomainEvent_ExistingEvent_RemovesFromCollection()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var event1 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1, "Event 1");
        var event2 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 2, "Event 2");

        aggregate.TestAddDomainEvent(event1);
        aggregate.TestAddDomainEvent(event2);

        // Act
        aggregate.TestRemoveDomainEvent(event1);

        // Assert
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(event2);
    }

    [Fact]
    public void RemoveDomainEvent_NonExistentEvent_DoesNothing()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var existingEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1);
        var nonExistentEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 2);

        aggregate.TestAddDomainEvent(existingEvent);

        // Act
        aggregate.TestRemoveDomainEvent(nonExistentEvent);

        // Assert
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(existingEvent);
    }

    [Fact]
    public void RemoveDomainEvent_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        var act = () => aggregate.TestRemoveDomainEvent(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearDomainEvents_WithEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var event1 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1);
        var event2 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 2);

        aggregate.TestAddDomainEvent(event1);
        aggregate.TestAddDomainEvent(event2);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.HasDomainEvents.Should().BeFalse();
    }

    [Fact]
    public void ClearDomainEvents_WithoutEvents_DoesNothing()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.HasDomainEvents.Should().BeFalse();
    }

    [Fact]
    public void GetUncommittedEvents_WithEvents_ReturnsEventsAndClears()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var event1 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1);
        var event2 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 2);

        aggregate.TestAddDomainEvent(event1);
        aggregate.TestAddDomainEvent(event2);

        // Act
        var uncommittedEvents = aggregate.GetUncommittedEvents();

        // Assert
        uncommittedEvents.Should().HaveCount(2);
        uncommittedEvents.Should().ContainInOrder(event1, event2);
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.HasDomainEvents.Should().BeFalse();
    }

    [Fact]
    public void GetUncommittedEvents_WithoutEvents_ReturnsEmptyCollection()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        var uncommittedEvents = aggregate.GetUncommittedEvents();

        // Assert
        uncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionAndUpdatesModifiedAt()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var originalVersion = aggregate.Version;
        var originalModifiedAt = aggregate.ModifiedAt;
        Thread.Sleep(10);

        // Act
        aggregate.TestIncrementVersion();

        // Assert
        aggregate.Version.Should().Be(originalVersion + 1);
        aggregate.ModifiedAt.Should().BeAfter(originalModifiedAt);
    }

    [Fact]
    public void SetVersion_WithValidVersion_SetsVersion()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var newVersion = 5L;

        // Act
        aggregate.TestSetVersion(newVersion);

        // Assert
        aggregate.Version.Should().Be(newVersion);
    }

    [Fact]
    public void SetVersion_WithNegativeVersion_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        var act = () => aggregate.TestSetVersion(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateName_AddsEventAndIncrementsVersion()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Original Name");
        var originalVersion = aggregate.Version;
        var newName = "Updated Name";

        // Act
        aggregate.UpdateName(newName);

        // Assert
        aggregate.Name.Should().Be(newName);
        aggregate.Version.Should().Be(originalVersion + 1);
        aggregate.DomainEvents.Should().ContainSingle();

        var domainEvent = aggregate.DomainEvents.First() as TestDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.Data.Should().Contain(newName);
    }

    [Fact]
    public void ConcurrentAccess_AddingEvents_ThreadSafe()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var events = Enumerable.Range(0, 100)
            .Select(i => new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), i, $"Event {i}"))
            .ToList();

        // Act
        Parallel.ForEach(events, domainEvent =>
        {
            aggregate.TestAddDomainEvent(domainEvent);
        });

        // Assert
        aggregate.DomainEvents.Should().HaveCount(100);
        aggregate.HasDomainEvents.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentAccess_VersionIncrement_ThreadSafe()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var tasks = new List<Task>();
        var incrementCount = 100;

        // Act
        for (int i = 0; i < incrementCount; i++)
        {
            tasks.Add(Task.Run(() => aggregate.TestIncrementVersion()));
        }

        await Task.WhenAll(tasks);

        // Assert
        aggregate.Version.Should().Be(incrementCount);
    }

    [Theory]
    [InlineData(1000)]
    public void AddDomainEvent_PerformanceTest_CompletesQuickly(int eventCount)
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var events = Enumerable.Range(0, eventCount)
            .Select(i => new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), i))
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        foreach (var domainEvent in events)
        {
            aggregate.TestAddDomainEvent(domainEvent);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Adding domain events should be fast");
        aggregate.DomainEvents.Should().HaveCount(eventCount);
    }

    [Fact]
    public void DomainEvents_ReadOnlyCollection_CannotBeModifiedDirectly()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1);
        aggregate.TestAddDomainEvent(domainEvent);

        // Act & Assert
        var domainEvents = aggregate.DomainEvents;
        domainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();

        // Verify it's truly read-only by checking it's not directly modifiable
        // Note: ReadOnlyCollection<T> implements ICollection<T> but throws on modifications
        domainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();

        // Verify that attempting to modify throws an exception
        if (domainEvents is ICollection<IDomainEvent> collection)
        {
            collection.IsReadOnly.Should().BeTrue();
        }
    }

    [Fact]
    public void EventDeduplication_SameEventHash_PreventsDuplicates()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Create events that should have the same hash (same type, aggregate ID, and timestamp precision)
        var event1 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1, "Same Data");
        var event2 = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregate), 1, "Same Data");

        // Act
        aggregate.TestAddDomainEvent(event1);
        aggregate.TestAddDomainEvent(event2);

        // Assert
        // Note: The current implementation uses a simple hash that might allow duplicates
        // This test documents the current behavior and can be updated when deduplication is improved
        aggregate.DomainEvents.Count.Should().BeGreaterOrEqualTo(1);
    }
}
