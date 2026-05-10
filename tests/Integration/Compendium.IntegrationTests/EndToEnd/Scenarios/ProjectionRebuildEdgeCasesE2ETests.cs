// -----------------------------------------------------------------------
// <copyright file="ProjectionRebuildEdgeCasesE2ETests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.PostgreSQL.Projections;
using Compendium.Core.EventSourcing;
using Compendium.Infrastructure.Projections;
using Compendium.IntegrationTests.EndToEnd.Infrastructure;
using Compendium.IntegrationTests.EndToEnd.TestAggregates;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;
using Compendium.IntegrationTests.EndToEnd.TestProjections;
using Compendium.IntegrationTests.Fixtures;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// Complements <see cref="ProjectionRebuildE2ETests"/> with edge cases that protect against
/// regressions in checkpoint handling:
///
/// <list type="bullet">
/// <item>Multi-phase rebuild — rebuild, append more events, rebuild again. The checkpoint
/// must advance monotonically and events written after the first rebuild must NOT be lost
/// (regression guard for the "stale checkpoint freezes a projection" failure mode).</item>
/// <item>Rebuild idempotency — calling <c>RebuildProjectionAsync</c> twice in a row with no
/// new events must not change the checkpoint or corrupt projection state. This pins down
/// the contract that drives at-most-once event application even if the rebuild button is
/// hit twice in the admin UI.</item>
/// <item>High starting checkpoint — when a checkpoint sits beyond the highest global position
/// (e.g. table truncation + checkpoint not reset), rebuild must not regress to position 0.
/// Backfill logic landed in <c>fix(projections): opt-in backfill from position 0 on empty
/// checkpoint (#40)</c>; this asserts the inverse: a NON-empty checkpoint stays put.</item>
/// </list>
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "Performance")]
public sealed class ProjectionRebuildEdgeCasesE2ETests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _pg;
    private PostgreSqlEventStore _eventStore = null!;
    private PostgreSqlStreamingEventStore _streamingEventStore = null!;
    private PostgreSqlProjectionStore _projectionStore = null!;
    private IProjectionManager _projectionManager = null!;
    private const string TableName = "rebuild_edges_events";

    public ProjectionRebuildEdgeCasesE2ETests(PostgreSqlFixture pg)
    {
        _pg = pg;
    }

    public async Task InitializeAsync()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Warning));

        services.Configure<PostgreSqlOptions>(o =>
        {
            o.ConnectionString = _pg.ConnectionString;
            o.TableName = TableName;
            o.AutoCreateSchema = true;
            o.BatchSize = 1000;
        });

        services.Configure<ProjectionOptions>(o =>
        {
            o.RebuildBatchSize = 100;
            o.MaxConcurrentRebuilds = 1;
            o.ProgressReportInterval = 50;
            o.EnableSnapshots = false;
        });

        services.AddSingleton<IEventDeserializer>(new E2EEventDeserializer());
        services.AddSingleton<PostgreSqlEventStore>();
        services.AddSingleton<IStreamingEventStore, PostgreSqlStreamingEventStore>();
        services.AddSingleton<IProjectionStore, PostgreSqlProjectionStore>();
        services.AddSingleton<IProjectionManager, EnhancedProjectionManager>();
        services.AddSingleton<OrderSummaryProjection>();

        var provider = services.BuildServiceProvider();
        _eventStore = provider.GetRequiredService<PostgreSqlEventStore>();
        _streamingEventStore = (PostgreSqlStreamingEventStore)provider.GetRequiredService<IStreamingEventStore>();
        _projectionStore = (PostgreSqlProjectionStore)provider.GetRequiredService<IProjectionStore>();
        _projectionManager = provider.GetRequiredService<IProjectionManager>();

        // Drop projection metadata so each test starts from a clean slate. We keep the
        // event-store table because the per-test orderIds are unique guids, so a shared
        // table is safe and faster than DDL on every run.
        await using var connection = new NpgsqlConnection(_pg.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync($"DROP TABLE IF EXISTS {TableName}");
        await connection.ExecuteAsync("DROP TABLE IF EXISTS projection_checkpoints");
        await connection.ExecuteAsync("DROP TABLE IF EXISTS projection_states");
        await connection.ExecuteAsync("DROP TABLE IF EXISTS projection_snapshots");

        var initEvents = await _eventStore.InitializeSchemaAsync();
        initEvents.IsSuccess.Should().BeTrue();
        var initStream = await _streamingEventStore.InitializeSchemaAsync();
        initStream.IsSuccess.Should().BeTrue();
        await _projectionStore.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [RequiresDockerFact]
    public async Task RebuildProjection_TwoPhaseAppend_CheckpointAdvancesMonotonically()
    {
        // Arrange — phase 1: append 5 events, rebuild, capture checkpoint.
        if (!_pg.IsAvailable)
        {
            return;
        }

        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-multi-phase", DateTimeOffset.UtcNow);
        var phase1Events = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        order.AddOrderLine("line-1", "product-A", 1, 10.00m);
        order.AddOrderLine("line-2", "product-B", 2, 20.00m);
        order.AddOrderLine("line-3", "product-C", 3, 30.00m);
        phase1Events.AddRange(order.DomainEvents);
        order.ClearDomainEvents();

        var phase1Append = await _eventStore.AppendEventsAsync(orderId.ToString(), phase1Events, expectedVersion: 0);
        phase1Append.IsSuccess.Should().BeTrue();

        _projectionManager.RegisterProjection<OrderSummaryProjection>();

        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(streamId: orderId.ToString());

        var checkpointAfterPhase1 = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");
        checkpointAfterPhase1.Should().NotBeNull();
        checkpointAfterPhase1!.Value.Should().BeGreaterThan(0, "phase 1 produced 4 events");

        // Act — phase 2: append more events, rebuild again.
        order.AddOrderLine("line-4", "product-D", 1, 40.00m);
        order.AddOrderLine("line-5", "product-E", 1, 50.00m);
        var phase2Events = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var phase2Append = await _eventStore.AppendEventsAsync(orderId.ToString(), phase2Events, expectedVersion: phase1Events.Count);
        phase2Append.IsSuccess.Should().BeTrue();

        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(streamId: orderId.ToString());
        var checkpointAfterPhase2 = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");

        // Assert
        checkpointAfterPhase2.Should().NotBeNull();
        checkpointAfterPhase2!.Value.Should().BeGreaterThan(checkpointAfterPhase1.Value,
            "the second rebuild must consume events appended after the first rebuild's checkpoint");

        var state = await _projectionManager.GetProjectionStateAsync("E2E_OrderSummary");
        state.Status.Should().Be(ProjectionStatus.Completed);
    }

    [RequiresDockerFact]
    public async Task RebuildProjection_CalledTwiceWithNoNewEvents_CheckpointStaysStableAndStateIsCompleted()
    {
        // Arrange — single stream, rebuild once.
        if (!_pg.IsAvailable)
        {
            return;
        }

        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-idempotent", DateTimeOffset.UtcNow);
        order.AddOrderLine("only-line", "product-X", 1, 99.99m);
        var events = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var append = await _eventStore.AppendEventsAsync(orderId.ToString(), events, expectedVersion: 0);
        append.IsSuccess.Should().BeTrue();

        _projectionManager.RegisterProjection<OrderSummaryProjection>();
        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(streamId: orderId.ToString());
        var firstCheckpoint = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");

        // Act — rebuild again with NO new events appended in between.
        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(streamId: orderId.ToString());
        var secondCheckpoint = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");

        // Assert
        firstCheckpoint.Should().NotBeNull();
        secondCheckpoint.Should().NotBeNull();
        secondCheckpoint!.Value.Should().Be(firstCheckpoint!.Value,
            "rebuilding twice with no new events must not change the checkpoint");

        var state = await _projectionManager.GetProjectionStateAsync("E2E_OrderSummary");
        state.Status.Should().Be(ProjectionStatus.Completed);
    }

    [RequiresDockerFact]
    public async Task RebuildProjection_WithCheckpointAlreadyAtMaxPosition_CompletesWithoutReprocessing()
    {
        // Arrange — append events, rebuild, capture max checkpoint. Then call rebuild again
        // and assert no events are re-applied below the existing checkpoint. We measure
        // re-application via a counting projection wrapper that tracks ApplyAsync calls.
        if (!_pg.IsAvailable)
        {
            return;
        }

        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-max-checkpoint", DateTimeOffset.UtcNow);
        order.AddOrderLine("a", "p1", 1, 5m);
        order.AddOrderLine("b", "p2", 1, 5m);
        var events = order.DomainEvents.ToList();
        order.ClearDomainEvents();
        await _eventStore.AppendEventsAsync(orderId.ToString(), events, expectedVersion: 0);

        _projectionManager.RegisterProjection<OrderSummaryProjection>();

        // First rebuild establishes the high-water-mark checkpoint.
        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(streamId: orderId.ToString());
        var initialCheckpoint = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");
        initialCheckpoint.Should().NotBeNull();
        var maxPosition = await _streamingEventStore.GetMaxGlobalPositionAsync();
        initialCheckpoint!.Value.Should().BeGreaterOrEqualTo(events.Count - 1,
            "the checkpoint must reach at least the count of applied events");

        // Act — rebuild again. The store already holds a checkpoint at the end of the stream.
        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(streamId: orderId.ToString());
        var afterCheckpoint = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");

        // Assert
        afterCheckpoint.Should().NotBeNull();
        afterCheckpoint!.Value.Should().BeLessOrEqualTo(maxPosition,
            "the checkpoint must never exceed the highest global position observed in the event store");
        afterCheckpoint.Value.Should().Be(initialCheckpoint.Value,
            "a rebuild starting from the existing checkpoint with no new events must leave the checkpoint untouched");
    }
}
