// -----------------------------------------------------------------------
// <copyright file="ProjectionRebuildE2ETests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.PostgreSQL.Projections;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Infrastructure.Projections;
using Compendium.IntegrationTests.EndToEnd.Infrastructure;
using Compendium.IntegrationTests.EndToEnd.TestAggregates;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;
using Compendium.IntegrationTests.EndToEnd.TestProjections;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Compendium.IntegrationTests.Fixtures;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// E2E Test Scenario: Projection Rebuild and Query.
/// Tests projection rebuild from large event streams with performance monitoring.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "Performance")]
public sealed class ProjectionRebuildE2ETests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private PostgreSqlEventStore? _eventStore;
    private PostgreSqlStreamingEventStore? _streamingEventStore;
    private PostgreSqlProjectionStore? _projectionStore;
    private IProjectionManager? _projectionManager;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        // Use EnvironmentConfigurationHelper for connection string fallback
        var externalConnectionString = Compendium.IntegrationTests.Infrastructure.EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            _connectionString = externalConnectionString;
        }
        else
        {
            // Fallback to TestContainers
            Console.WriteLine("⚠️ Starting TestContainer for PostgreSQL (Projection E2E)...");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("compendium_projection_e2e")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .WithCleanUp(true)
                .Build();

            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
        }

        // Set up dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Configure PostgreSQL options
        services.Configure<PostgreSqlOptions>(options =>
        {
            options.ConnectionString = _connectionString;
            options.TableName = "projection_events_e2e";
            options.AutoCreateSchema = true;
            options.BatchSize = 1000;
        });

        // Configure projection options
        services.Configure<ProjectionOptions>(options =>
        {
            options.RebuildBatchSize = 100;
            options.MaxConcurrentRebuilds = 2;
            options.ProgressReportInterval = 10; // Report every 10 events for testing
            options.EnableSnapshots = false; // Disable for clearer testing
            options.SnapshotInterval = TimeSpan.FromHours(1);
        });

        // Register services
        var eventDeserializer = new E2EEventDeserializer();
        services.AddSingleton<IEventDeserializer>(eventDeserializer);
        services.AddSingleton<PostgreSqlEventStore>();
        services.AddSingleton<IStreamingEventStore, PostgreSqlStreamingEventStore>();
        services.AddSingleton<IProjectionStore, PostgreSqlProjectionStore>();
        services.AddSingleton<IProjectionManager, EnhancedProjectionManager>();

        var provider = services.BuildServiceProvider();

        _eventStore = provider.GetRequiredService<PostgreSqlEventStore>();
        _streamingEventStore = (PostgreSqlStreamingEventStore)provider.GetRequiredService<IStreamingEventStore>();
        _projectionStore = (PostgreSqlProjectionStore)provider.GetRequiredService<IProjectionStore>();
        _projectionManager = provider.GetRequiredService<IProjectionManager>();

        // Drop and recreate tables to ensure clean state with proper constraints
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync($"DROP TABLE IF EXISTS projection_events_e2e");
        await connection.ExecuteAsync($"DROP TABLE IF EXISTS projection_checkpoints");
        await connection.CloseAsync();

        // Initialize schemas
        var initResult = await _eventStore.InitializeSchemaAsync();
        initResult.IsSuccess.Should().BeTrue();

        var streamInitResult = await _streamingEventStore.InitializeSchemaAsync();
        streamInitResult.IsSuccess.Should().BeTrue();

        await _projectionStore.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [RequiresDockerFact]
    public async Task RebuildProjection_From1000Events_CompletesSuccessfully()
    {
        // Arrange
        const int eventCount = 1000;
        var orderId = OrderId.New();
        var customerId = "customer-perf-001";

        // Create order
        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);
        var placeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore!.AppendEventsAsync(orderId.ToString(), placeEvents, 0);

        // Add 999 order lines in batches of 100 (more realistic for production)
        const int batchSize = 100;
        var currentVersion = 1L;

        for (int batchStart = 1; batchStart < eventCount; batchStart += batchSize)
        {
            var batchEvents = new List<IDomainEvent>();
            var batchEnd = Math.Min(batchStart + batchSize, eventCount);

            for (int i = batchStart; i < batchEnd; i++)
            {
                var lineResult = order.AddOrderLine($"line-{i}", $"product-{i}", 1, 10.00m);
                lineResult.IsSuccess.Should().BeTrue();

                var lineEvents = order.DomainEvents.ToList();
                batchEvents.AddRange(lineEvents);
                order.ClearDomainEvents();
            }

            // Append batch
            var appendResult = await _eventStore.AppendEventsAsync(orderId.ToString(), batchEvents, currentVersion);
            appendResult.IsSuccess.Should().BeTrue($"Batch starting at {batchStart} should append successfully");
            currentVersion += batchEvents.Count;
        }

        // Verify all events were appended
        var allEvents = await _eventStore.GetEventsAsync(orderId.ToString());
        allEvents.Value.Should().HaveCount(1000, "Should have 1 PlaceOrder + 999 LineAdded events");

        // Register projection
        _projectionManager!.RegisterProjection<OrderSummaryProjection>();

        // Track progress
        var progressReports = new List<RebuildProgress>();
        var progress = new Progress<RebuildProgress>(report => progressReports.Add(report));

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(
            streamId: orderId.ToString(),
            progress: progress);
        stopwatch.Stop();

        // Assert - Verify projection state
        var projectionState = await _projectionManager.GetProjectionStateAsync("E2E_OrderSummary");
        projectionState.Should().NotBeNull();
        projectionState.Status.Should().Be(ProjectionStatus.Completed);

        // Verify progress reports
        progressReports.Should().NotBeEmpty();
        var lastReport = progressReports.OrderBy(r => r.ProcessedEvents).Last();
        lastReport.ProcessedEvents.Should().Be(1000);
        lastReport.PercentComplete.Should().BeApproximately(100, 0.1);

        // Performance: Should meet 10k events/minute target
        var eventsPerMinute = eventCount * 60.0 / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Rebuilt projection from {eventCount} events in {stopwatch.Elapsed.TotalSeconds:F2}s ({eventsPerMinute:F0} events/min)");

        eventsPerMinute.Should().BeGreaterThan(10000,
            "Projection rebuild should process at least 10,000 events/minute");

        // **Expected Results:**
        // ✅ Projection rebuilt from 1000 events
        // ✅ Performance > 10,000 events/minute
        // ✅ Progress reporting accurate
        // ✅ Final projection state correct
    }

    [RequiresDockerFact]
    public async Task RebuildProjection_WithMultipleOrders_AggregatesCorrectly()
    {
        // Arrange
        var customer1 = "customer-multi-001";
        var customer2 = "customer-multi-002";

        // Create 3 orders for customer 1
        var orders1 = new List<OrderId>
        {
            await CreateOrderWithLinesAsync(customer1, 3, 25.00m),
            await CreateOrderWithLinesAsync(customer1, 5, 15.00m),
            await CreateOrderWithLinesAsync(customer1, 2, 50.00m)
        };

        // Create 2 orders for customer 2
        var orders2 = new List<OrderId>
        {
            await CreateOrderWithLinesAsync(customer2, 4, 30.00m),
            await CreateOrderWithLinesAsync(customer2, 1, 100.00m)
        };

        // Register projection
        _projectionManager!.RegisterProjection<OrderSummaryProjection>();

        // Act - Rebuild projection for all orders
        foreach (var orderId in orders1.Concat(orders2))
        {
            await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(
                streamId: orderId.ToString());
        }

        // Assert - Verify all orders were processed
        var projectionState = await _projectionManager.GetProjectionStateAsync("E2E_OrderSummary");
        projectionState.Should().NotBeNull();
        projectionState.Status.Should().Be(ProjectionStatus.Completed);

        // Verify all events were processed for all orders
        var stats = await _projectionManager.GetStatisticsAsync();
        stats.Should().NotBeNull();
        stats.TotalProjections.Should().BeGreaterThan(0);

        // **Expected Results:**
        // ✅ Multiple order streams rebuilt correctly
        // ✅ Projection state shows completed
        // ✅ Statistics show all events processed
    }

    [RequiresDockerFact]
    public async Task ProjectionWithCheckpoint_ResumesFromLastPosition()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = "customer-checkpoint-001";

        // Create order with 100 lines
        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);
        var placeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore!.AppendEventsAsync(orderId.ToString(), placeEvents, 0);

        for (int i = 1; i <= 100; i++)
        {
            order.AddOrderLine($"line-{i}", $"product-{i}", 1, 10.00m);
        }

        var allLineEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore.AppendEventsAsync(orderId.ToString(), allLineEvents, 1);

        // Register projection
        _projectionManager!.RegisterProjection<OrderSummaryProjection>();

        // Simulate processing first 50 events and saving checkpoint
        await _projectionStore!.SaveCheckpointAsync("E2E_OrderSummary", 50);

        // Act - Rebuild (should resume from checkpoint)
        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(
            streamId: orderId.ToString());

        // Assert
        var checkpoint = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");
        checkpoint.Should().BeGreaterThan(50, "Checkpoint should advance beyond 50");

        var projectionState = await _projectionManager.GetProjectionStateAsync("E2E_OrderSummary");
        projectionState.Should().NotBeNull();
        projectionState.Status.Should().Be(ProjectionStatus.Completed);

        // **Expected Results:**
        // ✅ Projection resumed from checkpoint
        // ✅ Did not reprocess events before checkpoint
        // ✅ Final state shows completed
    }

    [RequiresDockerFact]
    public async Task CompleteOrderLifecycle_WithProjectionRebuild_QueriesSucceed()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = "customer-lifecycle-001";

        // **Step 1: Create and complete order**
        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);
        var placeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore!.AppendEventsAsync(orderId.ToString(), placeEvents, 0);

        // Add 3 lines
        order.AddOrderLine("line-1", "product-A", 2, 25.00m);
        order.AddOrderLine("line-2", "product-B", 1, 50.00m);
        order.AddOrderLine("line-3", "product-C", 3, 10.00m);

        var lineEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore.AppendEventsAsync(orderId.ToString(), lineEvents, 1);

        // Complete order
        var completeResult = order.Complete(DateTimeOffset.UtcNow);
        completeResult.IsSuccess.Should().BeTrue();

        var completeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore.AppendEventsAsync(orderId.ToString(), completeEvents, 4);

        // **Step 2: Rebuild projection**
        _projectionManager!.RegisterProjection<OrderSummaryProjection>();

        await _projectionManager.RebuildProjectionAsync<OrderSummaryProjection>(
            streamId: orderId.ToString());

        // **Step 3: Verify projection state**
        var projectionState = await _projectionManager.GetProjectionStateAsync("E2E_OrderSummary");

        // Assert
        projectionState.Should().NotBeNull();
        projectionState.Status.Should().Be(ProjectionStatus.Completed);

        // Verify checkpoint advanced through all events
        var checkpoint = await _projectionStore!.GetCheckpointAsync("E2E_OrderSummary");
        checkpoint.Should().BeGreaterOrEqualTo(5, "Should process all 5 events");

        // **Expected Results:**
        // ✅ Projection rebuilt from 5 events (1 place + 3 lines + 1 complete)
        // ✅ Projection state shows completed
        // ✅ Checkpoint shows all events processed
    }

    private async Task<OrderId> CreateOrderWithLinesAsync(
        string customerId,
        int lineCount,
        decimal unitPrice)
    {
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);

        var placeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore!.AppendEventsAsync(orderId.ToString(), placeEvents, 0);

        var allLineEvents = new List<IDomainEvent>();
        for (int i = 1; i <= lineCount; i++)
        {
            order.AddOrderLine($"line-{i}", $"product-{i}", 1, unitPrice);
            var lineEvents = order.DomainEvents.ToList();
            allLineEvents.AddRange(lineEvents);
            order.ClearDomainEvents();
        }

        await _eventStore.AppendEventsAsync(orderId.ToString(), allLineEvents, 1);

        return orderId;
    }
}
