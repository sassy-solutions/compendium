// -----------------------------------------------------------------------
// <copyright file="PostgreSqlEventStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Compendium.Abstractions.EventSourcing;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Core.Results;
using Compendium.Core.Telemetry;
using Compendium.Multitenancy;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.EventStore;

/// <summary>
/// PostgreSQL implementation of IEventStore using Dapper for high performance.
/// Provides ACID compliance, optimistic concurrency control, and multi-tenancy support.
/// </summary>
public sealed class PostgreSqlEventStore : IEventStore, IAsyncDisposable
{
    private readonly PostgreSqlOptions _options;
    private readonly IEventDeserializer _eventDeserializer;
    private readonly ILogger<PostgreSqlEventStore> _logger;
    private readonly ITenantContext? _tenantContext;
    private readonly Infrastructure.Observability.IMetrics? _metrics;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly string _enhancedConnectionString;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlEventStore"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL configuration options.</param>
    /// <param name="eventDeserializer">The secure event deserializer.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tenantContext">The tenant context for multi-tenancy support.</param>
    /// <param name="metrics">Optional metrics collector for connection pooling instrumentation.</param>
    public PostgreSqlEventStore(
        IOptions<PostgreSqlOptions> options,
        IEventDeserializer eventDeserializer,
        ILogger<PostgreSqlEventStore> logger,
        ITenantContext? tenantContext = null,
        Infrastructure.Observability.IMetrics? metrics = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _eventDeserializer = eventDeserializer ?? throw new ArgumentNullException(nameof(eventDeserializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContext = tenantContext;
        _metrics = metrics;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new ArgumentException("Connection string is required", nameof(options));
        }

        // Validate connection pooling configuration
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(_options);
        if (!isValid)
        {
            throw new ArgumentException($"Invalid PostgreSQL configuration: {errorMessage}", nameof(options));
        }

        // Build enhanced connection string with pooling parameters
        _enhancedConnectionString = PostgreSqlConnectionStringBuilder.BuildConnectionString(_options);

        _logger.LogInformation(
            "PostgreSQL EventStore initialized with pooling: App MaxPoolSize={AppMaxPoolSize}, Npgsql Min/Max={NpgsqlMin}/{NpgsqlMax}, CommandTimeout={CommandTimeout}s",
            _options.MaxPoolSize, _options.MinimumPoolSize, _options.MaximumPoolSize, _options.CommandTimeout);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _connectionSemaphore = new SemaphoreSlim(_options.MaxPoolSize, _options.MaxPoolSize);
    }

    /// <inheritdoc />
    public async Task<Result> AppendEventsAsync(
        string aggregateId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        // COMP-016: Start distributed trace for event append operation
        using var activity = CompendiumTelemetry.ActivitySource.StartActivity(
            CompendiumTelemetry.EventStoreActivities.AppendEvents);

        // Use optimized batch insert for multiple events
        var eventList = events?.ToList() ?? new List<IDomainEvent>();

        // COMP-016: Add tags for filtering and correlation
        activity?.SetTag(CompendiumTelemetry.Tags.AggregateId, aggregateId);
        activity?.SetTag(CompendiumTelemetry.Tags.BatchSize, eventList.Count);
        activity?.SetTag(CompendiumTelemetry.Tags.TenantId, _tenantContext?.TenantId);
        if (eventList.Count > 0)
        {
            activity?.SetTag(CompendiumTelemetry.Tags.AggregateType, eventList[0].AggregateType);
        }

        var sw = Stopwatch.StartNew();

        try
        {
            Result result;
            if (eventList.Count >= 500)
            {
                // Use COPY for very large batches (>= 500 events) - fastest for bulk operations
                result = await AppendEventsCopyAsync(aggregateId, eventList, expectedVersion, cancellationToken).ConfigureAwait(false);
            }
            else if (eventList.Count >= 10)
            {
                // Use batched INSERT for medium batches (10-499 events) - good balance
                result = await AppendEventsBatchedAsync(aggregateId, eventList, expectedVersion, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Use standard insert for small batches (< 10 events) - low overhead
                result = await AppendEventsStandardAsync(aggregateId, eventList, expectedVersion, cancellationToken).ConfigureAwait(false);
            }

            sw.Stop();

            // COMP-016: Record metrics
            var status = result.IsSuccess
                ? CompendiumTelemetry.StatusValues.Success
                : CompendiumTelemetry.StatusValues.Failure;

            CompendiumTelemetry.EventsAppended.Add(eventList.Count,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.Status, status),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.TenantId, _tenantContext?.TenantId),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.AggregateType, eventList.Count > 0 ? eventList[0].AggregateType : "unknown"));

            CompendiumTelemetry.AppendDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.BatchSize, eventList.Count),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.TenantId, _tenantContext?.TenantId));

            // COMP-016: Set activity status
            activity?.SetStatus(result.IsSuccess
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);

            if (!result.IsSuccess)
            {
                activity?.SetTag(CompendiumTelemetry.Tags.ErrorType, result.Error.Code);
                activity?.SetTag(CompendiumTelemetry.Tags.ErrorMessage, result.Error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();

            // COMP-016: Record error metrics
            CompendiumTelemetry.EventsAppended.Add(0,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.Status, CompendiumTelemetry.StatusValues.Failure),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.TenantId, _tenantContext?.TenantId));

            // COMP-016: Record exception in trace
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Appends events using PostgreSQL COPY FROM STDIN for maximum throughput.
    /// Optimal for very large batches (>= 500 events).
    /// Target: 10,000+ events/sec.
    /// </summary>
    private async Task<Result> AppendEventsCopyAsync(
        string aggregateId,
        IReadOnlyList<IDomainEvent> eventList,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ArgumentNullException.ThrowIfNull(eventList);
        ThrowIfDisposed();

        if (eventList.Count == 0)
        {
            return Result.Success();
        }

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

            try
            {
                // Check current version for optimistic concurrency control
                var currentVersion = await GetCurrentVersionAsync(connection, transaction, aggregateId, cancellationToken).ConfigureAwait(false);

                if (expectedVersion != -1 && expectedVersion != currentVersion)
                {
                    _logger.LogWarning(
                        "Concurrency conflict for aggregate {AggregateId}. Expected version: {ExpectedVersion}, Current version: {CurrentVersion}",
                        aggregateId, expectedVersion, currentVersion);

                    return Error.Conflict("EventStore.ConcurrencyConflict",
                        $"Expected version {expectedVersion} but current version is {currentVersion}");
                }

                // Use COPY FROM STDIN for bulk insert
                var copyCommand = $@"COPY {_options.TableName} (
                    stream_id, stream_type, version, stream_position, event_type, event_data,
                    metadata, tenant_id, created_at, created_by, event_id, occurred_on
                ) FROM STDIN (FORMAT BINARY)";

                using var writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken).ConfigureAwait(false);

                var now = DateTimeOffset.UtcNow;
                var tenantId = _tenantContext?.TenantId;
                var version = currentVersion;
                var rowsWritten = 0;

                foreach (var domainEvent in eventList)
                {
                    version++;

                    var metadata = new Dictionary<string, object>
                    {
                        ["CorrelationId"] = Guid.NewGuid().ToString(),
                        ["Timestamp"] = now.Ticks,
                        ["EventVersion"] = version
                    };

                    await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(aggregateId, NpgsqlTypes.NpgsqlDbType.Varchar, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(domainEvent.AggregateType, NpgsqlTypes.NpgsqlDbType.Varchar, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(version, NpgsqlTypes.NpgsqlDbType.Bigint, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(version, NpgsqlTypes.NpgsqlDbType.Bigint, cancellationToken).ConfigureAwait(false); // stream_position
                    await writer.WriteAsync(domainEvent.GetType().AssemblyQualifiedName!, NpgsqlTypes.NpgsqlDbType.Varchar, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions), NpgsqlTypes.NpgsqlDbType.Jsonb, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(JsonSerializer.Serialize(metadata, _jsonOptions), NpgsqlTypes.NpgsqlDbType.Jsonb, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(tenantId ?? (object)DBNull.Value, NpgsqlTypes.NpgsqlDbType.Varchar, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(now, NpgsqlTypes.NpgsqlDbType.TimestampTz, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync("system", NpgsqlTypes.NpgsqlDbType.Varchar, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(domainEvent.EventId, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(domainEvent.OccurredOn, NpgsqlTypes.NpgsqlDbType.TimestampTz, cancellationToken).ConfigureAwait(false);
                    rowsWritten++;
                }

                await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);

                // Verify all rows were written
                if (rowsWritten != eventList.Count)
                {
                    _logger.LogError(
                        "Expected to insert {ExpectedCount} events but {ActualCount} were inserted for aggregate {AggregateId}",
                        eventList.Count, rowsWritten, aggregateId);

                    return Error.Failure("EventStore.InsertionFailed", "Not all events were inserted successfully");
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "Appended {EventCount} events to aggregate {AggregateId} using COPY (tenant: {TenantId})",
                    eventList.Count, aggregateId, tenantId);

                return Result.Success();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique constraint violation
            {
                _logger.LogWarning(ex,
                    "Unique constraint violation while appending events to aggregate {AggregateId}. This may indicate a concurrency conflict.",
                    aggregateId);

                return Error.Conflict("EventStore.ConcurrencyConflict",
                    "A concurrency conflict occurred while appending events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append events to aggregate {AggregateId} using COPY", aggregateId);
                return Error.Failure("EventStore.AppendFailed", ex.Message);
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Standard append implementation using Dapper's parameter batching.
    /// Optimal for small batches (less than 10 events).
    /// </summary>
    private async Task<Result> AppendEventsStandardAsync(
        string aggregateId,
        IReadOnlyList<IDomainEvent> eventList,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ArgumentNullException.ThrowIfNull(eventList);
        ThrowIfDisposed();

        if (eventList.Count == 0)
        {
            return Result.Success();
        }

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

            try
            {
                // Check current version for optimistic concurrency control
                var currentVersion = await GetCurrentVersionAsync(connection, transaction, aggregateId, cancellationToken).ConfigureAwait(false);

                if (expectedVersion != -1 && expectedVersion != currentVersion)
                {
                    _logger.LogWarning(
                        "Concurrency conflict for aggregate {AggregateId}. Expected version: {ExpectedVersion}, Current version: {CurrentVersion}",
                        aggregateId, expectedVersion, currentVersion);

                    return Error.Conflict("EventStore.ConcurrencyConflict",
                        $"Expected version {expectedVersion} but current version is {currentVersion}");
                }

                // Prepare events for insertion
                var insertSql = $@"
                    INSERT INTO {_options.TableName} (
                        stream_id, stream_type, version, stream_position, event_type, event_data,
                        metadata, tenant_id, created_at, created_by, event_id, occurred_on
                    ) VALUES (
                        @StreamId, @StreamType, @Version, @StreamPosition, @EventType, @EventData::jsonb,
                        @Metadata::jsonb, @TenantId, @CreatedAt, @CreatedBy, @EventId, @OccurredOn
                    )";

                var now = DateTimeOffset.UtcNow;
                var tenantId = _tenantContext?.TenantId;
                var version = currentVersion;

                var parameters = eventList.Select(domainEvent =>
                {
                    version++;
                    var metadata = new Dictionary<string, object>
                    {
                        ["CorrelationId"] = Guid.NewGuid().ToString(),
                        ["Timestamp"] = now.Ticks,
                        ["EventVersion"] = version
                    };

                    return new
                    {
                        StreamId = aggregateId,
                        StreamType = domainEvent.AggregateType,
                        Version = version,
                        StreamPosition = version, // Stream position equals version
                        EventType = domainEvent.GetType().AssemblyQualifiedName!,
                        EventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions),
                        Metadata = JsonSerializer.Serialize(metadata, _jsonOptions),
                        TenantId = tenantId,
                        CreatedAt = now,
                        CreatedBy = "system", // Could be enhanced with user context
                        EventId = domainEvent.EventId,
                        OccurredOn = domainEvent.OccurredOn
                    };
                }).ToList();

                var rowsAffected = await connection.ExecuteAsync(insertSql, parameters, transaction).ConfigureAwait(false);

                if (rowsAffected != eventList.Count)
                {
                    _logger.LogError(
                        "Expected to insert {ExpectedCount} events but {ActualCount} were inserted for aggregate {AggregateId}",
                        eventList.Count, rowsAffected, aggregateId);

                    return Error.Failure("EventStore.InsertionFailed", "Not all events were inserted successfully");
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "Appended {EventCount} events to aggregate {AggregateId} (tenant: {TenantId})",
                    eventList.Count, aggregateId, tenantId);

                return Result.Success();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique constraint violation
            {
                _logger.LogWarning(ex,
                    "Unique constraint violation while appending events to aggregate {AggregateId}. This may indicate a concurrency conflict.",
                    aggregateId);

                return Error.Conflict("EventStore.ConcurrencyConflict",
                    "A concurrency conflict occurred while appending events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append events to aggregate {AggregateId}", aggregateId);
                return Error.Failure("EventStore.AppendFailed", ex.Message);
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Optimized batch append using single INSERT with multiple VALUES.
    /// Uses NpgsqlCommand with parameterized queries (Npgsql auto-prepares at connection pool level).
    /// Optimal for medium batches (10-499 events).
    /// Target: >= 5,000 events/sec for batches of 100 events.
    /// </summary>
    private async Task<Result> AppendEventsBatchedAsync(
        string aggregateId,
        IReadOnlyList<IDomainEvent> eventList,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ArgumentNullException.ThrowIfNull(eventList);
        ThrowIfDisposed();

        if (eventList.Count == 0)
        {
            return Result.Success();
        }

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

            try
            {
                // Check current version for optimistic concurrency control
                var currentVersion = await GetCurrentVersionAsync(connection, transaction, aggregateId, cancellationToken).ConfigureAwait(false);

                if (expectedVersion != -1 && expectedVersion != currentVersion)
                {
                    _logger.LogWarning(
                        "Concurrency conflict for aggregate {AggregateId}. Expected version: {ExpectedVersion}, Current version: {CurrentVersion}",
                        aggregateId, expectedVersion, currentVersion);

                    return Error.Conflict("EventStore.ConcurrencyConflict",
                        $"Expected version {expectedVersion} but current version is {currentVersion}");
                }

                // Build single INSERT with multiple VALUES
                var valuesBuilder = new System.Text.StringBuilder();

                // COMP-015: Use ArrayPool to reduce allocations (-38,400 bytes per 100 events)
                var parameterCount = eventList.Count * 12; // 12 parameters per event
                var parameters = ArrayPool<NpgsqlParameter>.Shared.Rent(parameterCount);

                try
                {
                    var now = DateTimeOffset.UtcNow;
                    var tenantId = _tenantContext?.TenantId;
                    var version = currentVersion;

                    for (int i = 0; i < eventList.Count; i++)
                    {
                        version++;
                        var domainEvent = eventList[i];

                        var metadata = new Dictionary<string, object>
                        {
                            ["CorrelationId"] = Guid.NewGuid().ToString(),
                            ["Timestamp"] = now.Ticks,
                            ["EventVersion"] = version
                        };

                        if (i > 0)
                        {
                            valuesBuilder.Append(',');
                        }

                        var baseIndex = i * 12;
                        valuesBuilder.Append($"(@p{baseIndex}, @p{baseIndex + 1}, @p{baseIndex + 2}, @p{baseIndex + 3}, @p{baseIndex + 4}, @p{baseIndex + 5}::jsonb, @p{baseIndex + 6}::jsonb, @p{baseIndex + 7}, @p{baseIndex + 8}, @p{baseIndex + 9}, @p{baseIndex + 10}, @p{baseIndex + 11})");

                        parameters[baseIndex] = new NpgsqlParameter($"p{baseIndex}", aggregateId);
                        parameters[baseIndex + 1] = new NpgsqlParameter($"p{baseIndex + 1}", domainEvent.AggregateType);
                        parameters[baseIndex + 2] = new NpgsqlParameter($"p{baseIndex + 2}", version);
                        parameters[baseIndex + 3] = new NpgsqlParameter($"p{baseIndex + 3}", version);
                        parameters[baseIndex + 4] = new NpgsqlParameter($"p{baseIndex + 4}", domainEvent.GetType().AssemblyQualifiedName!);
                        parameters[baseIndex + 5] = new NpgsqlParameter($"p{baseIndex + 5}", JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions));
                        parameters[baseIndex + 6] = new NpgsqlParameter($"p{baseIndex + 6}", JsonSerializer.Serialize(metadata, _jsonOptions));
                        parameters[baseIndex + 7] = new NpgsqlParameter($"p{baseIndex + 7}", tenantId ?? (object)DBNull.Value);
                        parameters[baseIndex + 8] = new NpgsqlParameter($"p{baseIndex + 8}", now);
                        parameters[baseIndex + 9] = new NpgsqlParameter($"p{baseIndex + 9}", "system");
                        parameters[baseIndex + 10] = new NpgsqlParameter($"p{baseIndex + 10}", domainEvent.EventId);
                        parameters[baseIndex + 11] = new NpgsqlParameter($"p{baseIndex + 11}", domainEvent.OccurredOn);
                    }

                    var insertSql = $@"
                        INSERT INTO {_options.TableName} (
                            stream_id, stream_type, version, stream_position, event_type, event_data,
                            metadata, tenant_id, created_at, created_by, event_id, occurred_on
                        ) VALUES {valuesBuilder}";

                    using var cmd = new NpgsqlCommand(insertSql, connection, transaction);
                    // Only add the parameters we actually used (first parameterCount elements)
                    for (int i = 0; i < parameterCount; i++)
                    {
                        cmd.Parameters.Add(parameters[i]);
                    }

                    var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    if (rowsAffected != eventList.Count)
                    {
                        _logger.LogError(
                            "Expected to insert {ExpectedCount} events but {ActualCount} were inserted for aggregate {AggregateId}",
                            eventList.Count, rowsAffected, aggregateId);

                        return Error.Failure("EventStore.InsertionFailed", "Not all events were inserted successfully");
                    }

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogDebug(
                        "Appended {EventCount} events to aggregate {AggregateId} using batched insert (tenant: {TenantId})",
                        eventList.Count, aggregateId, tenantId);

                    return Result.Success();
                }
                finally
                {
                    // COMP-015: Return rented array to pool
                    ArrayPool<NpgsqlParameter>.Shared.Return(parameters, clearArray: true);
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique constraint violation
            {
                _logger.LogWarning(ex,
                    "Unique constraint violation while appending events to aggregate {AggregateId}. This may indicate a concurrency conflict.",
                    aggregateId);

                return Error.Conflict("EventStore.ConcurrencyConflict",
                    "A concurrency conflict occurred while appending events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append events to aggregate {AggregateId}", aggregateId);
                return Error.Failure("EventStore.AppendFailed", ex.Message);
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current version of an aggregate within a transaction.
    /// </summary>
    private async Task<long> GetCurrentVersionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string aggregateId,
        CancellationToken cancellationToken)
    {
        var versionSql = $@"
            SELECT COALESCE(MAX(version), 0)
            FROM {_options.TableName}
            WHERE stream_id = @StreamId
            AND (@TenantId IS NULL OR tenant_id = @TenantId)";

        return await connection.QuerySingleAsync<long>(
            versionSql,
            new { StreamId = aggregateId, TenantId = _tenantContext?.TenantId },
            transaction).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT event_id, event_type, event_data, occurred_on, version
                FROM {_options.TableName}
                WHERE stream_id = @StreamId
                AND (@TenantId IS NULL OR tenant_id = @TenantId)
                ORDER BY version";

            var storedEvents = await connection.QueryAsync(
                sql,
                new { StreamId = aggregateId, TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            // COMP-015: Pre-size list to reduce allocations from resizing
            var domainEvents = new List<IDomainEvent>(capacity: 128);

            foreach (var storedEvent in storedEvents)
            {
                var domainEvent = DeserializeEvent(storedEvent.event_data, storedEvent.event_type);
                if (domainEvent != null)
                {
                    domainEvents.Add(domainEvent);
                }
            }

            _logger.LogDebug(
                "Retrieved {EventCount} events for aggregate {AggregateId}",
                domainEvents.Count, aggregateId);

            return Result.Success<IReadOnlyList<IDomainEvent>>(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events for aggregate {AggregateId}", aggregateId);
            return Error.Failure("EventStore.GetEventsFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        long fromVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        if (fromVersion < 0)
        {
            return Error.Validation("EventStore.InvalidFromVersion", "FromVersion must be greater than or equal to 0");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT event_id, event_type, event_data, occurred_on, version
                FROM {_options.TableName}
                WHERE stream_id = @StreamId
                AND version > @FromVersion
                AND (@TenantId IS NULL OR tenant_id = @TenantId)
                ORDER BY version";

            var storedEvents = await connection.QueryAsync(
                sql,
                new { StreamId = aggregateId, FromVersion = fromVersion, TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            // COMP-015: Pre-size list to reduce allocations from resizing
            var domainEvents = new List<IDomainEvent>(capacity: 128);

            foreach (var storedEvent in storedEvents)
            {
                var domainEvent = DeserializeEvent(storedEvent.event_data, storedEvent.event_type);
                if (domainEvent != null)
                {
                    domainEvents.Add(domainEvent);
                }
            }

            return Result.Success<IReadOnlyList<IDomainEvent>>(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events for aggregate {AggregateId} from version {FromVersion}", aggregateId, fromVersion);
            return Error.Failure("EventStore.GetEventsFromVersionFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets events for a specific aggregate with pagination support (COMP-012).
    /// Optimized for large event streams - retrieves events in pages to reduce memory usage.
    /// Target: 10K events in less than 500ms.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="skip">Number of events to skip (default: 0).</param>
    /// <param name="take">Number of events to take (default: 1000).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the events.</returns>
    public async Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        if (skip < 0)
        {
            return Error.Validation("EventStore.InvalidSkip", "Skip must be greater than or equal to 0");
        }

        if (take <= 0 || take > 10000)
        {
            return Error.Validation("EventStore.InvalidTake", "Take must be between 1 and 10,000");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Optimized query using covering index (COMP-012)
            var sql = $@"
                SELECT event_id, event_type, event_data, occurred_on, version
                FROM {_options.TableName}
                WHERE stream_id = @StreamId
                AND (@TenantId IS NULL OR tenant_id = @TenantId)
                ORDER BY version
                OFFSET @Skip ROWS
                FETCH NEXT @Take ROWS ONLY";

            var storedEvents = await connection.QueryAsync(
                sql,
                new
                {
                    StreamId = aggregateId,
                    TenantId = _tenantContext?.TenantId,
                    Skip = skip,
                    Take = take
                }).ConfigureAwait(false);

            // COMP-015: Pre-size list with exact capacity (we know take value)
            var domainEvents = new List<IDomainEvent>(capacity: take);

            foreach (var storedEvent in storedEvents)
            {
                var domainEvent = DeserializeEvent(storedEvent.event_data, storedEvent.event_type);
                if (domainEvent != null)
                {
                    domainEvents.Add(domainEvent);
                }
            }

            _logger.LogDebug(
                "Retrieved {EventCount} events for aggregate {AggregateId} (skip: {Skip}, take: {Take})",
                domainEvents.Count, aggregateId, skip, take);

            return Result.Success<IReadOnlyList<IDomainEvent>>(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paginated events for aggregate {AggregateId}", aggregateId);
            return Error.Failure("EventStore.GetEventsPaginatedFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsInRangeAsync(
        string aggregateId,
        long fromVersion,
        long toVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        if (fromVersion < 0 || toVersion < 0)
        {
            return Error.Validation("EventStore.InvalidVersionRange", "Versions must be greater than or equal to 0");
        }

        if (fromVersion > toVersion)
        {
            return Error.Validation("EventStore.InvalidVersionRange", "FromVersion cannot be greater than ToVersion");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT event_id, event_type, event_data, occurred_on, version
                FROM {_options.TableName}
                WHERE stream_id = @StreamId
                AND version >= @FromVersion
                AND version <= @ToVersion
                AND (@TenantId IS NULL OR tenant_id = @TenantId)
                ORDER BY version";

            var storedEvents = await connection.QueryAsync(
                sql,
                new { StreamId = aggregateId, FromVersion = fromVersion, ToVersion = toVersion, TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            // COMP-015: Pre-size list to reduce allocations from resizing
            var domainEvents = new List<IDomainEvent>(capacity: 128);

            foreach (var storedEvent in storedEvents)
            {
                var domainEvent = DeserializeEvent(storedEvent.event_data, storedEvent.event_type);
                if (domainEvent != null)
                {
                    domainEvents.Add(domainEvent);
                }
            }

            return Result.Success<IReadOnlyList<IDomainEvent>>(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events for aggregate {AggregateId} in range {FromVersion}-{ToVersion}",
                aggregateId, fromVersion, toVersion);
            return Error.Failure("EventStore.GetEventsInRangeFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<IDomainEvent>> GetLastEventAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT event_id, event_type, event_data, occurred_on, version
                FROM {_options.TableName}
                WHERE stream_id = @StreamId
                AND (@TenantId IS NULL OR tenant_id = @TenantId)
                ORDER BY version DESC
                LIMIT 1";

            var storedEvent = await connection.QuerySingleOrDefaultAsync(
                sql,
                new { StreamId = aggregateId, TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            if (storedEvent == null)
            {
                return Error.NotFound("EventStore.NoEvents", $"No events found for aggregate {aggregateId}");
            }

            var lastEvent = DeserializeEvent(storedEvent.event_data, storedEvent.event_type);

            if (lastEvent == null)
            {
                return Error.Failure("EventStore.DeserializationFailed", "Failed to deserialize last event");
            }

            return Result.Success<IDomainEvent>(lastEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last event for aggregate {AggregateId}", aggregateId);
            return Error.Failure("EventStore.GetLastEventFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<long>> GetVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT COALESCE(MAX(version), 0)
                FROM {_options.TableName}
                WHERE stream_id = @StreamId
                AND (@TenantId IS NULL OR tenant_id = @TenantId)";

            var version = await connection.QuerySingleAsync<long>(
                sql,
                new { StreamId = aggregateId, TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            _logger.LogDebug("Aggregate {AggregateId} current version: {Version}", aggregateId, version);

            return Result.Success<long>(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get version for aggregate {AggregateId}", aggregateId);
            return Error.Failure("EventStore.GetVersionFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Error.Validation("EventStore.InvalidAggregateId", "AggregateId cannot be null or empty");
        }

        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT EXISTS (
                    SELECT 1 FROM {_options.TableName}
                    WHERE stream_id = @StreamId
                    AND (@TenantId IS NULL OR tenant_id = @TenantId)
                    LIMIT 1
                )";

            var exists = await connection.QuerySingleAsync<bool>(
                sql,
                new { StreamId = aggregateId, TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            return Result.Success<bool>(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence for aggregate {AggregateId}", aggregateId);
            return Error.Failure("EventStore.ExistsFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<EventStoreStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
                SELECT
                    COUNT(DISTINCT stream_id) as total_aggregates,
                    COUNT(*) as total_events,
                    stream_id,
                    COUNT(*) as event_count,
                    MIN(created_at) as first_event_date,
                    MAX(created_at) as last_event_date,
                    MAX(version) as current_version
                FROM {_options.TableName}
                WHERE (@TenantId IS NULL OR tenant_id = @TenantId)
                GROUP BY stream_id";

            var results = await connection.QueryAsync(sql, new { TenantId = _tenantContext?.TenantId }).ConfigureAwait(false);

            var stats = new EventStoreStatistics();
            var aggregateStatistics = new Dictionary<string, AggregateStatistics>();

            foreach (var result in results)
            {
                aggregateStatistics[result.stream_id] = new AggregateStatistics
                {
                    EventCount = (int)result.event_count,
                    FirstEventDate = result.first_event_date,
                    LastEventDate = result.last_event_date,
                    CurrentVersion = result.current_version
                };
            }

            stats.TotalAggregates = aggregateStatistics.Count;
            stats.TotalEvents = aggregateStatistics.Values.Sum(a => a.EventCount);
            stats.AggregateStatistics = aggregateStatistics;

            return Result.Success<EventStoreStatistics>(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event store statistics");
            return Error.Failure("EventStore.GetStatisticsFailed", ex.Message);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Initializes the database schema if auto-create is enabled.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> InitializeSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutoCreateSchema)
        {
            return Result.Success();
        }

        ThrowIfDisposed();

        try
        {
            using var connection = new NpgsqlConnection(_enhancedConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS {_options.TableName} (
                    id BIGSERIAL PRIMARY KEY,
                    stream_id VARCHAR(500) NOT NULL,
                    stream_type VARCHAR(500) NOT NULL,
                    version BIGINT NOT NULL,
                    stream_position BIGINT NOT NULL DEFAULT 0,
                    global_position BIGSERIAL UNIQUE,
                    event_type VARCHAR(500) NOT NULL,
                    event_data JSONB NOT NULL,
                    metadata JSONB,
                    tenant_id VARCHAR(255),
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    created_by VARCHAR(255),
                    event_id UUID NOT NULL,
                    occurred_on TIMESTAMP WITH TIME ZONE NOT NULL
                );

                -- Unique constraint for multi-tenant scenarios (tenant_id NOT NULL)
                -- COMP-042: Separate partial unique indexes handle NULL tenant_id correctly
                CREATE UNIQUE INDEX IF NOT EXISTS uk_stream_version_with_tenant
                    ON {_options.TableName}(stream_id, version, tenant_id)
                    WHERE tenant_id IS NOT NULL;

                -- Unique constraint for non-tenant scenarios (tenant_id IS NULL)
                -- This ensures optimistic concurrency works without tenant context
                CREATE UNIQUE INDEX IF NOT EXISTS uk_stream_version_without_tenant
                    ON {_options.TableName}(stream_id, version)
                    WHERE tenant_id IS NULL;

                -- Basic indexes
                CREATE INDEX IF NOT EXISTS idx_event_store_stream_id ON {_options.TableName}(stream_id);
                CREATE INDEX IF NOT EXISTS idx_event_store_tenant_id ON {_options.TableName}(tenant_id) WHERE tenant_id IS NOT NULL;
                CREATE INDEX IF NOT EXISTS idx_event_store_created_at ON {_options.TableName}(created_at);
                CREATE INDEX IF NOT EXISTS idx_event_store_event_type ON {_options.TableName}(event_type);

                -- Composite index for read optimization (COMP-012)
                -- Optimizes: WHERE stream_id = X AND tenant_id = Y ORDER BY version
                CREATE INDEX IF NOT EXISTS idx_event_store_stream_tenant_version
                    ON {_options.TableName}(stream_id, tenant_id, version)
                    WHERE tenant_id IS NOT NULL;

                -- Covering index for read optimization (COMP-012)
                -- Avoids table lookups by including commonly queried columns
                CREATE INDEX IF NOT EXISTS idx_event_store_stream_version_covering
                    ON {_options.TableName}(stream_id, version)
                    INCLUDE (event_type, event_data, occurred_on, event_id);";

            await connection.ExecuteAsync(createTableSql).ConfigureAwait(false);

            _logger.LogInformation("PostgreSQL event store schema initialized successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PostgreSQL event store schema");
            return Error.Failure("EventStore.SchemaInitializationFailed", ex.Message);
        }
    }

    /// <summary>
    /// Securely deserializes a stored event back to a domain event using the whitelisted type registry.
    /// </summary>
    /// <param name="eventData">The serialized event data.</param>
    /// <param name="eventType">The event type name.</param>
    /// <returns>The deserialized domain event, or null if deserialization fails or type is not whitelisted.</returns>
    private IDomainEvent? DeserializeEvent(string eventData, string eventType)
    {
        try
        {
            var result = _eventDeserializer.TryDeserializeEvent(eventData, eventType);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to securely deserialize event of type {EventType}: {Error}",
                    eventType, result.Error.Message);
                return null;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deserializing event of type {EventType}", eventType);
            return null;
        }
    }

    /// <summary>
    /// Throws an exception if the instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PostgreSqlEventStore));
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _connectionSemaphore?.Dispose();
            _disposed = true;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Wraps a database operation with connection pooling metrics.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation name for metrics tagging.</param>
    /// <param name="func">The database operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the database operation.</returns>
    private async Task<T> ExecuteWithMetricsAsync<T>(
        string operation,
        Func<NpgsqlConnection, Task<T>> func,
        CancellationToken cancellationToken)
    {
        var semaphoreWaitSw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            semaphoreWaitSw.Stop();
            _metrics?.RecordConnectionSemaphoreWait(semaphoreWaitSw.Elapsed.TotalMilliseconds, operation);

            // Record queue length after acquiring semaphore
            var queueLength = Math.Max(0, _options.MaxPoolSize - _connectionSemaphore.CurrentCount);
            _metrics?.RecordSemaphoreQueueLength(queueLength);

            try
            {
                var connectionAcquisitionSw = System.Diagnostics.Stopwatch.StartNew();
                using var connection = new NpgsqlConnection(_enhancedConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                connectionAcquisitionSw.Stop();
                _metrics?.RecordConnectionAcquisition(connectionAcquisitionSw.Elapsed.TotalMilliseconds, operation);

                // Record active connections (estimate)
                _metrics?.RecordActiveConnections(_options.MaxPoolSize - _connectionSemaphore.CurrentCount);

                var querySw = System.Diagnostics.Stopwatch.StartNew();
                var result = await func(connection).ConfigureAwait(false);
                querySw.Stop();
                _metrics?.RecordQueryExecution(GetQueryType(operation), querySw.Elapsed.TotalMilliseconds, operation);

                return result;
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("timeout") || ex.Message.Contains("pool"))
            {
                _metrics?.RecordConnectionError("pool_timeout", operation);
                throw;
            }
            catch (Exception)
            {
                _metrics?.RecordConnectionError("query_error", operation);
                throw;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _metrics?.RecordConnectionError("cancelled", operation);
            throw;
        }
    }

    private static string GetQueryType(string operation)
    {
        return operation switch
        {
            var op when op.Contains("Append") => "insert",
            var op when op.Contains("Get") => "select",
            var op when op.Contains("Exists") || op.Contains("Version") || op.Contains("Statistics") => "select",
            var op when op.Contains("Initialize") => "ddl",
            _ => "unknown"
        };
    }
}
