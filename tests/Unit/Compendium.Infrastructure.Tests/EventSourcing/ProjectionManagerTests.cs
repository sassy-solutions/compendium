// -----------------------------------------------------------------------
// <copyright file="ProjectionManagerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Tests.EventSourcing;

/// <summary>
/// Comprehensive test suite for ProjectionManager implementation.
/// Tests projection creation, event handling, rebuilding, and concurrency.
/// </summary>
public sealed class ProjectionManagerTests
{
    private readonly ITestOutputHelper _output;
    private readonly IEventStore _eventStore;
    private readonly ILogger<ProjectionManager> _logger;
    private readonly ProjectionManager _projectionManager;

    public ProjectionManagerTests(ITestOutputHelper output)
    {
        _output = output;
        _eventStore = Substitute.For<IEventStore>();
        _logger = Substitute.For<ILogger<ProjectionManager>>();
        _projectionManager = new ProjectionManager(_eventStore, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullEventStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectionManager(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectionManager(_eventStore, null!));
    }

    #endregion

    #region Projection Registration

    [Fact]
    public void RegisterProjection_WithValidProjection_ShouldRegisterSuccessfully()
    {
        // Arrange
        var projection = new TestProjection();

        // Act & Assert
        _projectionManager.Invoking(pm => pm.RegisterProjection(projection))
            .Should().NotThrow();
    }

    [Fact]
    public void RegisterProjection_WithNullProjection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _projectionManager.RegisterProjection(null!));
    }

    [Fact]
    public void RegisterProjection_SameProjectionTwice_ShouldReplaceExisting()
    {
        // Arrange
        var projection1 = new TestProjection();
        var projection2 = new TestProjection();

        // Act
        _projectionManager.RegisterProjection(projection1);
        _projectionManager.RegisterProjection(projection2);

        // Assert - Should not throw, second registration should replace first
        _projectionManager.Invoking(pm => pm.RegisterProjection(projection2))
            .Should().NotThrow();
    }

    #endregion

    #region Event Projection

    [Fact]
    public async Task ProcessEventAsync_WithNullEvent_ShouldReturnValidationError()
    {
        // Act
        var result = await _projectionManager.ProcessEventAsync("test-aggregate", null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ProcessEventAsync_WithNoRegisteredProjections_ShouldReturnSuccess()
    {
        // Arrange
        var domainEvent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Data = "Test data"
        };

        // Act
        var result = await _projectionManager.ProcessEventAsync("test-aggregate", domainEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProjectEventAsync_WithMatchingProjection_ShouldCallProjection()
    {
        // Arrange
        var projection = new TestProjection();
        var domainEvent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Data = "Test data"
        };

        _projectionManager.RegisterProjection(projection);

        // Act
        var result = await _projectionManager.ProcessEventAsync("test-aggregate", domainEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        projection.HandledEvents.Should().ContainSingle();
        projection.HandledEvents.First().Should().Be(domainEvent);
    }

    [Fact]
    public async Task ProjectEventAsync_WithMultipleMatchingProjections_ShouldCallAllProjections()
    {
        // Arrange
        var projection1 = new TestProjection();
        var projection2 = new AlternateTestProjection();
        var domainEvent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Data = "Test data"
        };

        _projectionManager.RegisterProjection(projection1);
        _projectionManager.RegisterProjection(projection2);

        // Act
        var result = await _projectionManager.ProcessEventAsync("test-aggregate", domainEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        projection1.HandledEvents.Should().ContainSingle();
        projection2.HandledEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task ProjectEventAsync_WithNonMatchingProjection_ShouldNotCallProjection()
    {
        // Arrange
        var projection = new TestProjection();
        var differentEvent = new DifferentTestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Message = "Different event"
        };

        _projectionManager.RegisterProjection(projection);

        // Act
        var result = await _projectionManager.ProcessEventAsync("test-aggregate", differentEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        projection.HandledEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ProjectEventAsync_WithFailingProjection_ShouldReturnFailure()
    {
        // Arrange
        var failingProjection = new FailingTestProjection();
        var domainEvent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Data = "Test data"
        };

        _projectionManager.RegisterProjection(failingProjection);

        // Act
        var result = await _projectionManager.ProcessEventAsync("test-aggregate", domainEvent);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Projection.Failed");
    }

    #endregion

    #region Projection Rebuilding

    [Fact]
    public async Task RebuildProjectionAsync_WithUnregisteredProjection_ShouldReturnNotFoundError()
    {
        // Act
        var result = await _projectionManager.RebuildProjectionAsync("test-projection", "test-aggregate");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Projection.NotFound");
    }

    [Fact]
    public async Task RebuildProjectionAsync_WithRegisteredProjection_ShouldCallReset()
    {
        // Arrange
        var projection = new TestProjection();
        _projectionManager.RegisterProjection(projection);

        // Set up event store to return empty events list (successful result)
        // Note: GetEventsAsync takes 3 parameters (aggregateId, position, cancellationToken)
        _eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<IDomainEvent>>(new List<IDomainEvent>()));

        // Act
        var result = await _projectionManager.RebuildProjectionAsync("test-projection", "test-aggregate");

        // Assert
        result.IsSuccess.Should().BeTrue();
        projection.ResetCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RebuildProjectionAsync_WithFailingReset_ShouldReturnFailure()
    {
        // Arrange
        var failingProjection = new FailingResetProjection();
        _projectionManager.RegisterProjection(failingProjection);

        // Set up event store to return empty events list (so we reach the reset logic)
        _eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<IDomainEvent>>(new List<IDomainEvent>()));

        // Act
        var result = await _projectionManager.RebuildProjectionAsync("failing-reset-projection", "test-aggregate");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Projection.RebuildFailed");
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ProjectEventAsync_ConcurrentEvents_ShouldHandleAllEvents()
    {
        // Arrange
        const int eventCount = 100;
        var projection = new ConcurrentTestProjection();
        _projectionManager.RegisterProjection(projection);

        var events = Enumerable.Range(0, eventCount)
            .Select(i => new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = $"aggregate-{i}",
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                AggregateVersion = 1,
                EventVersion = 1,
                Data = $"Event {i}"
            })
            .ToList();

        // Act
        var tasks = events.Select(e => _projectionManager.ProcessEventAsync("test-aggregate", e));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        projection.HandledEvents.Should().HaveCount(eventCount);
        projection.HandledEvents.Should().BeEquivalentTo(events);
    }

    [Fact]
    public async Task ProjectEventAsync_ConcurrentProjections_ShouldMaintainConsistency()
    {
        // Arrange
        const int eventCount = 50;

        // Use a single projection since we can't register multiple of the same type
        var projection = new ConcurrentTestProjection();
        _projectionManager.RegisterProjection(projection);

        var events = Enumerable.Range(0, eventCount)
            .Select(i => new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = $"aggregate-{i}",
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                AggregateVersion = 1,
                EventVersion = 1,
                Data = $"Event {i}"
            })
            .ToList();

        // Act
        var tasks = events.Select(e => _projectionManager.ProcessEventAsync("test-aggregate", e));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        projection.HandledEvents.Should().HaveCount(eventCount);
    }

    [Fact]
    public async Task ProjectionManager_ConcurrentRegistrationAndProjection_ShouldBeThreadSafe()
    {
        // Arrange
        const int operationCount = 100;
        var exceptions = new ConcurrentBag<Exception>();
        var results = new ConcurrentBag<Result>();

        // Act - Concurrent registration and projection
        var tasks = Enumerable.Range(0, operationCount)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    // Register a projection
                    var projection = new TestProjection();
                    _projectionManager.RegisterProjection(projection);

                    // Project an event
                    var domainEvent = new TestDomainEvent
                    {
                        EventId = Guid.NewGuid(),
                        AggregateId = $"aggregate-{i}",
                        AggregateType = "TestAggregate",
                        OccurredOn = DateTimeOffset.UtcNow,
                        AggregateVersion = 1,
                        EventVersion = 1,
                        Data = $"Event {i}"
                    };

                    var result = await _projectionManager.ProcessEventAsync("test-aggregate", domainEvent);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
        results.Should().HaveCount(operationCount);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ProjectionManager_PerformanceTest_ShouldHandleHighThroughput()
    {
        // Arrange
        const int eventCount = 10000;
        var projection = new TestProjection();
        _projectionManager.RegisterProjection(projection);

        var events = Enumerable.Range(0, eventCount)
            .Select(i => new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = $"aggregate-{i % 100}", // Reuse aggregate IDs
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                AggregateVersion = i + 1,
                EventVersion = 1,
                Data = $"Event {i}"
            })
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = events.Select(e => _projectionManager.ProcessEventAsync("test-aggregate", e));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        projection.HandledEvents.Should().HaveCount(eventCount);

        var avgTimePerEvent = (double)stopwatch.ElapsedMilliseconds / eventCount;
        _output.WriteLine($"Processed {eventCount} events in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per event: {avgTimePerEvent:F3}ms");

        avgTimePerEvent.Should().BeLessThan(1, "Should handle events efficiently");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ProjectEventAsync_WithProjectionThatHandlesMultipleEventTypes_ShouldWork()
    {
        // Arrange
        var multiEventProjection = new MultiEventProjection();
        _projectionManager.RegisterProjection(multiEventProjection);

        var testEvent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 1,
            EventVersion = 1,
            Data = "Test data"
        };

        var differentEvent = new DifferentTestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = "test-aggregate",
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = 2,
            EventVersion = 1,
            Message = "Different event"
        };

        // Act
        var result1 = await _projectionManager.ProcessEventAsync("test-aggregate", testEvent);
        var result2 = await _projectionManager.ProcessEventAsync("test-aggregate", differentEvent);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        multiEventProjection.HandledEvents.Should().HaveCount(2);
    }

    #endregion

    #region Test Helper Classes

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

    private sealed class DifferentTestEvent : IDomainEvent
    {
        public required Guid EventId { get; init; }
        public required string AggregateId { get; init; }
        public required string AggregateType { get; init; }
        public required DateTimeOffset OccurredOn { get; init; }
        public required long AggregateVersion { get; init; }
        public required int EventVersion { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    private sealed class TestProjection : ProjectionBase<TestDomainEvent, TestProjectionState>
    {
        public override string ProjectionId => "test-projection";
        public List<TestDomainEvent> HandledEvents { get; } = new();
        public bool ResetCalled { get; private set; }

        protected override Task HandleEventAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(domainEvent);
            var currentState = State;
            currentState.EventCount++;
            currentState.LastEventId = domainEvent.EventId;
            UpdateState(currentState);
            return Task.CompletedTask;
        }

        public override Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCalled = true;
            HandledEvents.Clear();
            return base.ResetAsync(cancellationToken);
        }
    }

    private sealed class TestProjectionState
    {
        public int EventCount { get; set; }
        public Guid? LastEventId { get; set; }
    }

    private sealed class AlternateTestProjection : ProjectionBase<TestDomainEvent, TestProjectionState>
    {
        public override string ProjectionId => "alternate-test-projection";
        public List<TestDomainEvent> HandledEvents { get; } = new();
        public bool ResetCalled { get; private set; }

        protected override Task HandleEventAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(domainEvent);
            var currentState = State;
            currentState.EventCount++;
            currentState.LastEventId = domainEvent.EventId;
            UpdateState(currentState);
            return Task.CompletedTask;
        }

        public override Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCalled = true;
            HandledEvents.Clear();
            return base.ResetAsync(cancellationToken);
        }
    }

    private sealed class ConcurrentTestProjection : ProjectionBase<TestDomainEvent, ConcurrentTestProjectionState>
    {
        public override string ProjectionId => "concurrent-test-projection";
        private readonly ConcurrentBag<TestDomainEvent> _handledEvents = new();

        public IEnumerable<TestDomainEvent> HandledEvents => _handledEvents;

        protected override Task HandleEventAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _handledEvents.Add(domainEvent);
            var currentState = State;
            Interlocked.Increment(ref currentState.EventCount);
            currentState.LastEventId = domainEvent.EventId;
            UpdateState(currentState);
            return Task.CompletedTask;
        }

        public override Task ResetAsync(CancellationToken cancellationToken = default)
        {
            // ConcurrentBag doesn't have a clear method, so we'd need to recreate it
            // For testing purposes, this is sufficient
            return base.ResetAsync(cancellationToken);
        }
    }

    private sealed class ConcurrentTestProjectionState
    {
        public int EventCount;
        public Guid? LastEventId { get; set; }
    }

    private sealed class FailingTestProjection : ProjectionBase<TestDomainEvent, TestProjectionState>
    {
        public override string ProjectionId => "failing-test-projection";

        protected override Task HandleEventAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test projection failure");
        }
    }

    private sealed class FailingResetProjection : ProjectionBase<TestDomainEvent, TestProjectionState>
    {
        public override string ProjectionId => "failing-reset-projection";

        protected override Task HandleEventAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override Task ResetAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Reset failure");
        }
    }

    private sealed class MultiEventProjection : IProjection
    {
        public string ProjectionId => "multi-event-projection";
        public int Version { get; private set; }
        public List<IDomainEvent> HandledEvents { get; } = new();

        public Task ApplyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent is TestDomainEvent || domainEvent is DifferentTestEvent)
            {
                HandledEvents.Add(domainEvent);
                Version++;
            }
            return Task.CompletedTask;
        }

        public Task<object> GetStateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>(new { EventCount = HandledEvents.Count, Events = HandledEvents });
        }

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            HandledEvents.Clear();
            Version = 0;
            return Task.CompletedTask;
        }
    }

    #endregion
}
