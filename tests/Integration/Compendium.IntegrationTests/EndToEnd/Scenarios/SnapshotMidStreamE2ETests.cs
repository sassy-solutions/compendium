// -----------------------------------------------------------------------
// <copyright file="SnapshotMidStreamE2ETests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Core.Domain.Events;
using Compendium.Infrastructure.EventSourcing;
using Compendium.IntegrationTests.EndToEnd.Infrastructure;
using Compendium.IntegrationTests.EndToEnd.TestAggregates;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;
using Compendium.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// E2E coverage for the "snapshot + delta events" reconstruction path that
/// <c>EventSourcedRepository</c> uses as its primary load optimisation. Existing scenarios
/// drive aggregates entirely from the event log; nothing exercises the cooperation between
/// the durable PostgreSQL event store and an <see cref="ISnapshotStore"/> sitting in front.
///
/// The scenarios pin down two contracts:
/// <list type="bullet">
/// <item>Save a snapshot at version V, append more events, then reconstruct: the snapshot
/// state plus the post-V events must yield the exact same final state as a from-scratch
/// rebuild of all events.</item>
/// <item>The snapshot store keeps the latest version: writing an older snapshot AFTER a
/// newer one must not regress the stored state. This guard prevents out-of-order saves
/// (e.g. from a delayed background task) corrupting the rehydration path.</item>
/// </list>
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "EventSourcing")]
public sealed class SnapshotMidStreamE2ETests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _pg;
    private PostgreSqlEventStore _eventStore = null!;
    private InMemorySnapshotStore _snapshots = null!;
    private const string TableName = "snapshot_midstream_events";

    public SnapshotMidStreamE2ETests(PostgreSqlFixture pg)
    {
        _pg = pg;
    }

    public async Task InitializeAsync()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var options = Options.Create(new PostgreSqlOptions
        {
            ConnectionString = _pg.ConnectionString,
            AutoCreateSchema = true,
            TableName = TableName,
            CommandTimeout = 30,
            BatchSize = 1000,
        });

        var deserializer = new E2EEventDeserializer();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        _eventStore = new PostgreSqlEventStore(options, deserializer, logger);

        var initResult = await _eventStore.InitializeSchemaAsync();
        initResult.IsSuccess.Should().BeTrue();
        await _pg.CleanTableAsync(TableName);

        _snapshots = new InMemorySnapshotStore();
    }

    public Task DisposeAsync()
    {
        _snapshots?.Dispose();
        return Task.CompletedTask;
    }

    [RequiresDockerFact]
    public async Task LoadFromMidStreamSnapshot_PlusDeltaEvents_ProducesIdenticalStateAsFullReplay()
    {
        // Arrange — append 4 events, snapshot at version 2, append 3 more events.
        // Loading via "snapshot + events from version 3 onward" must equal "replay all 7".
        if (!_pg.IsAvailable)
        {
            return;
        }

        var orderId = OrderId.New();
        var customerId = "snapshot-customer";

        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);
        order.AddOrderLine("line-1", "p1", 1, 10m);
        order.AddOrderLine("line-2", "p2", 1, 20m);
        var firstBatch = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var saveBatch1 = await _eventStore.AppendEventsAsync(orderId.ToString(), firstBatch, expectedVersion: 0);
        saveBatch1.IsSuccess.Should().BeTrue();

        // Snapshot the aggregate state at version 2 (after the two line additions). We use
        // a state DTO rather than the aggregate itself because the InMemorySnapshotStore
        // serializes via System.Text.Json, which does not round-trip the AggregateRoot's
        // private state shape — that's the realistic application-level pattern anyway.
        var snapshotAtV2 = new OrderSnapshotState
        {
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            LineCount = order.OrderLines.Count,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
        };

        var snapshotSave = await _snapshots.SaveSnapshotAsync(orderId.ToString(), snapshotAtV2, version: 2);
        snapshotSave.IsSuccess.Should().BeTrue();

        // Append more events after the snapshot.
        order.AddOrderLine("line-3", "p3", 5, 5m);
        var completeResult = order.Complete(DateTimeOffset.UtcNow);
        completeResult.IsSuccess.Should().BeTrue();
        var secondBatch = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var saveBatch2 = await _eventStore.AppendEventsAsync(orderId.ToString(), secondBatch, expectedVersion: firstBatch.Count);
        saveBatch2.IsSuccess.Should().BeTrue();

        // Act — variant A: full replay from version 0.
        var allEvents = await _eventStore.GetEventsAsync(orderId.ToString());
        allEvents.IsSuccess.Should().BeTrue();
        var fullReplay = OrderAggregate.FromEvents(orderId, allEvents.Value);

        // Act — variant B: load snapshot, then apply only the events past the snapshot version.
        var loadedSnapshot = await _snapshots.GetLatestSnapshotAsync<OrderSnapshotState>(orderId.ToString());
        loadedSnapshot.IsSuccess.Should().BeTrue();
        loadedSnapshot.Value.Version.Should().Be(2);

        var deltaEvents = await _eventStore.GetEventsAsync(orderId.ToString(), fromVersion: loadedSnapshot.Value.Version + 1);
        deltaEvents.IsSuccess.Should().BeTrue();

        var rehydrated = ApplyEventsToSnapshot(loadedSnapshot.Value.State, deltaEvents.Value);

        // Assert — both reconstructions must agree on the final state.
        rehydrated.LineCount.Should().Be(fullReplay.OrderLines.Count);
        rehydrated.Status.Should().Be(fullReplay.Status.ToString());
        rehydrated.TotalAmount.Should().Be(fullReplay.TotalAmount);
        rehydrated.CustomerId.Should().Be(fullReplay.CustomerId);

        deltaEvents.Value.Should().HaveCount(secondBatch.Count,
            "loading from a snapshot at v2 must skip the first 2 events and only re-apply the post-snapshot delta");
    }

    [RequiresDockerFact]
    public async Task SaveSnapshot_OlderVersionAfterNewer_DoesNotRegressStoredState()
    {
        // Arrange — write a v5 snapshot, then attempt to write a v3 snapshot. The store
        // must keep v5. Without this guard, an out-of-order save (e.g. a delayed background
        // snapshotter racing the live writer) would corrupt the rehydration path.
        if (!_pg.IsAvailable)
        {
            return;
        }

        var aggregateId = $"aggregate-{Guid.NewGuid():N}";
        var newerState = new OrderSnapshotState
        {
            CustomerId = "newer",
            Status = "Completed",
            LineCount = 5,
            TotalAmount = 500m,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var olderState = new OrderSnapshotState
        {
            CustomerId = "older-and-stale",
            Status = "Created",
            LineCount = 3,
            TotalAmount = 300m,
            CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
        };

        var saveNewer = await _snapshots.SaveSnapshotAsync(aggregateId, newerState, version: 5);
        saveNewer.IsSuccess.Should().BeTrue();

        // Act
        var saveOlder = await _snapshots.SaveSnapshotAsync(aggregateId, olderState, version: 3);

        // Assert
        saveOlder.IsSuccess.Should().BeTrue("the store reports success when skipping a stale write");

        var loaded = await _snapshots.GetLatestSnapshotAsync<OrderSnapshotState>(aggregateId);
        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.Version.Should().Be(5, "the older snapshot must NOT replace the newer one");
        loaded.Value.State.CustomerId.Should().Be("newer");
        loaded.Value.State.LineCount.Should().Be(5);
    }

    private static OrderSnapshotState ApplyEventsToSnapshot(
        OrderSnapshotState snapshot,
        IEnumerable<IDomainEvent> deltaEvents)
    {
        // Apply the post-snapshot events to a copy of the snapshot state. Mirrors the
        // reconstruction path used by Compendium.Infrastructure's EventSourcedRepository
        // when both a snapshot and additional events are available for an aggregate.
        var working = new OrderSnapshotState
        {
            CustomerId = snapshot.CustomerId,
            Status = snapshot.Status,
            LineCount = snapshot.LineCount,
            TotalAmount = snapshot.TotalAmount,
            CreatedAt = snapshot.CreatedAt,
        };

        foreach (var @event in deltaEvents)
        {
            switch (@event)
            {
                case TestAggregates.Events.OrderLineAddedEvent line:
                    working.LineCount++;
                    working.TotalAmount += line.Quantity * line.UnitPrice;
                    break;
                case TestAggregates.Events.OrderCompletedEvent:
                    working.Status = "Completed";
                    break;
            }
        }

        return working;
    }

    /// <summary>
    /// Snapshot DTO. We use a flat record instead of the full aggregate because
    /// <see cref="InMemorySnapshotStore"/> serializes via System.Text.Json, which doesn't
    /// preserve private aggregate state. That mirrors how production code hand-writes a
    /// snapshot DTO and rehydrates by replaying domain events on top.
    /// </summary>
    public sealed class OrderSnapshotState
    {
        public string CustomerId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int LineCount { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
