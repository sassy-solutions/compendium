// -----------------------------------------------------------------------
// <copyright file="IProjectionStore.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Interface for projection storage operations.
/// Handles checkpoint persistence and snapshot storage for efficient projection rebuilds.
/// </summary>
public interface IProjectionStore
{
    /// <summary>
    /// Saves a checkpoint for a projection at a specific position.
    /// </summary>
    /// <param name="projectionName">The name of the projection.</param>
    /// <param name="position">The global position that was last processed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveCheckpointAsync(string projectionName, long position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last saved checkpoint for a projection.
    /// </summary>
    /// <param name="projectionName">The name of the projection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The last processed position, or null if no checkpoint exists.</returns>
    Task<long?> GetCheckpointAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a snapshot of the projection state.
    /// </summary>
    /// <typeparam name="TProjection">The type of the projection.</typeparam>
    /// <param name="projection">The projection instance to snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveSnapshotAsync<TProjection>(TProjection projection, CancellationToken cancellationToken = default)
        where TProjection : IProjection;

    /// <summary>
    /// Loads the latest snapshot for a projection.
    /// </summary>
    /// <typeparam name="TProjection">The type of the projection.</typeparam>
    /// <param name="projectionName">The name of the projection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded projection snapshot, or null if no snapshot exists.</returns>
    Task<TProjection?> LoadSnapshotAsync<TProjection>(string projectionName, CancellationToken cancellationToken = default)
        where TProjection : IProjection;

    /// <summary>
    /// Deletes all data associated with a projection.
    /// </summary>
    /// <param name="projectionName">The name of the projection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteProjectionDataAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a projection.
    /// </summary>
    /// <param name="projectionName">The name of the projection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projection state.</returns>
    Task<ProjectionState?> GetProjectionStateAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the state of a projection.
    /// </summary>
    /// <param name="state">The projection state to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveProjectionStateAsync(ProjectionState state, CancellationToken cancellationToken = default);
}
