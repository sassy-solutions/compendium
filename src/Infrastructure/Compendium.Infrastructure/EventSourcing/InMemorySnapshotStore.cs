// -----------------------------------------------------------------------
// <copyright file="InMemorySnapshotStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text.Json;
using Compendium.Multitenancy;

namespace Compendium.Infrastructure.EventSourcing;

/// <summary>
/// In-memory implementation of ISnapshotStore for testing and development purposes.
/// Provides thread-safe snapshot storage with multi-tenancy support.
/// </summary>
public sealed class InMemorySnapshotStore : ISnapshotStore, IDisposable
{
    private readonly ConcurrentDictionary<string, SnapshotEntry> _snapshots = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly ILogger<InMemorySnapshotStore>? _logger;
    private readonly ITenantContext? _tenantContext;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySnapshotStore"/> class.
    /// </summary>
    /// <param name="logger">The logger instance (optional for testing).</param>
    /// <param name="tenantContext">The tenant context for multi-tenancy support (optional for testing).</param>
    public InMemorySnapshotStore(ILogger<InMemorySnapshotStore>? logger = null, ITenantContext? tenantContext = null)
    {
        _logger = logger;
        _tenantContext = tenantContext;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public Task<Result<Snapshot<T>>> GetLatestSnapshotAsync<T>(
        string aggregateId,
        CancellationToken cancellationToken = default) where T : class
    {
        if (_disposed)
        {
            return Task.FromResult(Result.Failure<Snapshot<T>>(
                Error.Failure("SnapshotStore.Disposed", "InMemorySnapshotStore has been disposed")));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

            var tenantedAggregateId = GetTenantedAggregateId(aggregateId);

            _lock.EnterReadLock();
            try
            {
                if (!_snapshots.TryGetValue(tenantedAggregateId, out var entry))
                {
                    _logger?.LogDebug("No snapshot found for aggregate {AggregateId}", aggregateId);
                    return Task.FromResult(Result.Failure<Snapshot<T>>(
                        Error.NotFound("SnapshotStore.NotFound", $"No snapshot found for aggregate {aggregateId}")));
                }

                // Deserialize the snapshot data
                var snapshotData = JsonSerializer.Deserialize<T>(entry.SerializedData, _jsonOptions);
                if (snapshotData == null)
                {
                    _logger?.LogWarning("Failed to deserialize snapshot for aggregate {AggregateId}", aggregateId);
                    return Task.FromResult(Result.Failure<Snapshot<T>>(
                        Error.Failure("SnapshotStore.DeserializationFailed", $"Failed to deserialize snapshot for aggregate {aggregateId}")));
                }

                var snapshot = new Snapshot<T>(snapshotData, entry.Version, entry.CreatedAt, entry.TenantId);

                _logger?.LogDebug("Retrieved snapshot for aggregate {AggregateId} at version {Version}",
                    aggregateId, entry.Version);

                return Task.FromResult(Result.Success(snapshot));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Get snapshot operation cancelled for aggregate {AggregateId}", aggregateId);
            return Task.FromResult(Result.Failure<Snapshot<T>>(
                Error.Failure("SnapshotStore.OperationCancelled", "Get snapshot operation was cancelled")));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get snapshot for aggregate {AggregateId}", aggregateId);
            return Task.FromResult(Result.Failure<Snapshot<T>>(
                Error.Failure("SnapshotStore.UnexpectedError", $"Failed to get snapshot for aggregate {aggregateId}: {ex.Message}")));
        }
    }

    /// <inheritdoc />
    public Task<Result> SaveSnapshotAsync<T>(
        string aggregateId,
        T snapshot,
        long version,
        CancellationToken cancellationToken = default) where T : class
    {
        if (_disposed)
        {
            return Task.FromResult(Result.Failure(
                Error.Failure("SnapshotStore.Disposed", "InMemorySnapshotStore has been disposed")));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
            ArgumentNullException.ThrowIfNull(snapshot);

            if (version < 0)
            {
                return Task.FromResult(Result.Failure(
                    Error.Validation("SnapshotStore.InvalidVersion", "Version must be non-negative")));
            }

            var tenantedAggregateId = GetTenantedAggregateId(aggregateId);
            var currentTenantId = _tenantContext?.TenantId;

            // Serialize the snapshot data
            var serializedData = JsonSerializer.Serialize(snapshot, _jsonOptions);

            var entry = new SnapshotEntry
            {
                AggregateId = aggregateId,
                Version = version,
                SerializedData = serializedData,
                CreatedAt = DateTimeOffset.UtcNow,
                TenantId = currentTenantId
            };

            _lock.EnterWriteLock();
            try
            {
                // Check if we should update (only if version is newer)
                if (_snapshots.TryGetValue(tenantedAggregateId, out var existingEntry))
                {
                    if (existingEntry.Version >= version)
                    {
                        _logger?.LogDebug("Skipping snapshot save for aggregate {AggregateId} - existing version {ExistingVersion} is newer than or equal to {Version}",
                            aggregateId, existingEntry.Version, version);
                        return Task.FromResult(Result.Success());
                    }
                }

                _snapshots[tenantedAggregateId] = entry;

                _logger?.LogDebug("Saved snapshot for aggregate {AggregateId} at version {Version}",
                    aggregateId, version);

                return Task.FromResult(Result.Success());
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Save snapshot operation cancelled for aggregate {AggregateId}", aggregateId);
            return Task.FromResult(Result.Failure(
                Error.Failure("SnapshotStore.OperationCancelled", "Save snapshot operation was cancelled")));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save snapshot for aggregate {AggregateId}", aggregateId);
            return Task.FromResult(Result.Failure(
                Error.Failure("SnapshotStore.UnexpectedError", $"Failed to save snapshot for aggregate {aggregateId}: {ex.Message}")));
        }
    }

    /// <summary>
    /// Gets statistics about the snapshot store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Snapshot store statistics.</returns>
    public Task<Result<SnapshotStoreStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return Task.FromResult(Result.Failure<SnapshotStoreStatistics>(
                Error.Failure("SnapshotStore.Disposed", "InMemorySnapshotStore has been disposed")));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _lock.EnterReadLock();
            try
            {
                var statistics = new SnapshotStoreStatistics
                {
                    TotalSnapshots = _snapshots.Count,
                    OldestSnapshot = _snapshots.Values.MinBy(s => s.CreatedAt)?.CreatedAt,
                    NewestSnapshot = _snapshots.Values.MaxBy(s => s.CreatedAt)?.CreatedAt
                };

                return Task.FromResult(Result.Success(statistics));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(Result.Failure<SnapshotStoreStatistics>(
                Error.Failure("SnapshotStore.OperationCancelled", "Get statistics operation was cancelled")));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get snapshot store statistics");
            return Task.FromResult(Result.Failure<SnapshotStoreStatistics>(
                Error.Failure("SnapshotStore.UnexpectedError", $"Failed to get snapshot store statistics: {ex.Message}")));
        }
    }

    /// <summary>
    /// Gets the tenanted aggregate identifier.
    /// </summary>
    /// <param name="aggregateId">The base aggregate identifier.</param>
    /// <returns>The tenanted aggregate identifier.</returns>
    private string GetTenantedAggregateId(string aggregateId)
    {
        var tenantId = _tenantContext?.TenantId;
        return string.IsNullOrEmpty(tenantId) ? aggregateId : $"{tenantId}:{aggregateId}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _lock.Dispose();
        _snapshots.Clear();
        _disposed = true;
    }

    /// <summary>
    /// Internal snapshot entry for storage.
    /// </summary>
    private sealed class SnapshotEntry
    {
        public required string AggregateId { get; init; }
        public required long Version { get; init; }
        public required string SerializedData { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public string? TenantId { get; init; }
    }
}

/// <summary>
/// Statistics about the snapshot store.
/// </summary>
public sealed class SnapshotStoreStatistics
{
    /// <summary>
    /// Gets or sets the total number of snapshots.
    /// </summary>
    public int TotalSnapshots { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the oldest snapshot.
    /// </summary>
    public DateTimeOffset? OldestSnapshot { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the newest snapshot.
    /// </summary>
    public DateTimeOffset? NewestSnapshot { get; set; }
}
