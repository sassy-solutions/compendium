// -----------------------------------------------------------------------
// <copyright file="InMemoryProjectionCheckpointStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Core.Results;
using Compendium.Infrastructure.EventSourcing;

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// In-memory implementation of <see cref="IProjectionCheckpointStore"/> for testing
/// and InMemory-backed framework E2E scenarios. Thread-safe via <see cref="ConcurrentDictionary{TKey, TValue}"/>;
/// data lives for the lifetime of the instance (no persistence).
/// </summary>
/// <remarks>
/// Semantic contract: matches <c>PostgreSqlProjectionCheckpointStore</c> and
/// <c>RedisProjectionCheckpointStore</c>. Returns <c>0</c> when no checkpoint
/// exists for the (projection, aggregate) pair (treated as "start from
/// position 0").
/// </remarks>
public sealed class InMemoryProjectionCheckpointStore : IProjectionCheckpointStore
{
    private readonly ConcurrentDictionary<(string ProjectionId, string AggregateId), long> _checkpoints = new();

    /// <inheritdoc />
    public Task<Result<long>> GetCheckpointAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        var position = _checkpoints.TryGetValue((projectionId, aggregateId), out var value) ? value : 0L;
        return Task.FromResult(Result.Success(position));
    }

    /// <inheritdoc />
    public Task<Result> SaveCheckpointAsync(
        string projectionId,
        string aggregateId,
        long position,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        _checkpoints[(projectionId, aggregateId)] = position;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> DeleteCheckpointAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        _checkpoints.TryRemove((projectionId, aggregateId), out _);
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Clears all checkpoints. Test-only helper.
    /// </summary>
    public void Clear() => _checkpoints.Clear();
}
