// -----------------------------------------------------------------------
// <copyright file="PostgreSqlProjectionCheckpointStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Core.Results;
using Compendium.Infrastructure.EventSourcing;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Projections;

/// <summary>
/// PostgreSQL implementation of IProjectionCheckpointStore for per-aggregate projection checkpointing.
/// Stores checkpoint positions with composite key (projection_id, aggregate_id) for independent rebuild tracking.
/// </summary>
public sealed class PostgreSqlProjectionCheckpointStore : IProjectionCheckpointStore
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlProjectionCheckpointStore>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProjectionCheckpointStore"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL configuration options.</param>
    /// <param name="logger">Optional logger instance.</param>
    public PostgreSqlProjectionCheckpointStore(
        IOptions<PostgreSqlOptions> options,
        ILogger<PostgreSqlProjectionCheckpointStore>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = options.Value?.ConnectionString
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database schema for projection checkpoints.
    /// Creates the projection_rebuild_checkpoints table if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            -- Table for storing per-aggregate projection rebuild checkpoints
            CREATE TABLE IF NOT EXISTS projection_rebuild_checkpoints (
                projection_id VARCHAR(255) NOT NULL,
                aggregate_id VARCHAR(255) NOT NULL,
                position BIGINT NOT NULL,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                PRIMARY KEY (projection_id, aggregate_id)
            );

            -- Index for querying checkpoints by projection
            CREATE INDEX IF NOT EXISTS idx_projection_rebuild_checkpoints_projection_id
                ON projection_rebuild_checkpoints(projection_id);

            -- Index for querying checkpoints by aggregate
            CREATE INDEX IF NOT EXISTS idx_projection_rebuild_checkpoints_aggregate_id
                ON projection_rebuild_checkpoints(aggregate_id);
        ";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql);
            _logger?.LogInformation("Projection checkpoint store schema initialized");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize projection checkpoint store schema");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<long>> GetCheckpointAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure<long>(Error.Validation(
                "ProjectionCheckpoint.InvalidProjectionId",
                "Projection ID cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Result.Failure<long>(Error.Validation(
                "ProjectionCheckpoint.InvalidAggregateId",
                "Aggregate ID cannot be null or empty"));
        }

        const string sql = @"
            SELECT position
            FROM projection_rebuild_checkpoints
            WHERE projection_id = @ProjectionId
              AND aggregate_id = @AggregateId;
        ";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var position = await connection.QuerySingleOrDefaultAsync<long?>(
                sql,
                new { ProjectionId = projectionId, AggregateId = aggregateId });

            var checkpointPosition = position ?? 0L;

            _logger?.LogDebug(
                "Retrieved checkpoint for projection {ProjectionId}, aggregate {AggregateId}: position {Position}",
                projectionId, aggregateId, checkpointPosition);

            return Result.Success(checkpointPosition);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to get checkpoint for projection {ProjectionId}, aggregate {AggregateId}",
                projectionId, aggregateId);

            return Result.Failure<long>(Error.Failure(
                "ProjectionCheckpoint.GetFailed",
                $"Failed to get checkpoint: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveCheckpointAsync(
        string projectionId,
        string aggregateId,
        long position,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure(Error.Validation(
                "ProjectionCheckpoint.InvalidProjectionId",
                "Projection ID cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Result.Failure(Error.Validation(
                "ProjectionCheckpoint.InvalidAggregateId",
                "Aggregate ID cannot be null or empty"));
        }

        if (position < 0)
        {
            return Result.Failure(Error.Validation(
                "ProjectionCheckpoint.InvalidPosition",
                "Position cannot be negative"));
        }

        const string sql = @"
            INSERT INTO projection_rebuild_checkpoints
                (projection_id, aggregate_id, position, updated_at)
            VALUES
                (@ProjectionId, @AggregateId, @Position, @UpdatedAt)
            ON CONFLICT (projection_id, aggregate_id)
            DO UPDATE SET
                position = @Position,
                updated_at = @UpdatedAt;
        ";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                sql,
                new
                {
                    ProjectionId = projectionId,
                    AggregateId = aggregateId,
                    Position = position,
                    UpdatedAt = DateTime.UtcNow
                });

            _logger?.LogDebug(
                "Saved checkpoint for projection {ProjectionId}, aggregate {AggregateId} at position {Position}",
                projectionId, aggregateId, position);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to save checkpoint for projection {ProjectionId}, aggregate {AggregateId} at position {Position}",
                projectionId, aggregateId, position);

            return Result.Failure(Error.Failure(
                "ProjectionCheckpoint.SaveFailed",
                $"Failed to save checkpoint: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCheckpointAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure(Error.Validation(
                "ProjectionCheckpoint.InvalidProjectionId",
                "Projection ID cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Result.Failure(Error.Validation(
                "ProjectionCheckpoint.InvalidAggregateId",
                "Aggregate ID cannot be null or empty"));
        }

        const string sql = @"
            DELETE FROM projection_rebuild_checkpoints
            WHERE projection_id = @ProjectionId
              AND aggregate_id = @AggregateId;
        ";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new { ProjectionId = projectionId, AggregateId = aggregateId });

            _logger?.LogInformation(
                "Deleted checkpoint for projection {ProjectionId}, aggregate {AggregateId} ({RowsAffected} rows)",
                projectionId, aggregateId, rowsAffected);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to delete checkpoint for projection {ProjectionId}, aggregate {AggregateId}",
                projectionId, aggregateId);

            return Result.Failure(Error.Failure(
                "ProjectionCheckpoint.DeleteFailed",
                $"Failed to delete checkpoint: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets all checkpoints for a specific projection (useful for monitoring).
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary of aggregate IDs to checkpoint positions.</returns>
    public async Task<Result<Dictionary<string, long>>> GetAllCheckpointsForProjectionAsync(
        string projectionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure<Dictionary<string, long>>(Error.Validation(
                "ProjectionCheckpoint.InvalidProjectionId",
                "Projection ID cannot be null or empty"));
        }

        const string sql = @"
            SELECT aggregate_id, position
            FROM projection_rebuild_checkpoints
            WHERE projection_id = @ProjectionId;
        ";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var checkpoints = await connection.QueryAsync<(string AggregateId, long Position)>(
                sql,
                new { ProjectionId = projectionId });

            var result = checkpoints.ToDictionary(c => c.AggregateId, c => c.Position);

            _logger?.LogDebug(
                "Retrieved {Count} checkpoints for projection {ProjectionId}",
                result.Count, projectionId);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to get all checkpoints for projection {ProjectionId}",
                projectionId);

            return Result.Failure<Dictionary<string, long>>(Error.Failure(
                "ProjectionCheckpoint.GetAllFailed",
                $"Failed to get checkpoints: {ex.Message}"));
        }
    }

    /// <summary>
    /// Deletes all checkpoints for a specific projection (useful for full rebuild).
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Number of checkpoints deleted.</returns>
    public async Task<Result<int>> DeleteAllCheckpointsForProjectionAsync(
        string projectionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure<int>(Error.Validation(
                "ProjectionCheckpoint.InvalidProjectionId",
                "Projection ID cannot be null or empty"));
        }

        const string sql = @"
            DELETE FROM projection_rebuild_checkpoints
            WHERE projection_id = @ProjectionId;
        ";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new { ProjectionId = projectionId });

            _logger?.LogWarning(
                "Deleted {RowsAffected} checkpoints for projection {ProjectionId}",
                rowsAffected, projectionId);

            return Result.Success(rowsAffected);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to delete all checkpoints for projection {ProjectionId}",
                projectionId);

            return Result.Failure<int>(Error.Failure(
                "ProjectionCheckpoint.DeleteAllFailed",
                $"Failed to delete checkpoints: {ex.Message}"));
        }
    }
}
