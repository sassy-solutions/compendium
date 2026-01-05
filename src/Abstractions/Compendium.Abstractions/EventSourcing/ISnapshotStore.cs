// -----------------------------------------------------------------------
// <copyright file="ISnapshotStore.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.EventSourcing;

/// <summary>
/// Optional snapshot store for performance optimization.
/// Services work perfectly without snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Gets the latest snapshot for an aggregate.
    /// </summary>
    /// <typeparam name="T">The type of the snapshot data.</typeparam>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the snapshot result.</returns>
    Task<Result<Snapshot<T>>> GetLatestSnapshotAsync<T>(
        string aggregateId,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Saves a snapshot for an aggregate.
    /// </summary>
    /// <typeparam name="T">The type of the snapshot data.</typeparam>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="snapshot">The snapshot data.</param>
    /// <param name="version">The aggregate version at the time of snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> SaveSnapshotAsync<T>(
        string aggregateId,
        T snapshot,
        long version,
        CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Represents a snapshot of an aggregate at a specific version.
/// </summary>
/// <typeparam name="T">The type of the snapshot data.</typeparam>
/// <param name="State">The aggregate state at the time of snapshot.</param>
/// <param name="Version">The aggregate version when the snapshot was taken.</param>
/// <param name="CreatedAt">When the snapshot was created.</param>
/// <param name="TenantId">The tenant identifier (for multi-tenant scenarios).</param>
public record Snapshot<T>(
    T State,
    long Version,
    DateTimeOffset CreatedAt,
    string? TenantId = null
) where T : class;

/// <summary>
/// Strategy for determining when to take snapshots.
/// </summary>
public interface ISnapshotStrategy
{
    /// <summary>
    /// Determines if a snapshot should be taken based on the current state.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="version">The current aggregate version.</param>
    /// <param name="eventCount">The total number of events for this aggregate.</param>
    /// <returns>True if a snapshot should be taken; otherwise, false.</returns>
    bool ShouldTakeSnapshot(string aggregateId, long version, int eventCount);
}
