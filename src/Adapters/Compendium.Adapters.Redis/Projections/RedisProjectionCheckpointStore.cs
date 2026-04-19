// -----------------------------------------------------------------------
// <copyright file="RedisProjectionCheckpointStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using Compendium.Core.Results;
using Compendium.Infrastructure.EventSourcing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.Projections;

/// <summary>
/// Redis implementation of IProjectionCheckpointStore for high-performance projection checkpointing.
/// Uses Redis hash sets to store checkpoint positions with automatic expiration support.
/// </summary>
public sealed class RedisProjectionCheckpointStore : IProjectionCheckpointStore, IAsyncDisposable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisProjectionCheckpointStore>? _logger;
    private readonly TimeSpan _defaultExpiration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisProjectionCheckpointStore"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
    /// <param name="options">Redis configuration options.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="defaultExpiration">Default expiration time for checkpoints (default: 7 days).</param>
    public RedisProjectionCheckpointStore(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisOptions> options,
        ILogger<RedisProjectionCheckpointStore>? logger = null,
        TimeSpan? defaultExpiration = null)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _database = _connectionMultiplexer.GetDatabase(_options.DefaultDatabase);
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromDays(7);
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

        ThrowIfDisposed();

        var redisKey = GetRedisKey(projectionId);
        var hashField = GetHashField(aggregateId);

        try
        {
            var value = await _database.HashGetAsync(redisKey, hashField).ConfigureAwait(false);

            if (!value.HasValue)
            {
                _logger?.LogDebug(
                    "No checkpoint found for projection {ProjectionId}, aggregate {AggregateId}",
                    projectionId, aggregateId);
                return Result.Success(0L);
            }

            var position = (long)value;

            _logger?.LogDebug(
                "Retrieved checkpoint for projection {ProjectionId}, aggregate {AggregateId}: position {Position}",
                projectionId, aggregateId, position);

            return Result.Success(position);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to get checkpoint for projection {ProjectionId}, aggregate {AggregateId}",
                projectionId, aggregateId);

            // Return 0 on failure to allow rebuild to start from beginning
            return Result.Success(0L);
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

        ThrowIfDisposed();

        var redisKey = GetRedisKey(projectionId);
        var hashField = GetHashField(aggregateId);

        try
        {
            // Save checkpoint position in hash
            var success = await _database.HashSetAsync(redisKey, hashField, position).ConfigureAwait(false);

            if (!success)
            {
                _logger?.LogWarning(
                    "Failed to save checkpoint for projection {ProjectionId}, aggregate {AggregateId}",
                    projectionId, aggregateId);

                return Result.Failure(Error.Failure(
                    "ProjectionCheckpoint.SaveFailed",
                    "Failed to save checkpoint to Redis"));
            }

            // Set expiration on the hash key to prevent stale checkpoints
            await _database.KeyExpireAsync(redisKey, _defaultExpiration).ConfigureAwait(false);

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

        ThrowIfDisposed();

        var redisKey = GetRedisKey(projectionId);
        var hashField = GetHashField(aggregateId);

        try
        {
            var success = await _database.HashDeleteAsync(redisKey, hashField).ConfigureAwait(false);

            _logger?.LogInformation(
                "Deleted checkpoint for projection {ProjectionId}, aggregate {AggregateId} (success: {Success})",
                projectionId, aggregateId, success);

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

        ThrowIfDisposed();

        var redisKey = GetRedisKey(projectionId);

        try
        {
            var entries = await _database.HashGetAllAsync(redisKey).ConfigureAwait(false);

            var result = entries.ToDictionary(
                entry => entry.Name.ToString(),
                entry => (long)entry.Value);

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
    /// <returns>True if key was deleted, false if key did not exist.</returns>
    public async Task<Result<bool>> DeleteAllCheckpointsForProjectionAsync(
        string projectionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure<bool>(Error.Validation(
                "ProjectionCheckpoint.InvalidProjectionId",
                "Projection ID cannot be null or empty"));
        }

        ThrowIfDisposed();

        var redisKey = GetRedisKey(projectionId);

        try
        {
            var success = await _database.KeyDeleteAsync(redisKey).ConfigureAwait(false);

            _logger?.LogWarning(
                "Deleted all checkpoints for projection {ProjectionId} (success: {Success})",
                projectionId, success);

            return Result.Success(success);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to delete all checkpoints for projection {ProjectionId}",
                projectionId);

            return Result.Failure<bool>(Error.Failure(
                "ProjectionCheckpoint.DeleteAllFailed",
                $"Failed to delete checkpoints: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates a Redis key for a projection's checkpoints.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <returns>The Redis key.</returns>
    private string GetRedisKey(string projectionId)
    {
        var baseKey = $"projection:checkpoint:{projectionId}";
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? baseKey
            : $"{_options.KeyPrefix}:{baseKey}";
    }

    /// <summary>
    /// Creates a hash field name for an aggregate's checkpoint.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <returns>The hash field name.</returns>
    private static string GetHashField(string aggregateId)
    {
        return aggregateId;
    }

    /// <summary>
    /// Throws an exception if the instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RedisProjectionCheckpointStore));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger?.LogDebug("Redis projection checkpoint store disposed");
        }

        return ValueTask.CompletedTask;
    }
}
