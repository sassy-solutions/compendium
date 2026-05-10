// -----------------------------------------------------------------------
// <copyright file="EventSourcedRepositoryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Domain.Specifications;
using Compendium.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for the abstract <see cref="EventSourcedRepository{TAggregate, TId}"/>
/// using a minimal concrete subclass for verification.
/// </summary>
public sealed class EventSourcedRepositoryTests
{
    [Fact]
    public void Ctor_NullEventStore_Throws()
    {
        // Arrange / Act
        var act = () => new TestRepository(null!, NullLogger.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("eventStore");
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();

        // Act
        var act = () => new TestRepository(eventStore, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task GetByIdAsync_NoSnapshotNoEvents_ReturnsNotFound()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>()));
        var sut = new TestRepository(eventStore, NullLogger.Instance);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Repository.AggregateNotFound");
    }

    [Fact]
    public async Task GetByIdAsync_EventsExist_BuildsAggregate()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        IReadOnlyList<IDomainEvent> events = new IDomainEvent[] { new TestEvent { AggregateVersion = 1 } };
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));
        var sut = new TestRepository(eventStore, NullLogger.Instance);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be("a-1");
    }

    [Fact]
    public async Task GetByIdAsync_EventStoreFailure_PropagatesError()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<IDomainEvent>>(Error.Failure("es.fail", "boom")));
        var sut = new TestRepository(eventStore, NullLogger.Instance);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("es.fail");
    }

    [Fact]
    public async Task GetByIdAsync_BuildAggregateFails_PropagatesError()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        IReadOnlyList<IDomainEvent> events = new IDomainEvent[] { new TestEvent() };
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));
        var sut = new FailingBuildRepository(eventStore, NullLogger.Instance);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("build.fail");
    }

    [Fact]
    public async Task GetByIdAsync_WithSnapshot_LoadsFromSnapshotAndAppliesSubsequentEvents()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        var snapshotStore = Substitute.For<ISnapshotStore>();

        var snapshot = new Snapshot<TestAggregate>(new TestAggregate("a-1", version: 5), 5, DateTimeOffset.UtcNow);
        snapshotStore
            .GetLatestSnapshotAsync<TestAggregate>("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(snapshot));

        IReadOnlyList<IDomainEvent> events = new IDomainEvent[] { new TestEvent { AggregateVersion = 6 } };
        eventStore.GetEventsAsync("a-1", 6L, Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        var sut = new TestRepository(eventStore, NullLogger.Instance, snapshotStore, new NeverSnapshotStrategy());

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await eventStore.Received().GetEventsAsync("a-1", 6L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithSnapshotButFailedEventLoad_ReturnsFailure()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        var snapshotStore = Substitute.For<ISnapshotStore>();
        var snapshot = new Snapshot<TestAggregate>(new TestAggregate("a-1", version: 5), 5, DateTimeOffset.UtcNow);
        snapshotStore
            .GetLatestSnapshotAsync<TestAggregate>("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(snapshot));

        eventStore.GetEventsAsync("a-1", 6L, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<IDomainEvent>>(Error.Failure("es.fail", "x")));

        var sut = new TestRepository(eventStore, NullLogger.Instance, snapshotStore);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("es.fail");
    }

    [Fact]
    public async Task GetByIdAsync_WithSnapshotLoadException_FallsBackToEventStream()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        var snapshotStore = Substitute.For<ISnapshotStore>();
        snapshotStore
            .GetLatestSnapshotAsync<TestAggregate>("a-1", Arg.Any<CancellationToken>())
            .Returns<Result<Snapshot<TestAggregate>>>(_ => throw new InvalidOperationException("snap.fail"));

        IReadOnlyList<IDomainEvent> events = new IDomainEvent[] { new TestEvent { AggregateVersion = 1 } };
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        var sut = new TestRepository(eventStore, NullLogger.Instance, snapshotStore);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert — repository swallowed the snapshot error and fell back to events
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_EventStoreThrows_ReturnsFailure()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Result<IReadOnlyList<IDomainEvent>>>(_ => throw new InvalidOperationException("kaboom"));
        var sut = new TestRepository(eventStore, NullLogger.Instance);

        // Act
        var result = await sut.GetByIdAsync("a-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Repository.LoadFailed");
    }

    [Fact]
    public async Task FindAsync_AlwaysReturnsNotSupported()
    {
        // Arrange
        var sut = new TestRepository(Substitute.For<IEventStore>(), NullLogger.Instance);

        // Act
        var result = await sut.FindAsync(new NoOpSpecification());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Repository.FindNotSupported");
    }

    private sealed class NoOpSpecification : Specification<TestAggregate>
    {
        public NoOpSpecification() : base(_ => true)
        {
        }
    }

    [Fact]
    public async Task SaveAsync_NoUncommittedEvents_ReturnsSuccessWithoutCallingStore()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        var sut = new TestRepository(eventStore, NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await eventStore.DidNotReceive().AppendEventsAsync(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithEvents_AppendsAndClearsEvents()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var sut = new TestRepository(eventStore, NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        aggregate.HasDomainEvents.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AppendFails_ReturnsFailure()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("es.fail", "x")));
        var sut = new TestRepository(eventStore, NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("es.fail");
    }

    [Fact]
    public async Task SaveAsync_AppendThrows_ReturnsSaveFailedError()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns<Result>(_ => throw new InvalidOperationException("kaboom"));
        var sut = new TestRepository(eventStore, NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Repository.SaveFailed");
    }

    [Fact]
    public async Task AddAsync_DelegatesToSaveAsync()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var sut = new TestRepository(eventStore, NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.AddAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToSaveAsync()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var sut = new TestRepository(eventStore, NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.UpdateAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_DefaultImplementation_ReturnsDeleteNotSupported()
    {
        // Arrange
        var sut = new TestRepository(Substitute.For<IEventStore>(), NullLogger.Instance);
        using var aggregate = new TestAggregate("a-1");

        // Act
        var result = await sut.RemoveAsync(aggregate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Repository.DeleteNotSupported");
    }

    [Fact]
    public async Task SaveAsync_StrategyRequestsSnapshot_SavesSnapshot()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<IDomainEvent>>(new IDomainEvent[] { new TestEvent() }));

        var snapshotStore = Substitute.For<ISnapshotStore>();
        snapshotStore.SaveSnapshotAsync(Arg.Any<string>(), Arg.Any<TestAggregate>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new TestRepository(eventStore, NullLogger.Instance, snapshotStore, new AlwaysSnapshotStrategy());
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await snapshotStore.Received().SaveSnapshotAsync(
            "a-1", Arg.Any<TestAggregate>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_SnapshotSaveFails_StillReturnsSuccess()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<IDomainEvent>>(new IDomainEvent[] { new TestEvent() }));

        var snapshotStore = Substitute.For<ISnapshotStore>();
        snapshotStore.SaveSnapshotAsync(Arg.Any<string>(), Arg.Any<TestAggregate>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("snap.fail", "x")));

        var sut = new TestRepository(eventStore, NullLogger.Instance, snapshotStore, new AlwaysSnapshotStrategy());
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert — snapshot is best-effort; save still succeeds
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_SnapshotThrows_StillReturnsSuccess()
    {
        // Arrange
        var eventStore = Substitute.For<IEventStore>();
        eventStore.AppendEventsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        eventStore.GetEventsAsync("a-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<IDomainEvent>>(new IDomainEvent[] { new TestEvent() }));

        var snapshotStore = Substitute.For<ISnapshotStore>();
        snapshotStore.SaveSnapshotAsync(Arg.Any<string>(), Arg.Any<TestAggregate>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns<Result>(_ => throw new InvalidOperationException("kaboom"));

        var sut = new TestRepository(eventStore, NullLogger.Instance, snapshotStore, new AlwaysSnapshotStrategy());
        using var aggregate = new TestAggregate("a-1");
        aggregate.AddTestEvent();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    private sealed class TestEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public string AggregateId { get; init; } = "a-1";
        public string AggregateType { get; init; } = "Test";
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
        public long AggregateVersion { get; init; }
        public int EventVersion { get; init; } = 1;
    }

    private sealed class TestAggregate : AggregateRoot<string>
    {
        public TestAggregate(string id, long version = 0) : base(id)
        {
            for (var i = 0; i < version; i++)
            {
                IncrementVersion();
            }
        }

        public void AddTestEvent()
        {
            AddDomainEvent(new TestEvent { AggregateVersion = Version + 1 });
            IncrementVersion();
        }
    }

    private sealed class TestRepository : EventSourcedRepository<TestAggregate, string>
    {
        public TestRepository(IEventStore eventStore, ILogger logger, ISnapshotStore? snapshotStore = null, ISnapshotStrategy? strategy = null)
            : base(eventStore, logger, snapshotStore, strategy)
        {
        }

        protected override Task<Result<TestAggregate>> BuildAggregateFromEvents(IReadOnlyList<IDomainEvent> events)
        {
            return Task.FromResult(Result.Success(new TestAggregate("a-1", version: events.Count)));
        }

        protected override Task<Result<TestAggregate>> ApplyEventsToAggregate(TestAggregate aggregate, IReadOnlyList<IDomainEvent> events)
        {
            return Task.FromResult(Result.Success(aggregate));
        }
    }

    private sealed class FailingBuildRepository : EventSourcedRepository<TestAggregate, string>
    {
        public FailingBuildRepository(IEventStore eventStore, ILogger logger)
            : base(eventStore, logger)
        {
        }

        protected override Task<Result<TestAggregate>> BuildAggregateFromEvents(IReadOnlyList<IDomainEvent> events)
        {
            return Task.FromResult(Result.Failure<TestAggregate>(Error.Failure("build.fail", "boom")));
        }

        protected override Task<Result<TestAggregate>> ApplyEventsToAggregate(TestAggregate aggregate, IReadOnlyList<IDomainEvent> events)
        {
            return Task.FromResult(Result.Success(aggregate));
        }
    }

    private sealed class AlwaysSnapshotStrategy : ISnapshotStrategy
    {
        public bool ShouldTakeSnapshot(string aggregateId, long version, int eventCount) => true;
    }

    private sealed class NeverSnapshotStrategy : ISnapshotStrategy
    {
        public bool ShouldTakeSnapshot(string aggregateId, long version, int eventCount) => false;
    }
}
