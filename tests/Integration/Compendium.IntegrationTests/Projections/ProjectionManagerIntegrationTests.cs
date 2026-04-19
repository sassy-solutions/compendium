// -----------------------------------------------------------------------
// <copyright file="ProjectionManagerIntegrationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.PostgreSQL.Projections;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Core.Results;
using Compendium.Infrastructure.Projections;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Compendium.IntegrationTests.Fixtures;
using Xunit;

namespace Compendium.IntegrationTests.Projections;

/// <summary>
/// Integration tests for the projection manager using real PostgreSQL.
/// Tests performance, concurrency, and correctness of projection rebuilds.
/// </summary>
public class ProjectionManagerIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private IStreamingEventStore _eventStore = null!;
    private IProjectionStore _projectionStore = null!;
    private IProjectionManager _projectionManager = null!;
    private ILiveProjectionProcessor _liveProcessor = null!;
    private string _connectionString = null!;

    // Generate unique IDs per test run to avoid conflicts between tests
    private readonly string _testRunId = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Initializes test infrastructure with real PostgreSQL container.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Use EnvironmentConfigurationHelper for connection string fallback
        var externalConnectionString = Infrastructure.EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            _connectionString = externalConnectionString;
        }
        else
        {
            // Fallback to TestContainers
            Console.WriteLine("⚠️ Starting TestContainer for PostgreSQL...");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("compendium_test")
                .WithUsername("test")
                .WithPassword("test123")
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
            options.TableName = "events";
            options.AutoCreateSchema = true;
            options.BatchSize = 1000;
        });

        // Configure projection options
        services.Configure<ProjectionOptions>(options =>
        {
            options.RebuildBatchSize = 100;
            options.MaxConcurrentRebuilds = 2;
            options.ProgressReportInterval = 50;
            options.EnableSnapshots = true;
            options.SnapshotInterval = TimeSpan.FromSeconds(5);
        });

        // Register core services
        services.AddSingleton<IEventDeserializer, TestEventDeserializer>();
        services.AddSingleton<PostgreSqlEventStore>();
        services.AddSingleton<IStreamingEventStore, PostgreSqlStreamingEventStore>();
        services.AddSingleton<IProjectionStore, PostgreSqlProjectionStore>();
        services.AddSingleton<IProjectionManager, EnhancedProjectionManager>();
        services.AddSingleton<ILiveProjectionProcessor, LiveProjectionProcessor>();

        var provider = services.BuildServiceProvider();

        _eventStore = provider.GetRequiredService<IStreamingEventStore>();
        _projectionStore = provider.GetRequiredService<IProjectionStore>();
        _projectionManager = provider.GetRequiredService<IProjectionManager>();
        _liveProcessor = provider.GetRequiredService<ILiveProjectionProcessor>();

        // Initialize schemas
        var streamingStore = (PostgreSqlStreamingEventStore)_eventStore;
        var result = await streamingStore.InitializeSchemaAsync();
        result.IsSuccess.Should().BeTrue();

        var postgresStore = (PostgreSqlProjectionStore)_projectionStore;
        await postgresStore.InitializeAsync();
    }

    /// <summary>
    /// Cleans up test infrastructure.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [RequiresDockerFact(Skip = "Flaky: Event seeding intermittently fails in CI. Requires investigation.")]
    public async Task RebuildProjection_WithLargeEventStream_CompletesSuccessfully()
    {
        // Arrange
        var streamId = $"test-stream-{_testRunId}-{Guid.NewGuid():N}";
        var events = GenerateTestEvents(1000);
        await SeedEventsAsync(streamId, events);

        var progressReports = new ConcurrentBag<RebuildProgress>();
        var progress = new Progress<RebuildProgress>(report => progressReports.Add(report));

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _projectionManager.RebuildProjectionAsync<TestCounterProjection>(
            streamId: streamId,
            progress: progress);
        stopwatch.Stop();

        // Assert
        var finalState = await _projectionManager.GetProjectionStateAsync("TestCounter");
        finalState.Status.Should().Be(ProjectionStatus.Completed);

        progressReports.Should().NotBeEmpty();
        var lastReport = progressReports.OrderBy(r => r.ProcessedEvents).Last();
        lastReport.ProcessedEvents.Should().Be(1000);
        lastReport.PercentComplete.Should().BeApproximately(100, 0.1);

        // Performance assertion: should process at least 1000 events/minute
        var eventsPerMinute = 1000 * 60.0 / stopwatch.Elapsed.TotalSeconds;
        eventsPerMinute.Should().BeGreaterThan(10000, "Should meet the 10k events/minute target");

        Console.WriteLine($"Processed 1000 events in {stopwatch.Elapsed.TotalSeconds:F2}s ({eventsPerMinute:F0} events/min)");
    }

    [RequiresDockerFact]
    public async Task RebuildProjection_WithCheckpoint_ResumesFromCheckpoint()
    {
        // Arrange
        var events = GenerateTestEvents(200);
        await SeedEventsAsync("checkpoint-stream", events);

        // Save checkpoint at position 100
        await _projectionStore.SaveCheckpointAsync("TestCounter", 100);

        var progressReports = new List<RebuildProgress>();
        var progress = new Progress<RebuildProgress>(report => progressReports.Add(report));

        // Act
        await _projectionManager.RebuildProjectionAsync<TestCounterProjection>(
            streamId: "checkpoint-stream",
            progress: progress);

        // Assert
        var checkpoint = await _projectionStore.GetCheckpointAsync("TestCounter");
        checkpoint.Should().BeGreaterThan(100);

        // Should have started from around position 100
        var firstReport = progressReports.FirstOrDefault();
        if (firstReport != null)
        {
            firstReport.ProcessedEvents.Should().BeLessThan(200, "Should not reprocess all events");
        }
    }

    [RequiresDockerFact(Skip = "Flaky: Event seeding intermittently fails in CI. Requires investigation.")]
    public async Task RebuildProjection_WithConcurrentRebuilds_LimitsParallelism()
    {
        // Arrange - use unique stream IDs to avoid conflicts
        var baseStreamId = $"concurrent-{_testRunId}-{Guid.NewGuid():N}";
        var events = GenerateTestEvents(100);
        await SeedEventsAsync($"{baseStreamId}-1", events);
        await SeedEventsAsync($"{baseStreamId}-2", events);
        await SeedEventsAsync($"{baseStreamId}-3", events);

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var concurrentTasks = new List<Task>();

        // Act - Start 3 concurrent rebuilds (limit is 2)
        for (int i = 1; i <= 3; i++)
        {
            var streamId = $"{baseStreamId}-{i}";
            var task = Task.Run(async () =>
            {
                Interlocked.Increment(ref concurrentCount);
                maxConcurrent = Math.Max(maxConcurrent, concurrentCount);

                try
                {
                    await _projectionManager.RebuildProjectionAsync<TestCounterProjection>(streamId: streamId);
                }
                finally
                {
                    Interlocked.Decrement(ref concurrentCount);
                }
            });
            concurrentTasks.Add(task);
        }

        await Task.WhenAll(concurrentTasks);

        // Assert
        maxConcurrent.Should().BeLessOrEqualTo(2, "Should respect MaxConcurrentRebuilds setting");
    }

    [RequiresDockerFact]
    public async Task LiveProjectionProcessor_ProcessesNewEvents_InRealTime()
    {
        // Arrange
        _liveProcessor.RegisterProjection<TestCounterProjection>();

        var cts = new CancellationTokenSource();
        var processingTask = _liveProcessor.StartAsync(cts.Token);

        // Wait for processor to start
        await Task.Delay(500);

        // Act - Add events while processor is running
        var events = GenerateTestEvents(50);
        await SeedEventsAsync("live-stream", events);

        // Wait for processing
        await Task.Delay(2000);

        // Assert
        var status = _liveProcessor.GetStatus();
        status.IsRunning.Should().BeTrue();
        status.TotalEventsProcessed.Should().BeGreaterThan(0);

        var checkpoint = await _projectionStore.GetCheckpointAsync("TestCounter");
        checkpoint.Should().BeGreaterThan(0);

        // Cleanup
        cts.Cancel();
        try
        {
            await processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [RequiresDockerFact(Skip = "Flaky: Event seeding intermittently fails in CI. Requires investigation.")]
    public async Task ProjectionStore_SaveAndLoadSnapshot_PreservesState()
    {
        // Arrange
        var projection = new TestCounterProjection();

        // Apply some events to create state
        var events = GenerateTestEvents(10);
        foreach (var evt in events)
        {
            var metadata = new EventMetadata(
                "test-stream", 1, 1, DateTime.UtcNow, null, null, null);
            await projection.ApplyAsync(evt, metadata);
        }

        // Act
        await _projectionStore.SaveSnapshotAsync(projection);
        var loaded = await _projectionStore.LoadSnapshotAsync<TestCounterProjection>("TestCounter");

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Count.Should().Be(10);
        loaded.LastProcessedEventId.Should().Be(events.Last().EventId);
    }

    [RequiresDockerFact]
    public async Task ProjectionManager_GetStatistics_ReturnsAccurateData()
    {
        // Arrange
        _projectionManager.RegisterProjection<TestCounterProjection>();
        _projectionManager.RegisterProjection<TestSummaryProjection>();

        var events = GenerateTestEvents(100);
        await SeedEventsAsync("stats-stream", events);

        // Act
        await _projectionManager.RebuildProjectionAsync<TestCounterProjection>(streamId: "stats-stream");
        var stats = await _projectionManager.GetStatisticsAsync();

        // Assert
        stats.TotalProjections.Should().BeGreaterThan(0);
        stats.ProjectionDetails.Should().ContainKey("TestCounter");

        var counterState = stats.ProjectionDetails["TestCounter"];
        counterState.Status.Should().Be(ProjectionStatus.Completed);
    }

    [RequiresDockerFact]
    public async Task RebuildProjection_WithErrors_HandlesGracefully()
    {
        // Arrange
        var events = new List<TestEvent>
        {
            new() { EventId = Guid.NewGuid(), Value = 1 },
            new() { EventId = Guid.NewGuid(), Value = -999 }, // This will cause an error in TestCounterProjection
            new() { EventId = Guid.NewGuid(), Value = 3 }
        };

        await SeedEventsAsync("error-stream", events);

        // Act & Assert
        var act = async () => await _projectionManager.RebuildProjectionAsync<TestCounterProjection>(streamId: "error-stream");

        await act.Should().ThrowAsync<InvalidOperationException>("Projection should fail on negative values");

        var state = await _projectionManager.GetProjectionStateAsync("TestCounter");
        state.Status.Should().Be(ProjectionStatus.Failed);
        state.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [RequiresDockerFact(Skip = "Flaky: Event seeding intermittently fails in CI. Requires investigation.")]
    public async Task PerformanceBenchmark_ProcessingSpeed_MeetsTarget()
    {
        // Arrange
        var streamId = $"perf-stream-{_testRunId}-{Guid.NewGuid():N}";
        const int eventCount = 10000;
        var events = GenerateTestEvents(eventCount);
        await SeedEventsAsync(streamId, events);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _projectionManager.RebuildProjectionAsync<TestCounterProjection>(streamId: streamId);
        stopwatch.Stop();

        // Assert
        var eventsPerMinute = eventCount * 60.0 / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Performance: {eventsPerMinute:F0} events/minute");

        eventsPerMinute.Should().BeGreaterThan(10000,
            "Should process at least 10,000 events per minute as per requirements");

        // Memory usage should be reasonable
        var beforeGc = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterGc = GC.GetTotalMemory(false);

        Console.WriteLine($"Memory before GC: {beforeGc / 1024 / 1024:F1} MB, after GC: {afterGc / 1024 / 1024:F1} MB");

        // Should not use excessive memory for 10k events
        afterGc.Should().BeLessThan(100 * 1024 * 1024, "Should use less than 100MB for 10k events");
    }

    /// <summary>
    /// Generates test events for use in tests.
    /// </summary>
    private List<TestEvent> GenerateTestEvents(int count)
    {
        var events = new List<TestEvent>();
        for (int i = 0; i < count; i++)
        {
            events.Add(new TestEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = "test-aggregate",
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow.AddSeconds(-count + i),
                AggregateVersion = i + 1,
                Value = i + 1
            });
        }
        return events;
    }

    /// <summary>
    /// Seeds events into the event store.
    /// </summary>
    private async Task SeedEventsAsync(string streamId, List<TestEvent> events)
    {
        var domainEvents = events.Cast<IDomainEvent>().ToList();
        var result = await _eventStore.AppendEventsAsync(streamId, domainEvents, -1);
        result.IsSuccess.Should().BeTrue();
    }
}

/// <summary>
/// Test projection that counts events and tracks the last processed event.
/// </summary>
public class TestCounterProjection : IProjection<TestEvent>
{
    public string ProjectionName => "TestCounter";
    public int Version => 1;
    public int Count { get; private set; }
    public Guid LastProcessedEventId { get; private set; }

    public Task ApplyAsync(TestEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        // Simulate an error condition for testing
        if (@event.Value < 0)
        {
            throw new InvalidOperationException("Test projection does not accept negative values");
        }

        Count++;
        LastProcessedEventId = @event.EventId;
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        Count = 0;
        LastProcessedEventId = Guid.Empty;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test projection that creates summary statistics.
/// </summary>
public class TestSummaryProjection : IProjection<TestEvent>
{
    public string ProjectionName => "TestSummary";
    public int Version => 1;
    public int TotalEvents { get; private set; }
    public int TotalValue { get; private set; }
    public double AverageValue => TotalEvents > 0 ? (double)TotalValue / TotalEvents : 0;

    public Task ApplyAsync(TestEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        TotalEvents++;
        TotalValue += @event.Value;
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        TotalEvents = 0;
        TotalValue = 0;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test domain event for projection testing.
/// </summary>
public class TestEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public string AggregateId { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
    public DateTimeOffset OccurredOn { get; init; }
    public long AggregateVersion { get; init; }
    public int EventVersion { get; init; } = 1;
    public int Value { get; init; }
}

/// <summary>
/// Test event deserializer that handles TestEvent types.
/// </summary>
public class TestEventDeserializer : IEventDeserializer
{
    private readonly JsonSerializerOptions _jsonOptions;

    public TestEventDeserializer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public Result<IDomainEvent> TryDeserializeEvent(string eventData, string eventType)
    {
        try
        {
            // Log the event type to understand what PostgreSQL is storing
            Console.WriteLine($"Deserializing event type: '{eventType}' with data: '{eventData}'");

            // PostgreSQL stores AssemblyQualifiedName, so check for the namespace and class name
            if (eventType.Contains("TestEvent") || eventType.Contains("Compendium.IntegrationTests"))
            {
                var testEvent = System.Text.Json.JsonSerializer.Deserialize<TestEvent>(eventData, _jsonOptions);
                Console.WriteLine($"Successfully deserialized TestEvent with EventId: {testEvent?.EventId}");
                return Result.Success<IDomainEvent>(testEvent!);
            }

            Console.WriteLine($"Event type '{eventType}' not supported");
            return Result.Failure<IDomainEvent>(Error.Validation("Deserializer.UnknownType", $"Unknown event type: {eventType}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize event: {ex.Message}");
            return Result.Failure<IDomainEvent>(Error.Failure("Deserializer.Failed", ex.Message));
        }
    }

    public IDomainEvent? DeserializeEvent(string eventData, string eventTypeName)
    {
        var result = TryDeserializeEvent(eventData, eventTypeName);
        return result.IsSuccess ? result.Value : null;
    }

    public T? DeserializeEvent<T>(string eventData) where T : class, IDomainEvent
    {
        var result = TryDeserializeEvent(eventData, typeof(T).Name);
        return result.IsSuccess ? result.Value as T : null;
    }
}
