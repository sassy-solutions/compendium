// -----------------------------------------------------------------------
// <copyright file="PostgreSqlProjectionStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Infrastructure.Projections;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Projections;

/// <summary>
/// PostgreSQL implementation of IProjectionStore with optimized checkpoint and snapshot management.
/// </summary>
public class PostgreSqlProjectionStore : IProjectionStore
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlProjectionStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProjectionStore"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public PostgreSqlProjectionStore(
        IOptions<PostgreSqlOptions> options,
        ILogger<PostgreSqlProjectionStore> logger)
    {
        _connectionString = options?.Value?.ConnectionString
            ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Initializes the database schema for projection storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            -- Table for storing projection checkpoints
            CREATE TABLE IF NOT EXISTS projection_checkpoints (
                projection_name VARCHAR(255) PRIMARY KEY,
                position BIGINT NOT NULL,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            );

            -- Table for storing projection snapshots
            CREATE TABLE IF NOT EXISTS projection_snapshots (
                id SERIAL PRIMARY KEY,
                projection_name VARCHAR(255) NOT NULL,
                version INT NOT NULL,
                snapshot_data JSONB NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                UNIQUE(projection_name, version)
            );

            -- Table for storing projection states
            CREATE TABLE IF NOT EXISTS projection_states (
                projection_name VARCHAR(255) PRIMARY KEY,
                version INT NOT NULL DEFAULT 1,
                last_processed_position BIGINT DEFAULT 0,
                last_processed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                status VARCHAR(50) NOT NULL DEFAULT 'Idle',
                error_message TEXT
            );

            -- Indexes for better performance
            CREATE INDEX IF NOT EXISTS idx_projection_snapshots_name 
                ON projection_snapshots(projection_name);
            CREATE INDEX IF NOT EXISTS idx_projection_snapshots_name_version 
                ON projection_snapshots(projection_name, version DESC);
            CREATE INDEX IF NOT EXISTS idx_projection_states_status 
                ON projection_states(status);
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql);

        _logger.LogInformation("Projection store tables initialized");
    }

    /// <inheritdoc />
    public async Task SaveCheckpointAsync(string projectionName, long position, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO projection_checkpoints (projection_name, position, updated_at)
            VALUES (@ProjectionName, @Position, @UpdatedAt)
            ON CONFLICT (projection_name) 
            DO UPDATE SET 
                position = @Position,
                updated_at = @UpdatedAt;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            ProjectionName = projectionName,
            Position = position,
            UpdatedAt = DateTime.UtcNow
        });

        _logger.LogDebug("Saved checkpoint for {ProjectionName} at position {Position}",
            projectionName, position);
    }

    /// <inheritdoc />
    public async Task<long?> GetCheckpointAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT position 
            FROM projection_checkpoints 
            WHERE projection_name = @ProjectionName;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        var position = await connection.QuerySingleOrDefaultAsync<long?>(sql, new
        {
            ProjectionName = projectionName
        });

        _logger.LogDebug("Retrieved checkpoint for {ProjectionName}: {Position}",
            projectionName, position);

        return position;
    }

    /// <inheritdoc />
    public async Task SaveSnapshotAsync<TProjection>(TProjection projection, CancellationToken cancellationToken = default)
        where TProjection : IProjection
    {
        const string sql = @"
            INSERT INTO projection_snapshots (projection_name, version, snapshot_data, created_at)
            VALUES (@ProjectionName, @Version, @SnapshotData::jsonb, @CreatedAt)
            ON CONFLICT (projection_name, version)
            DO UPDATE SET
                snapshot_data = @SnapshotData::jsonb,
                created_at = @CreatedAt;
        ";

        var snapshotData = JsonSerializer.Serialize(projection, _jsonOptions);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            ProjectionName = projection.ProjectionName,
            Version = projection.Version,
            SnapshotData = snapshotData,
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Saved snapshot for {ProjectionName} v{Version}",
            projection.ProjectionName, projection.Version);
    }

    /// <inheritdoc />
    public async Task<TProjection?> LoadSnapshotAsync<TProjection>(
        string projectionName,
        CancellationToken cancellationToken = default) where TProjection : IProjection
    {
        const string sql = @"
            SELECT snapshot_data 
            FROM projection_snapshots 
            WHERE projection_name = @ProjectionName
            ORDER BY version DESC, created_at DESC
            LIMIT 1;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        var snapshotData = await connection.QuerySingleOrDefaultAsync<string>(sql, new
        {
            ProjectionName = projectionName
        });

        if (string.IsNullOrEmpty(snapshotData))
        {
            _logger.LogDebug("No snapshot found for {ProjectionName}", projectionName);
            return default;
        }

        try
        {
            var projection = JsonSerializer.Deserialize<TProjection>(snapshotData, _jsonOptions);
            _logger.LogInformation("Loaded snapshot for {ProjectionName}", projectionName);
            return projection;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize snapshot for {ProjectionName}", projectionName);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task DeleteProjectionDataAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM projection_checkpoints WHERE projection_name = @ProjectionName;
            DELETE FROM projection_snapshots WHERE projection_name = @ProjectionName;
            DELETE FROM projection_states WHERE projection_name = @ProjectionName;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { ProjectionName = projectionName });

        _logger.LogWarning("Deleted all data for projection {ProjectionName}", projectionName);
    }

    /// <inheritdoc />
    public async Task<ProjectionState?> GetProjectionStateAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                projection_name as ProjectionName,
                version as Version,
                last_processed_position as LastProcessedPosition,
                last_processed_at as LastProcessedAt,
                status as Status,
                error_message as ErrorMessage
            FROM projection_states 
            WHERE projection_name = @ProjectionName;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        var state = await connection.QuerySingleOrDefaultAsync<ProjectionState>(sql, new
        {
            ProjectionName = projectionName
        });

        return state;
    }

    /// <inheritdoc />
    public async Task SaveProjectionStateAsync(ProjectionState state, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO projection_states (
                projection_name, 
                version, 
                last_processed_position, 
                last_processed_at, 
                status, 
                error_message
            )
            VALUES (
                @ProjectionName, 
                @Version, 
                @LastProcessedPosition, 
                @LastProcessedAt, 
                @Status, 
                @ErrorMessage
            )
            ON CONFLICT (projection_name) 
            DO UPDATE SET 
                version = @Version,
                last_processed_position = @LastProcessedPosition,
                last_processed_at = @LastProcessedAt,
                status = @Status,
                error_message = @ErrorMessage;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            ProjectionName = state.ProjectionName,
            Version = state.Version,
            LastProcessedPosition = state.LastProcessedPosition,
            LastProcessedAt = state.LastProcessedAt,
            Status = state.Status.ToString(),
            ErrorMessage = state.ErrorMessage
        });

        _logger.LogDebug("Saved state for projection {ProjectionName}: {Status}",
            state.ProjectionName, state.Status);
    }

    /// <summary>
    /// Gets projection statistics for monitoring and debugging.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Dictionary of projection statistics.</returns>
    public async Task<Dictionary<string, object>> GetProjectionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                COUNT(*) as total_projections,
                COUNT(CASE WHEN status = 'Building' THEN 1 END) as building_projections,
                COUNT(CASE WHEN status = 'Rebuilding' THEN 1 END) as rebuilding_projections,
                COUNT(CASE WHEN status = 'Failed' THEN 1 END) as failed_projections,
                COUNT(CASE WHEN status = 'Paused' THEN 1 END) as paused_projections
            FROM projection_states;
            
            SELECT 
                projection_name,
                COUNT(*) as snapshot_count,
                MAX(created_at) as latest_snapshot
            FROM projection_snapshots
            GROUP BY projection_name;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        using var multi = await connection.QueryMultipleAsync(sql);

        var stats = await multi.ReadSingleAsync();
        var snapshotStats = await multi.ReadAsync();

        return new Dictionary<string, object>
        {
            ["TotalProjections"] = stats.total_projections,
            ["BuildingProjections"] = stats.building_projections,
            ["RebuildingProjections"] = stats.rebuilding_projections,
            ["FailedProjections"] = stats.failed_projections,
            ["PausedProjections"] = stats.paused_projections,
            ["SnapshotStatistics"] = snapshotStats.ToList()
        };
    }

    /// <summary>
    /// Performs cleanup of old snapshots to prevent storage bloat.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain snapshots.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Number of snapshots deleted.</returns>
    public async Task<int> CleanupOldSnapshotsAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM projection_snapshots 
            WHERE created_at < @CutoffDate
            AND (projection_name, version) NOT IN (
                SELECT projection_name, MAX(version)
                FROM projection_snapshots
                GROUP BY projection_name
            );
        ";

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        using var connection = new NpgsqlConnection(_connectionString);
        var deletedCount = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate });

        _logger.LogInformation("Cleaned up {DeletedCount} old snapshots older than {CutoffDate}",
            deletedCount, cutoffDate);

        return deletedCount;
    }
}
