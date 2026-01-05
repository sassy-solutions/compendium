// -----------------------------------------------------------------------
// <copyright file="LiveProjectionE2ETests.cs" company="Compendium">
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
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// E2E Test Scenario 8: Live Projection Processing.
/// Tests real-time projection updates as events are appended to the event store.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "LiveProjection")]
public sealed class LiveProjectionE2ETests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private PostgreSqlEventStore? _eventStore;
    private PostgreSqlStreamingEventStore? _streamingEventStore;
    private PostgreSqlProjectionStore? _projectionStore;
    private LiveProjectionProcessor? _liveProcessor;
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
            Console.WriteLine("⚠️ Starting TestContainer for PostgreSQL (Live Projection E2E)...");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("compendium_live_projection_e2e")
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
            options.TableName = "live_projection_events_e2e";
            options.AutoCreateSchema = true;
            options.BatchSize = 1000;
        });

        // Configure projection options
        services.Configure<ProjectionOptions>(options =>
        {
            options.RebuildBatchSize = 100;
            options.MaxConcurrentRebuilds = 2;
            options.ProgressReportInterval = 50;
            options.EnableSnapshots = false; // Disable for clearer testing
            options.SnapshotInterval = TimeSpan.FromHours(1);
        });

        // Register services
        var eventDeserializer = new E2EEventDeserializer();
        services.AddSingleton<IEventDeserializer>(eventDeserializer);
        services.AddSingleton<PostgreSqlEventStore>();
        services.AddSingleton<IStreamingEventStore, PostgreSqlStreamingEventStore>();
        services.AddSingleton<IProjectionStore, PostgreSqlProjectionStore>();

        var provider = services.BuildServiceProvider();

        _eventStore = provider.GetRequiredService<PostgreSqlEventStore>();
        _streamingEventStore = (PostgreSqlStreamingEventStore)provider.GetRequiredService<IStreamingEventStore>();
        _projectionStore = (PostgreSqlProjectionStore)provider.GetRequiredService<IProjectionStore>();

        // Drop and recreate tables to ensure clean state with proper constraints
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync($"DROP TABLE IF EXISTS live_projection_events_e2e");
        await connection.ExecuteAsync($"DROP TABLE IF EXISTS projection_checkpoints");
        await connection.CloseAsync();

        // Initialize schemas
        var initResult = await _eventStore.InitializeSchemaAsync();
        initResult.IsSuccess.Should().BeTrue();

        var streamInitResult = await _streamingEventStore.InitializeSchemaAsync();
        streamInitResult.IsSuccess.Should().BeTrue();

        await _projectionStore.InitializeAsync();

        // Create LiveProjectionProcessor manually (not as a hosted service)
        var logger = provider.GetRequiredService<ILogger<LiveProjectionProcessor>>();
        var projectionOptions = provider.GetRequiredService<IOptions<ProjectionOptions>>();
        _liveProcessor = new LiveProjectionProcessor(
            _streamingEventStore,
            _projectionStore,
            provider,
            logger,
            projectionOptions);
    }

    public async Task DisposeAsync()
    {
        if (_liveProcessor != null)
        {
            await _liveProcessor.StopAsync(CancellationToken.None);
        }

        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Fact]
    public async Task LiveProcessor_UpdatesProjectionInRealTime_WithinLatencyTarget()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = "customer-live-001";

        // Register projection and start processor
        _liveProcessor!.RegisterProjection<OrderSummaryProjection>();
        await _liveProcessor.StartAsync(CancellationToken.None);

        // Wait for processor initialization
        await Task.Delay(200);

        // **Step 1: Create order and verify projection updates**
        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);
        var placeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var sw1 = Stopwatch.StartNew();
        await _eventStore!.AppendEventsAsync(orderId.ToString(), placeEvents, 0);

        // Wait for live processor to pick up event (polling interval + processing time)
        await Task.Delay(300); // 100ms poll + 200ms buffer
        sw1.Stop();

        // Query projection state
        var projection1 = GetProjectionInstance();
        var summary1 = projection1.GetOrderSummary(orderId.ToString());

        summary1.Should().NotBeNull("Projection should have processed OrderPlaced event");
        summary1!.OrderId.Should().Be(orderId.ToString());
        summary1.CustomerId.Should().Be(customerId);
        summary1.Status.Should().Be("Created");
        summary1.LineCount.Should().Be(0);

        Console.WriteLine($"Event 1 latency: {sw1.ElapsedMilliseconds}ms");
        sw1.ElapsedMilliseconds.Should().BeLessThan(500, "OrderPlaced event should be processed within 500ms");

        // **Step 2: Add order lines and verify real-time updates**
        order.AddOrderLine("line-1", "product-A", 2, 25.00m);
        order.AddOrderLine("line-2", "product-B", 1, 50.00m);
        order.AddOrderLine("line-3", "product-C", 3, 10.00m);

        var lineEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var sw2 = Stopwatch.StartNew();
        await _eventStore.AppendEventsAsync(orderId.ToString(), lineEvents, 1);

        // Wait for live processor
        await Task.Delay(300);
        sw2.Stop();

        // Query updated projection
        var projection2 = GetProjectionInstance();
        var summary2 = projection2.GetOrderSummary(orderId.ToString());

        summary2.Should().NotBeNull();
        summary2!.LineCount.Should().Be(3, "All 3 order lines should be reflected");
        summary2.TotalAmount.Should().Be((2 * 25.00m) + (1 * 50.00m) + (3 * 10.00m));

        Console.WriteLine($"Events 2-4 latency: {sw2.ElapsedMilliseconds}ms");
        sw2.ElapsedMilliseconds.Should().BeLessThan(500, "OrderLineAdded events should be processed within 500ms");

        // **Step 3: Complete order and verify final state**
        var completeResult = order.Complete(DateTimeOffset.UtcNow);
        completeResult.IsSuccess.Should().BeTrue();

        var completeEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var sw3 = Stopwatch.StartNew();
        await _eventStore.AppendEventsAsync(orderId.ToString(), completeEvents, 4);

        // Wait for live processor
        await Task.Delay(300);
        sw3.Stop();

        // Query final projection state
        var projection3 = GetProjectionInstance();
        var summary3 = projection3.GetOrderSummary(orderId.ToString());

        summary3.Should().NotBeNull();
        summary3!.Status.Should().Be("Completed");
        summary3.CompletedAt.Should().NotBeNull();

        Console.WriteLine($"Event 5 latency: {sw3.ElapsedMilliseconds}ms");
        sw3.ElapsedMilliseconds.Should().BeLessThan(500, "OrderCompleted event should be processed within 500ms");

        // **Step 4: Verify processor status**
        var status = _liveProcessor.GetStatus();
        status.IsRunning.Should().BeTrue();
        status.RegisteredProjections.Should().Be(1);
        status.ActiveProjections.Should().Be(1);
        status.TotalEventsProcessed.Should().BeGreaterOrEqualTo(5);

        // **Expected Results:**
        // ✅ Projection updates within 500ms of event append
        // ✅ All 5 events processed
        // ✅ Final state matches expectations
    }

    [Fact]
    public async Task LiveProcessor_ProcessesMultipleOrders_Concurrently()
    {
        // Arrange
        _liveProcessor!.RegisterProjection<OrderSummaryProjection>();
        await _liveProcessor.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        // **Step 1: Create 5 orders concurrently**
        var orderIds = new List<OrderId>();
        var appendTasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            var orderId = OrderId.New();
            orderIds.Add(orderId);

            var order = OrderAggregate.PlaceOrder(orderId, $"customer-concurrent-{i:000}", DateTimeOffset.UtcNow);
            order.AddOrderLine($"line-{i}", $"product-{i}", i + 1, 10.00m * (i + 1));

            var events = order.DomainEvents.ToList();
            order.ClearDomainEvents();

            appendTasks.Add(_eventStore!.AppendEventsAsync(orderId.ToString(), events, 0));
        }

        var sw = Stopwatch.StartNew();
        await Task.WhenAll(appendTasks);

        // Wait for live processor to catch up
        await Task.Delay(500);
        sw.Stop();

        // **Step 2: Verify all orders processed**
        var projection = GetProjectionInstance();

        foreach (var orderId in orderIds)
        {
            var summary = projection.GetOrderSummary(orderId.ToString());
            summary.Should().NotBeNull($"Order {orderId} should be processed");
            summary!.LineCount.Should().Be(1);
        }

        Console.WriteLine($"5 concurrent orders processed in {sw.ElapsedMilliseconds}ms");

        // **Step 3: Verify processor statistics**
        var status = _liveProcessor.GetStatus();
        status.TotalEventsProcessed.Should().BeGreaterOrEqualTo(10, "At least 10 events (5 orders x 2 events each)");

        // **Expected Results:**
        // ✅ All 5 orders processed
        // ✅ Concurrent processing succeeds
        // ✅ No events missed
    }

    [Fact]
    public async Task LiveProcessor_GracefulShutdown_SavesCheckpoint()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = "customer-shutdown-001";

        _liveProcessor!.RegisterProjection<OrderSummaryProjection>();
        await _liveProcessor.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        // **Step 1: Append events**
        var order = OrderAggregate.PlaceOrder(orderId, customerId, DateTimeOffset.UtcNow);
        order.AddOrderLine("line-1", "product-A", 1, 10.00m);

        var events = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        await _eventStore!.AppendEventsAsync(orderId.ToString(), events, 0);

        // Wait for processing
        await Task.Delay(300);

        // **Step 2: Get checkpoint before shutdown**
        var checkpointBefore = await _projectionStore!.GetCheckpointAsync("E2E_OrderSummary");
        checkpointBefore.Should().BeGreaterThan(0, "Checkpoint should be saved during processing");

        // **Step 3: Graceful shutdown**
        await _liveProcessor.StopAsync(CancellationToken.None);

        // **Step 4: Verify final checkpoint saved**
        var checkpointAfter = await _projectionStore.GetCheckpointAsync("E2E_OrderSummary");
        checkpointAfter.Should().NotBeNull();
        checkpointAfter!.Value.Should().BeGreaterOrEqualTo(checkpointBefore!.Value, "Final checkpoint should be saved on shutdown");

        // **Step 5: Verify processor stopped**
        var status = _liveProcessor.GetStatus();
        status.IsRunning.Should().BeFalse();

        // **Expected Results:**
        // ✅ Checkpoint saved on shutdown
        // ✅ Processor stopped gracefully
    }

    [Fact]
    public async Task LiveProcessor_StartStop_CanRestart()
    {
        // Arrange
        _liveProcessor!.RegisterProjection<OrderSummaryProjection>();

        // **Step 1: Start processor**
        await _liveProcessor.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        var status1 = _liveProcessor.GetStatus();
        status1.IsRunning.Should().BeTrue();

        // **Step 2: Stop processor**
        await _liveProcessor.StopAsync(CancellationToken.None);
        await Task.Delay(100);

        var status2 = _liveProcessor.GetStatus();
        status2.IsRunning.Should().BeFalse();

        // **Step 3: Restart processor**
        await _liveProcessor.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        var status3 = _liveProcessor.GetStatus();
        status3.IsRunning.Should().BeTrue();

        // **Step 4: Append event and verify processing resumes**
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-restart-001", DateTimeOffset.UtcNow);
        var events = order.DomainEvents.ToList();

        await _eventStore!.AppendEventsAsync(orderId.ToString(), events, 0);

        // Wait for processing
        await Task.Delay(300);

        // Verify event processed
        var projection = GetProjectionInstance();
        var summary = projection.GetOrderSummary(orderId.ToString());
        summary.Should().NotBeNull("Event should be processed after restart");

        // **Expected Results:**
        // ✅ Processor can stop and restart
        // ✅ Processing resumes after restart
    }

    /// <summary>
    /// Helper method to get the projection instance from the live processor.
    /// Uses reflection since projections are stored internally.
    /// </summary>
    private OrderSummaryProjection GetProjectionInstance()
    {
        var liveProjectionsField = typeof(LiveProjectionProcessor)
            .GetField("_liveProjections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var liveProjections = liveProjectionsField!.GetValue(_liveProcessor)
            as System.Collections.Concurrent.ConcurrentDictionary<string, IProjection>;

        if (liveProjections!.TryGetValue("E2E_OrderSummary", out var projection))
        {
            return (OrderSummaryProjection)projection;
        }

        throw new InvalidOperationException("OrderSummaryProjection not found in live processor");
    }
}
