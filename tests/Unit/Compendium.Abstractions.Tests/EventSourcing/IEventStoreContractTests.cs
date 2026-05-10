// -----------------------------------------------------------------------
// <copyright file="IEventStoreContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;

namespace Compendium.Abstractions.Tests.EventSourcing;

public class IEventStoreContractTests
{
    private sealed record FakeDomainEvent(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent
    {
        public string AggregateId => "agg-1";

        public string AggregateType => "FakeAggregate";

        public long AggregateVersion => 1L;

        public int EventVersion => 1;
    }

    [Fact]
    public async Task IEventStore_Substitute_AppendEventsAsync_ForwardsArgumentsAndReturnsConfiguredResult()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        var events = new[] { new FakeDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow) };
        store
            .AppendEventsAsync("agg-1", events, 0L, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await store.AppendEventsAsync("agg-1", events, 0L, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await store.Received(1).AppendEventsAsync("agg-1", events, 0L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IEventStore_Substitute_GetEventsAsync_ReturnsEventList()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        IReadOnlyList<IDomainEvent> expected = new IDomainEvent[]
        {
            new FakeDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
        };
        store.GetEventsAsync("agg-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(expected));

        // Act
        var result = await store.GetEventsAsync("agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task IEventStore_Substitute_GetEventsAsync_FromVersion_ReturnsConfiguredEvents()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        IReadOnlyList<IDomainEvent> expected = Array.Empty<IDomainEvent>();
        store.GetEventsAsync("agg-1", 5L, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expected));

        // Act
        var result = await store.GetEventsAsync("agg-1", 5L, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task IEventStore_Substitute_GetEventsInRangeAsync_ReturnsConfiguredEvents()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        IReadOnlyList<IDomainEvent> expected = new IDomainEvent[]
        {
            new FakeDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
        };
        store.GetEventsInRangeAsync("agg-1", 0L, 10L, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expected));

        // Act
        var result = await store.GetEventsInRangeAsync("agg-1", 0L, 10L, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task IEventStore_Substitute_GetLastEventAsync_ReturnsEvent()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        var lastEvent = new FakeDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        store.GetLastEventAsync("agg-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IDomainEvent>(lastEvent));

        // Act
        var result = await store.GetLastEventAsync("agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(lastEvent);
    }

    [Fact]
    public async Task IEventStore_Substitute_GetVersionAsync_ReturnsLong()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        store.GetVersionAsync("agg-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(99L));

        // Act
        var result = await store.GetVersionAsync("agg-1", CancellationToken.None);

        // Assert
        result.Value.Should().Be(99L);
    }

    [Fact]
    public async Task IEventStore_Substitute_ExistsAsync_ReturnsBool()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        store.ExistsAsync("agg-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        // Act
        var result = await store.ExistsAsync("agg-1", CancellationToken.None);

        // Assert
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IEventStore_Substitute_GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        var stats = new EventStoreStatistics { TotalAggregates = 1, TotalEvents = 7 };
        store.GetStatisticsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(stats));

        // Act
        var result = await store.GetStatisticsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(7);
    }

    [Fact]
    public async Task IEventStore_Substitute_ReturnsNotFoundError_WhenAggregateMissing()
    {
        // Arrange
        var store = Substitute.For<IEventStore>();
        var error = Error.NotFound("event_store.not_found", "missing");
        store.GetVersionAsync("missing", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(error));

        // Act
        var result = await store.GetVersionAsync("missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
