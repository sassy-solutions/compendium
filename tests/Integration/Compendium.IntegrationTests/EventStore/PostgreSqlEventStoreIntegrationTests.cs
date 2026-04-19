// -----------------------------------------------------------------------
// <copyright file="PostgreSqlEventStoreIntegrationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Core.Results;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testcontainers.PostgreSql;
using Compendium.IntegrationTests.Fixtures;
using Xunit;

namespace Compendium.IntegrationTests.EventStore;

/// <summary>
/// Integration tests for PostgreSqlEventStore using real PostgreSQL container.
/// Tests performance, concurrency, and data integrity scenarios.
/// </summary>
public sealed class PostgreSqlEventStoreIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private PostgreSqlEventStore? _eventStore;
    private IEventDeserializer? _eventDeserializer;

    public async Task InitializeAsync()
    {
        // Use EnvironmentConfigurationHelper for connection string fallback
        var connectionString = Infrastructure.EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Use external PostgreSQL (environment variable or Docker Compose)
            var options = Options.Create(new PostgreSqlOptions
            {
                ConnectionString = connectionString,
                AutoCreateSchema = true,
                TableName = "event_store_test",
                CommandTimeout = 30,
                BatchSize = 1000
            });

            await InitializeEventStore(options);
            return;
        }

        // Fallback to TestContainers
        Console.WriteLine("⚠️ Starting TestContainer for PostgreSQL...");
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("compendium_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        await _postgres.StartAsync();

        var containerOptions = Options.Create(new PostgreSqlOptions
        {
            ConnectionString = _postgres.GetConnectionString(),
            AutoCreateSchema = true,
            TableName = "event_store_test",
            CommandTimeout = 30,
            BatchSize = 1000
        });

        await InitializeEventStore(containerOptions);
    }

    private async Task InitializeEventStore(IOptions<PostgreSqlOptions> options)
    {
        _eventDeserializer = Substitute.For<IEventDeserializer>();
        _eventDeserializer.TryDeserializeEvent(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var eventData = callInfo.ArgAt<string>(0);
                var eventType = callInfo.ArgAt<string>(1);

                // Log the event type to understand what PostgreSQL is storing
                Console.WriteLine($"Deserializing event type: '{eventType}' with data: '{eventData}'");

                // PostgreSQL stores AssemblyQualifiedName, so check for the namespace and class name
                if (eventType.Contains("TestEvent") || eventType.Contains("Compendium.IntegrationTests"))
                {
                    try
                    {
                        // Use the same JSON options as PostgreSqlEventStore (camelCase)
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = false
                        };
                        var deserializedEvent = JsonSerializer.Deserialize<TestEvent>(eventData, jsonOptions);
                        Console.WriteLine($"Successfully deserialized TestEvent with EventId: {deserializedEvent?.EventId}");
                        return Result.Success<IDomainEvent>(deserializedEvent!);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to deserialize event: {ex.Message}");
                        return Result.Failure<IDomainEvent>(Error.Failure("Deserialization.Failed", $"Failed to deserialize event: {ex.Message}"));
                    }
                }

                Console.WriteLine($"Event type '{eventType}' not supported");
                return Result.Failure<IDomainEvent>(Error.Failure("EventType.NotSupported", $"Event type '{eventType}' not supported in tests"));
            });

        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();

        _eventStore = new PostgreSqlEventStore(options, _eventDeserializer, logger);

        // Initialize the schema
        var initResult = await _eventStore.InitializeSchemaAsync();
        if (!initResult.IsSuccess)
        {
            Console.WriteLine($"Schema initialization failed: {initResult.Error.Code} - {initResult.Error.Message}");
        }
        initResult.IsSuccess.Should().BeTrue();
    }

    public async Task DisposeAsync()
    {
        if (_eventStore != null)
        {
            await _eventStore.DisposeAsync();
        }

        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    /// <summary>
    /// Cleans the test table for isolated test runs.
    /// </summary>
    private async Task CleanTestTableAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            // Skip cleaning for TestContainer tests as they use isolated containers
            return;
        }

        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM event_store_test");
    }

    [RequiresDockerFact]
    public async Task AppendEventsAsync_WithValidEvents_ShouldSucceed()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var events = new List<IDomainEvent>
        {
            new TestEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                AggregateVersion = 1,
                Data = "First event"
            },
            new TestEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                AggregateVersion = 2,
                Data = "Second event"
            }
        };

        // Act
        var result = await _eventStore!.AppendEventsAsync(aggregateId, events, 0);

        // Assert
        if (!result.IsSuccess)
        {
            Console.WriteLine($"AppendEventsAsync failed: {result.Error.Code} - {result.Error.Message}");
        }
        result.IsSuccess.Should().BeTrue();

        var retrievedEventsResult = await _eventStore.GetEventsAsync(aggregateId);
        retrievedEventsResult.IsSuccess.Should().BeTrue();
        retrievedEventsResult.Value.Should().HaveCount(2);
    }

    [RequiresDockerFact]
    public async Task AppendEventsAsync_WithConcurrentWrites_ShouldMaintainConsistency()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var tasks = new List<Task<Result>>();

        // Act - Attempt 10 concurrent writes to the same aggregate, all expecting version 0
        for (int i = 0; i < 10; i++)
        {
            var eventNumber = i; // Capture for closure
            tasks.Add(Task.Run(async () =>
            {
                var events = new[]
                {
                    new TestEvent
                    {
                        EventId = Guid.NewGuid(),
                        AggregateId = aggregateId,
                        AggregateType = "TestAggregate",
                        OccurredOn = DateTimeOffset.UtcNow,
                        AggregateVersion = 1, // All events will try to be version 1
                        Data = $"Event {eventNumber}"
                    }
                };
                return await _eventStore!.AppendEventsAsync(aggregateId, events, 0); // All expect version 0 (no events)
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Only one should succeed due to optimistic concurrency control
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => !r.IsSuccess).ToList();

        successfulResults.Should().HaveCount(1);
        failedResults.Should().HaveCount(9);
        failedResults.Should().AllSatisfy(r =>
            r.Error.Code.Should().Be("EventStore.ConcurrencyConflict"));
    }

    [RequiresDockerFact]
    public async Task GetEventsAsync_WithFromVersion_ShouldReturnCorrectEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var events = Enumerable.Range(1, 5).Select(i => new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = i,
            Data = $"Event {i}"
        }).Cast<IDomainEvent>().ToList();

        await _eventStore!.AppendEventsAsync(aggregateId, events, 0);

        // Act
        var result = await _eventStore.GetEventsAsync(aggregateId, 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3); // Events 3, 4, 5
    }

    [RequiresDockerFact]
    public async Task GetEventsInRangeAsync_ShouldReturnEventsInRange()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var events = Enumerable.Range(1, 10).Select(i => new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = i,
            Data = $"Event {i}"
        }).Cast<IDomainEvent>().ToList();

        await _eventStore!.AppendEventsAsync(aggregateId, events, 0);

        // Act
        var result = await _eventStore.GetEventsInRangeAsync(aggregateId, 3, 7);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5); // Events 3, 4, 5, 6, 7
    }

    [RequiresDockerFact]
    public async Task GetVersionAsync_ShouldReturnCorrectVersion()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var events = Enumerable.Range(1, 3).Select(i => new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = i,
            Data = $"Event {i}"
        }).Cast<IDomainEvent>().ToList();

        await _eventStore!.AppendEventsAsync(aggregateId, events, 0);

        // Act
        var result = await _eventStore.GetVersionAsync(aggregateId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [RequiresDockerFact]
    public async Task ExistsAsync_ShouldReturnCorrectValue()
    {
        // Arrange
        var existingAggregateId = Guid.NewGuid().ToString();
        var nonExistingAggregateId = Guid.NewGuid().ToString();

        var events = new[]
        {
            new TestEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = existingAggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                AggregateVersion = 1,
                Data = "Test event"
            }
        };

        await _eventStore!.AppendEventsAsync(existingAggregateId, events, 0);

        // Act & Assert
        var existsResult = await _eventStore.ExistsAsync(existingAggregateId);
        existsResult.IsSuccess.Should().BeTrue();
        existsResult.Value.Should().BeTrue();

        var notExistsResult = await _eventStore.ExistsAsync(nonExistingAggregateId);
        notExistsResult.IsSuccess.Should().BeTrue();
        notExistsResult.Value.Should().BeFalse();
    }

    [RequiresDockerFact(Skip = "Flaky: Performance varies significantly in CI environments. Requires investigation.")]
    public async Task PerformanceTest_ShouldHandle1000EventsQuickly()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var events = Enumerable.Range(1, 1000).Select(i => new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = i,
            Data = $"Event {i}"
        }).Cast<IDomainEvent>().ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        // Use -1 for new stream (expectedVersion: 0 means "stream exists with version 0")
        var result = await _eventStore!.AppendEventsAsync(aggregateId, events, -1);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        Console.WriteLine($"Performance test took {stopwatch.ElapsedMilliseconds}ms");
        // Increased tolerance for remote PostgreSQL database
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(60000); // Should complete within 60 seconds for remote DB

        // Verify all events were stored
        var retrievedResult = await _eventStore.GetEventsAsync(aggregateId);
        retrievedResult.IsSuccess.Should().BeTrue();
        retrievedResult.Value.Should().HaveCount(1000);
    }

    [RequiresDockerFact]
    public async Task GetStatisticsAsync_ShouldReturnAccurateStatistics()
    {
        // Arrange - Clean table first for accurate statistics
        await CleanTestTableAsync();

        var aggregateId1 = Guid.NewGuid().ToString();
        var aggregateId2 = Guid.NewGuid().ToString();

        var events1 = Enumerable.Range(1, 3).Select(i => new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId1,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = i,
            Data = $"Event {i}"
        }).Cast<IDomainEvent>().ToList();

        var events2 = Enumerable.Range(1, 2).Select(i => new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId2,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = i,
            Data = $"Event {i}"
        }).Cast<IDomainEvent>().ToList();

        await _eventStore!.AppendEventsAsync(aggregateId1, events1, 0);
        await _eventStore.AppendEventsAsync(aggregateId2, events2, 0);

        // Act
        var result = await _eventStore.GetStatisticsAsync();

        // Assert
        if (!result.IsSuccess)
        {
            Console.WriteLine($"GetStatisticsAsync failed: {result.Error.Code} - {result.Error.Message}");
        }
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAggregates.Should().Be(2);
        result.Value.TotalEvents.Should().Be(5);
        result.Value.AggregateStatistics.Should().ContainKey(aggregateId1);
        result.Value.AggregateStatistics.Should().ContainKey(aggregateId2);
        result.Value.AggregateStatistics[aggregateId1].EventCount.Should().Be(3);
        result.Value.AggregateStatistics[aggregateId2].EventCount.Should().Be(2);
    }

    /// <summary>
    /// Test event class for integration testing.
    /// </summary>
    private sealed class TestEvent : IDomainEvent
    {
        public Guid EventId { get; init; }
        public string AggregateId { get; init; } = string.Empty;
        public string AggregateType { get; init; } = string.Empty;
        public DateTimeOffset OccurredOn { get; init; }
        public long AggregateVersion { get; init; }
        public int EventVersion { get; init; } = 1;
        public string Data { get; init; } = string.Empty;
    }
}
