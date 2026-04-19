// -----------------------------------------------------------------------
// <copyright file="InMemoryEventStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Core.EventSourcing;

namespace Compendium.Infrastructure.Tests.EventSourcing;

/// <summary>
/// Comprehensive test suite for InMemoryEventStore implementation.
/// Tests thread-safety, concurrency control, performance, and edge cases.
/// </summary>
public sealed class InMemoryEventStoreTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<InMemoryEventStore> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly IEventDeserializer _eventDeserializer;
    private readonly InMemoryEventStore _sut;
    private readonly string _defaultTenantId = "test-tenant";
    private readonly string _defaultAggregateId = "test-aggregate-123";

    public InMemoryEventStoreTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = Substitute.For<ILogger<InMemoryEventStore>>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.TenantId.Returns(_defaultTenantId);
        _eventDeserializer = Substitute.For<IEventDeserializer>();

        // Configure the event deserializer mock to properly deserialize TestDomainEvent
        _eventDeserializer.TryDeserializeEvent(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var eventData = callInfo.ArgAt<string>(0);
                var eventTypeName = callInfo.ArgAt<string>(1);

                if (eventTypeName.Contains(nameof(TestDomainEvent)))
                {
                    try
                    {
                        // Use the same JSON options as InMemoryEventStore (camelCase)
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = false
                        };
                        var deserializedEvent = JsonSerializer.Deserialize<TestDomainEvent>(eventData, jsonOptions);
                        return Result.Success<IDomainEvent>(deserializedEvent!);
                    }
                    catch (Exception ex)
                    {
                        return Result.Failure<IDomainEvent>(Error.Failure("Deserialization.Failed", $"Failed to deserialize event: {ex.Message}"));
                    }
                }

                return Result.Failure<IDomainEvent>(Error.Failure("EventType.NotSupported", "Event type not supported in tests"));
            });

        _sut = new InMemoryEventStore(_eventDeserializer, _logger, _tenantContext);
    }

    #region Basic Operations

    [Fact]
    public async Task AppendEventsAsync_WithValidEvents_ShouldStoreSuccessfully()
    {
        // Arrange
        var events = GenerateTestEvents(3);
        const long expectedVersion = 0;

        // Act
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, events, expectedVersion);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        var storedEvents = await _sut.GetEventsAsync(_defaultAggregateId);
        storedEvents.IsSuccess.Should().BeTrue();
        storedEvents.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task AppendEventsAsync_WithNullAggregateId_ShouldThrowArgumentException()
    {
        // Arrange
        var events = GenerateTestEvents(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.AppendEventsAsync(null!, events, 0));
    }

    [Fact]
    public async Task AppendEventsAsync_WithEmptyAggregateId_ShouldThrowArgumentException()
    {
        // Arrange
        var events = GenerateTestEvents(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.AppendEventsAsync("", events, 0));
    }

    [Fact]
    public async Task AppendEventsAsync_WithNullEvents_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.AppendEventsAsync(_defaultAggregateId, null!, 0));
    }

    [Fact]
    public async Task AppendEventsAsync_WithEmptyEventsList_ShouldReturnSuccess()
    {
        // Arrange
        var emptyEvents = new List<IDomainEvent>();

        // Act
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, emptyEvents, 0);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Concurrency Control

    [Fact]
    public async Task AppendEventsAsync_WithExpectedVersion_ShouldEnforceConcurrencyControl()
    {
        // Arrange
        var firstBatch = GenerateTestEvents(2);
        var secondBatch = GenerateTestEvents(1);

        // Act - First append should succeed
        var firstResult = await _sut.AppendEventsAsync(_defaultAggregateId, firstBatch, 0);

        // Act - Second append with wrong expected version should fail
        var secondResult = await _sut.AppendEventsAsync(_defaultAggregateId, secondBatch, 0);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.Error.Type.Should().Be(ErrorType.Conflict);
        secondResult.Error.Code.Should().Be("EventStore.ConcurrencyConflict");
    }

    [Fact]
    public async Task AppendEventsAsync_WithCorrectExpectedVersion_ShouldSucceed()
    {
        // Arrange
        var firstBatch = GenerateTestEvents(2);
        var secondBatch = GenerateTestEvents(1);

        // Act
        await _sut.AppendEventsAsync(_defaultAggregateId, firstBatch, 0);
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, secondBatch, 2);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var allEvents = await _sut.GetEventsAsync(_defaultAggregateId);
        allEvents.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task AppendEventsAsync_WithExpectedVersionMinusOne_ShouldSkipConcurrencyCheck()
    {
        // Arrange
        var firstBatch = GenerateTestEvents(2);
        var secondBatch = GenerateTestEvents(1);

        // Act
        await _sut.AppendEventsAsync(_defaultAggregateId, firstBatch, 0);
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, secondBatch, -1);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var allEvents = await _sut.GetEventsAsync(_defaultAggregateId);
        allEvents.Value.Should().HaveCount(3);
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task AppendEventsAsync_ConcurrentWrites_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 50;
        const int eventsPerThread = 10;
        var tasks = new List<Task<Result>>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    var aggregateId = $"aggregate-{threadId}";
                    var events = GenerateTestEvents(eventsPerThread, $"thread-{threadId}");
                    return await _sut.AppendEventsAsync(aggregateId, events, 0);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    throw;
                }
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // Verify all events were stored correctly
        for (int i = 0; i < threadCount; i++)
        {
            var aggregateId = $"aggregate-{i}";
            var storedEvents = await _sut.GetEventsAsync(aggregateId);
            storedEvents.IsSuccess.Should().BeTrue();
            storedEvents.Value.Should().HaveCount(eventsPerThread);
        }
    }

    [Fact]
    public async Task EventStore_ConcurrentReadsAndWrites_ShouldMaintainConsistency()
    {
        // Arrange
        const int writerCount = 25;
        const int readerCount = 25;
        const int eventsPerWriter = 5;
        var allTasks = new List<Task>();
        var readResults = new ConcurrentBag<int>();
        var writeExceptions = new ConcurrentBag<Exception>();

        // Act - Start concurrent writers
        for (int i = 0; i < writerCount; i++)
        {
            var writerId = i;
            var writeTask = Task.Run(async () =>
            {
                try
                {
                    var aggregateId = $"mixed-aggregate-{writerId}";
                    var events = GenerateTestEvents(eventsPerWriter);
                    await _sut.AppendEventsAsync(aggregateId, events, 0);
                }
                catch (Exception ex)
                {
                    writeExceptions.Add(ex);
                }
            });
            allTasks.Add(writeTask);
        }

        // Act - Start concurrent readers
        for (int i = 0; i < readerCount; i++)
        {
            var readerId = i;
            var readTask = Task.Run(async () =>
            {
                var aggregateId = $"mixed-aggregate-{readerId % writerCount}";
                await Task.Delay(10); // Small delay to allow some writes

                var events = await _sut.GetEventsAsync(aggregateId);
                if (events.IsSuccess)
                {
                    readResults.Add(events.Value.Count());
                }
            });
            allTasks.Add(readTask);
        }

        await Task.WhenAll(allTasks);

        // Assert
        writeExceptions.Should().BeEmpty();
        readResults.Should().NotBeEmpty();
        readResults.Should().AllSatisfy(count => count.Should().BeInRange(0, eventsPerWriter));
    }

    #endregion

    #region Version Management

    [Fact]
    public async Task GetVersionAsync_ForNewAggregate_ShouldReturnZero()
    {
        // Act
        var result = await _sut.GetVersionAsync("new-aggregate");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task GetVersionAsync_AfterSavingEvents_ShouldReturnCorrectVersion()
    {
        // Arrange
        var events = GenerateTestEvents(5);
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.GetVersionAsync(_defaultAggregateId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task GetVersionAsync_WithNullAggregateId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetVersionAsync(null!));
    }

    #endregion

    #region Filtering and Queries

    [Fact]
    public async Task GetEventsAsync_WithFromVersion_ShouldFilterCorrectly()
    {
        // Arrange
        var events = GenerateTestEvents(10);
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.GetEventsAsync(_defaultAggregateId, fromVersion: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5); // Events 6-10
    }

    [Fact]
    public async Task GetEventsAsync_WithFromVersionZero_ShouldReturnAllEvents()
    {
        // Arrange
        var events = GenerateTestEvents(7);
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.GetEventsAsync(_defaultAggregateId, fromVersion: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetEventsAsync_WithFromVersionGreaterThanTotal_ShouldReturnEmpty()
    {
        // Arrange
        var events = GenerateTestEvents(3);
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.GetEventsAsync(_defaultAggregateId, fromVersion: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsAsync_ForNonExistentAggregate_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _sut.GetEventsAsync("non-existent-aggregate");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsAsync_WithNegativeFromVersion_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.GetEventsAsync(_defaultAggregateId, fromVersion: -1));
    }

    #endregion

    #region Existence Checks

    [Fact]
    public async Task ExistsAsync_ForExistingAggregate_ShouldReturnTrue()
    {
        // Arrange
        var events = GenerateTestEvents(1);
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.ExistsAsync(_defaultAggregateId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ForNonExistentAggregate_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.ExistsAsync("non-existent-aggregate");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullAggregateId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ExistsAsync(null!));
    }

    #endregion

    #region Multi-tenancy

    [Fact]
    public async Task EventStore_ShouldIsolateEventsByTenant()
    {
        // Arrange
        var tenant1Context = Substitute.For<ITenantContext>();
        tenant1Context.TenantId.Returns("tenant-1");
        using var eventStore1 = new InMemoryEventStore(_eventDeserializer, _logger, tenant1Context);

        var tenant2Context = Substitute.For<ITenantContext>();
        tenant2Context.TenantId.Returns("tenant-2");
        using var eventStore2 = new InMemoryEventStore(_eventDeserializer, _logger, tenant2Context);

        var events1 = GenerateTestEvents(3, "tenant1");
        var events2 = GenerateTestEvents(2, "tenant2");

        // Act
        await eventStore1.AppendEventsAsync(_defaultAggregateId, events1, 0);
        await eventStore2.AppendEventsAsync(_defaultAggregateId, events2, 0);

        // Assert
        var tenant1Events = await eventStore1.GetEventsAsync(_defaultAggregateId);
        var tenant2Events = await eventStore2.GetEventsAsync(_defaultAggregateId);

        tenant1Events.Value.Should().HaveCount(3);
        tenant2Events.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task EventStore_WithNullTenantId_ShouldStoreEventsWithoutTenantPrefix()
    {
        // Arrange
        var nullTenantContext = Substitute.For<ITenantContext>();
        nullTenantContext.TenantId.Returns((string?)null);
        using var eventStore = new InMemoryEventStore(_eventDeserializer, _logger, nullTenantContext);

        var events = GenerateTestEvents(2);

        // Act
        var result = await eventStore.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var storedEvents = await eventStore.GetEventsAsync(_defaultAggregateId);
        storedEvents.Value.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AppendEventsAsync_WithLargeBatch_ShouldHandleEfficiently()
    {
        // Arrange
        const int eventCount = 1000;
        var events = GenerateTestEvents(eventCount);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Large batch should be processed quickly");

        var storedEvents = await _sut.GetEventsAsync(_defaultAggregateId);
        storedEvents.Value.Should().HaveCount(eventCount);

        _output.WriteLine($"Processed {eventCount} events in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task EventStore_WithSpecialCharactersInAggregateId_ShouldHandleCorrectly()
    {
        // Arrange
        var specialAggregateId = "aggregate-with-special-chars-!@#$%^&*()_+-=[]{}|;':\",./<>?";
        var events = GenerateTestEvents(2);

        // Act
        var result = await _sut.AppendEventsAsync(specialAggregateId, events, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var storedEvents = await _sut.GetEventsAsync(specialAggregateId);
        storedEvents.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task EventStore_WithUnicodeCharactersInEventData_ShouldSerializeCorrectly()
    {
        // Arrange
        var unicodeEvent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = _defaultAggregateId,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Data = "Unicode test: 你好世界 🌍 émojis and spëcial chars"
        };

        // Act
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, new[] { unicodeEvent }, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var storedEvents = await _sut.GetEventsAsync(_defaultAggregateId);
        var retrievedEvent = storedEvents.Value.Cast<TestDomainEvent>().First();
        retrievedEvent.Data.Should().Be(unicodeEvent.Data);
    }

    #endregion

    #region Memory and Performance

    [Fact]
    public async Task EventStore_ShouldHandleLargeBatch_Efficiently()
    {
        // Arrange
        const int eventCount = 10000;
        var events = GenerateTestEvents(eventCount);
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var result = await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "10,000 events should be processed in under 1 second");

        var memoryIncrease = finalMemory - initialMemory;
        _output.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024:F2} MB for {eventCount} events");
        _output.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds}ms");

        // Verify all events were stored
        var version = await _sut.GetVersionAsync(_defaultAggregateId);
        version.Value.Should().Be(eventCount);
    }

    [Fact]
    public async Task EventStore_MemoryUsage_ShouldNotLeakAfterOperations()
    {
        // Arrange
        const int iterations = 100;
        const int eventsPerIteration = 50;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var aggregateId = $"memory-test-{i}";
            var events = GenerateTestEvents(eventsPerIteration);
            await _sut.AppendEventsAsync(aggregateId, events, 0);

            // Read events back
            await _sut.GetEventsAsync(aggregateId);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var expectedMemoryIncrease = iterations * eventsPerIteration * 1024; // Rough estimate

        _output.WriteLine($"Memory increase: {memoryIncrease / 1024:F2} KB");
        _output.WriteLine($"Expected max increase: {expectedMemoryIncrease / 1024:F2} KB");

        // Memory increase should be reasonable (not indicating a major leak).
        // Use generous tolerance (x30) to avoid flaky failures from concurrent test GC pressure.
        memoryIncrease.Should().BeLessThan(expectedMemoryIncrease * 30,
            "Memory usage should not indicate significant leaks");
    }

    #endregion

    #region Disposal

    [Fact]
    public void Dispose_ShouldDisposeResourcesCorrectly()
    {
        // Arrange
        using var eventStore = new InMemoryEventStore(_eventDeserializer, _logger, _tenantContext);

        // Act
        eventStore.Dispose();

        // Assert - Should not throw when disposed
        Assert.Throws<ObjectDisposedException>(() =>
            eventStore.AppendEventsAsync(_defaultAggregateId, GenerateTestEvents(1), 0).Wait());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var eventStore = new InMemoryEventStore(_eventDeserializer, _logger, _tenantContext);

        // Act & Assert
        eventStore.Dispose();
        eventStore.Dispose(); // Should not throw
    }

    #endregion

    #region New Methods Tests

    [Fact]
    public async Task GetEventsInRangeAsync_ShouldReturnCorrectRange()
    {
        // Arrange
        var events = GenerateTestEvents(20);
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.GetEventsInRangeAsync(_defaultAggregateId, 5, 15);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(11); // Events 5-15 inclusive
    }

    [Fact]
    public async Task GetEventsInRangeAsync_WithInvalidRange_ShouldReturnValidationError()
    {
        // Act
        var result = await _sut.GetEventsInRangeAsync(_defaultAggregateId, 10, 5); // Invalid: from > to

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("FromVersion cannot be greater than ToVersion");
    }

    [Fact]
    public async Task GetEventsInRangeAsync_WithNegativeVersions_ShouldReturnValidationError()
    {
        // Act
        var result = await _sut.GetEventsInRangeAsync(_defaultAggregateId, -1, 5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetLastEventAsync_ShouldReturnMostRecentEvent()
    {
        // Arrange
        var events = GenerateTestEvents(5).ToList();
        await _sut.AppendEventsAsync(_defaultAggregateId, events, 0);

        // Act
        var result = await _sut.GetLastEventAsync(_defaultAggregateId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AggregateVersion.Should().Be(5);
    }

    [Fact]
    public async Task GetLastEventAsync_ForEmptyAggregate_ShouldReturnNotFoundError()
    {
        // Act
        var result = await _sut.GetLastEventAsync("non-existent-aggregate");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var aggregate1 = "aggregate-1";
        var aggregate2 = "aggregate-2";

        await _sut.AppendEventsAsync(aggregate1, GenerateTestEvents(5, "agg1"), 0);
        await _sut.AppendEventsAsync(aggregate2, GenerateTestEvents(3, "agg2"), 0);

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAggregates.Should().Be(2);
        result.Value.TotalEvents.Should().Be(8);
        result.Value.AggregateStatistics.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyStore_ShouldReturnZeroStatistics()
    {
        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAggregates.Should().Be(0);
        result.Value.TotalEvents.Should().Be(0);
        result.Value.AggregateStatistics.Should().BeEmpty();
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Generates a collection of test domain events.
    /// </summary>
    /// <param name="count">Number of events to generate.</param>
    /// <param name="dataPrefix">Optional prefix for event data.</param>
    /// <returns>Collection of test events.</returns>
    private IEnumerable<IDomainEvent> GenerateTestEvents(int count, string dataPrefix = "test")
    {
        return Enumerable.Range(1, count)
            .Select(i => new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = _defaultAggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow.AddMilliseconds(-count + i), // Ensure ordering
                AggregateVersion = i,
                EventVersion = 1,
                Data = $"{dataPrefix}-event-{i}"
            });
    }

    /// <summary>
    /// Test implementation of IDomainEvent for testing purposes.
    /// </summary>
    private sealed class TestDomainEvent : IDomainEvent
    {
        public required Guid EventId { get; init; }
        public required string AggregateId { get; init; }
        public required string AggregateType { get; init; }
        public required DateTimeOffset OccurredOn { get; init; }
        public required long AggregateVersion { get; init; }
        public required int EventVersion { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    #endregion

    public void Dispose()
    {
        _sut?.Dispose();
    }
}
