// -----------------------------------------------------------------------
// <copyright file="PostgreSqlStreamingEventStore.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text.Json;
using Compendium.Abstractions.EventSourcing;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Core.Results;
using Compendium.Infrastructure.Projections;
using Compendium.Multitenancy;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.EventStore;

/// <summary>
/// PostgreSQL implementation of IStreamingEventStore with optimized streaming capabilities for projections.
/// Extends the base PostgreSqlEventStore with efficient streaming methods.
/// </summary>
public sealed class PostgreSqlStreamingEventStore : IStreamingEventStore, IAsyncDisposable
{
    private readonly PostgreSqlEventStore _baseEventStore;
    private readonly PostgreSqlOptions _options;
    private readonly IEventDeserializer _eventDeserializer;
    private readonly ILogger<PostgreSqlStreamingEventStore> _logger;
    private readonly ITenantContext? _tenantContext;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _connectionSemaphore;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlStreamingEventStore"/> class.
    /// </summary>
    /// <param name="baseEventStore">The base event store implementation.</param>
    /// <param name="options">PostgreSQL configuration options.</param>
    /// <param name="eventDeserializer">The secure event deserializer.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tenantContext">The tenant context for multi-tenancy support.</param>
    public PostgreSqlStreamingEventStore(
        PostgreSqlEventStore baseEventStore,
        IOptions<PostgreSqlOptions> options,
        IEventDeserializer eventDeserializer,
        ILogger<PostgreSqlStreamingEventStore> logger,
        ITenantContext? tenantContext = null)
    {
        _baseEventStore = baseEventStore ?? throw new ArgumentNullException(nameof(baseEventStore));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _eventDeserializer = eventDeserializer ?? throw new ArgumentNullException(nameof(eventDeserializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContext = tenantContext;
        _connectionSemaphore = new SemaphoreSlim(_options.MaxPoolSize);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region IEventStore Implementation (Delegate to base)

    /// <inheritdoc />
    public Task<Result> AppendEventsAsync(
        string aggregateId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
        => _baseEventStore.AppendEventsAsync(aggregateId, events, expectedVersion, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
        => _baseEventStore.GetEventsAsync(aggregateId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        long fromVersion,
        CancellationToken cancellationToken = default)
        => _baseEventStore.GetEventsAsync(aggregateId, fromVersion, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsInRangeAsync(
        string aggregateId,
        long fromVersion,
        long toVersion,
        CancellationToken cancellationToken = default)
        => _baseEventStore.GetEventsInRangeAsync(aggregateId, fromVersion, toVersion, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IDomainEvent>> GetLastEventAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
        => _baseEventStore.GetLastEventAsync(aggregateId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<long>> GetVersionAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
        => _baseEventStore.GetVersionAsync(aggregateId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<bool>> ExistsAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
        => _baseEventStore.ExistsAsync(aggregateId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<EventStoreStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
        => _baseEventStore.GetStatisticsAsync(cancellationToken);

    #endregion

    #region IStreamingEventStore Implementation

    /// <inheritdoc />
    public async IAsyncEnumerable<EventData> StreamEventsAsync(
        string? streamId,
        long fromPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var batchSize = _options.BatchSize;
        var currentPosition = fromPosition;
        var hasMoreEvents = true;

        while (hasMoreEvents && !cancellationToken.IsCancellationRequested)
        {
            var events = await GetEventBatchAsync(streamId, currentPosition, batchSize, cancellationToken);

            if (!events.Any())
            {
                hasMoreEvents = false;
                yield break;
            }

            foreach (var eventData in events)
            {
                yield return eventData;
                currentPosition = Math.Max(currentPosition, eventData.GlobalPosition);
            }

            // If we got fewer events than the batch size, we've reached the end
            if (events.Count < batchSize)
            {
                hasMoreEvents = false;
            }
            else
            {
                currentPosition++; // Move to next position for next batch
            }
        }
    }

    /// <inheritdoc />
    public async Task<long> GetEventCountAsync(string? streamId = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM {0} 
            WHERE (@StreamId IS NULL OR stream_id = @StreamId)
              AND (@TenantId IS NULL OR tenant_id = @TenantId OR tenant_id IS NULL);
        ";

        var tableName = _options.TableName;
        var query = string.Format(sql, tableName);

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var connection = new NpgsqlConnection(_options.ConnectionString);
            var count = await connection.QuerySingleAsync<long>(query, new
            {
                StreamId = streamId,
                TenantId = GetTenantId()
            });

            return count;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<long> GetMaxGlobalPositionAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COALESCE(MAX(global_position), 0) 
            FROM {0}
            WHERE (@TenantId IS NULL OR tenant_id = @TenantId OR tenant_id IS NULL);
        ";

        var tableName = _options.TableName;
        var query = string.Format(sql, tableName);

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var connection = new NpgsqlConnection(_options.ConnectionString);
            var maxPosition = await connection.QuerySingleAsync<long>(query, new
            {
                TenantId = GetTenantId()
            });

            return maxPosition;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    #endregion

    /// <summary>
    /// Gets a batch of events for streaming.
    /// </summary>
    private async Task<List<EventData>> GetEventBatchAsync(
        string? streamId,
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var sql = $@"
            SELECT
                event_id as EventId,
                stream_id as StreamId,
                stream_position as StreamPosition,
                global_position as GlobalPosition,
                event_type as EventType,
                event_data as EventDataJson,
                occurred_on as Timestamp,
                metadata->>'userId' as UserId,
                tenant_id as TenantId,
                metadata as Headers
            FROM {_options.TableName}
            WHERE global_position > @FromPosition
              AND (@StreamId IS NULL OR stream_id = @StreamId)
              AND (@TenantId IS NULL OR tenant_id = @TenantId OR tenant_id IS NULL)
            ORDER BY global_position
            LIMIT @BatchSize;
        ";

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var connection = new NpgsqlConnection(_options.ConnectionString);
            var rawEvents = await connection.QueryAsync<RawEventData>(sql, new
            {
                FromPosition = fromPosition,
                StreamId = streamId,
                TenantId = GetTenantId(),
                BatchSize = batchSize
            });

            var events = new List<EventData>();

            foreach (var rawEvent in rawEvents)
            {
                try
                {
                    // Deserialize the domain event
                    var deserializationResult = _eventDeserializer.TryDeserializeEvent(
                        rawEvent.EventDataJson,
                        rawEvent.EventType);

                    if (deserializationResult.IsSuccess)
                    {
                        var eventData = new EventData
                        {
                            EventId = rawEvent.EventId,
                            StreamId = rawEvent.StreamId,
                            StreamPosition = rawEvent.StreamPosition,
                            GlobalPosition = rawEvent.GlobalPosition,
                            EventType = rawEvent.EventType,
                            EventDataJson = rawEvent.EventDataJson,
                            Event = deserializationResult.Value,
                            Timestamp = rawEvent.Timestamp,
                            UserId = rawEvent.UserId,
                            TenantId = rawEvent.TenantId,
                            Headers = ParseHeaders(rawEvent.Headers)
                        };

                        events.Add(eventData);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize event {EventId} of type {EventType}: {Error}",
                            rawEvent.EventId, rawEvent.EventType, deserializationResult.Error.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventId} during streaming", rawEvent.EventId);
                }
            }

            return events;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Parses headers from the metadata JSON.
    /// </summary>
    private Dictionary<string, object>? ParseHeaders(object? metadata)
    {
        if (metadata == null)
        {
            return null;
        }

        try
        {
            var jsonString = metadata.ToString();
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse event metadata headers");
            return null;
        }
    }

    /// <summary>
    /// Gets the current tenant ID for multi-tenant filtering.
    /// </summary>
    private string? GetTenantId() => _tenantContext?.TenantId;

    /// <summary>
    /// Initializes the database schema for event streaming support.
    /// </summary>
    public async Task<Result> InitializeSchemaAsync(CancellationToken cancellationToken = default)
    {
        // First initialize the base event store schema
        var baseResult = await _baseEventStore.InitializeSchemaAsync(cancellationToken);
        if (!baseResult.IsSuccess)
        {
            return baseResult;
        }

        // Add additional indexes for streaming performance
        const string sql = @"
            -- Index for efficient streaming by global position
            CREATE INDEX IF NOT EXISTS idx_events_global_position_stream
                ON {0}(global_position);

            -- Index for stream-specific streaming
            CREATE INDEX IF NOT EXISTS idx_events_stream_global_position
                ON {0}(stream_id, global_position);

            -- Index for tenant isolation in streaming
            CREATE INDEX IF NOT EXISTS idx_events_tenant_global_position
                ON {0}(tenant_id, global_position) WHERE tenant_id IS NOT NULL;
        ";

        var tableName = _options.TableName;
        var query = string.Format(sql, tableName);

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.ExecuteAsync(query);

            _logger.LogInformation("Streaming event store indexes created");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize streaming event store schema");
            return Result.Failure(Error.Failure("StreamingEventStore.InitializationFailed", ex.Message));
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Disposes the streaming event store and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _connectionSemaphore.Dispose();
        await _baseEventStore.DisposeAsync();
        _disposed = true;
    }
}

/// <summary>
/// Raw event data retrieved from PostgreSQL for streaming.
/// </summary>
internal class RawEventData
{
    public Guid EventId { get; set; }
    public string StreamId { get; set; } = string.Empty;
    public long StreamPosition { get; set; }
    public long GlobalPosition { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventDataJson { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public object? Headers { get; set; }
}
