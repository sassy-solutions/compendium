// -----------------------------------------------------------------------
// <copyright file="IProjectionManager.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Interface for managing projections with advanced rebuild and progress tracking capabilities.
/// </summary>
public interface IProjectionManager
{
    /// <summary>
    /// Rebuilds a projection from a specific point in time or from the beginning.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection to rebuild.</typeparam>
    /// <param name="streamId">Optional stream identifier to rebuild from specific stream.</param>
    /// <param name="fromTimestamp">Optional timestamp to start rebuild from.</param>
    /// <param name="progress">Optional progress reporter for real-time feedback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous rebuild operation.</returns>
    Task RebuildProjectionAsync<TProjection>(
        string? streamId = null,
        DateTime? fromTimestamp = null,
        IProgress<RebuildProgress>? progress = null,
        CancellationToken cancellationToken = default) where TProjection : IProjection, new();

    /// <summary>
    /// Gets the current state of a projection.
    /// </summary>
    /// <param name="projectionName">The name of the projection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current projection state.</returns>
    Task<ProjectionState> GetProjectionStateAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a projection, stopping it from processing new events.
    /// </summary>
    /// <param name="projectionName">The name of the projection to pause.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PauseProjectionAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused projection.
    /// </summary>
    /// <param name="projectionName">The name of the projection to resume.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResumeProjectionAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a projection and all its associated data.
    /// </summary>
    /// <param name="projectionName">The name of the projection to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteProjectionAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a projection for live processing.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection to register.</typeparam>
    void RegisterProjection<TProjection>() where TProjection : IProjection, new();

    /// <summary>
    /// Gets statistics about all registered projections.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Projection manager statistics.</returns>
    Task<ProjectionManagerStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about the projection manager and all its projections.
/// </summary>
public class ProjectionManagerStatistics
{
    /// <summary>
    /// Gets or sets the total number of registered projections.
    /// </summary>
    public int TotalProjections { get; init; }

    /// <summary>
    /// Gets or sets the number of active projections.
    /// </summary>
    public int ActiveProjections { get; init; }

    /// <summary>
    /// Gets or sets the number of rebuilding projections.
    /// </summary>
    public int RebuildingProjections { get; init; }

    /// <summary>
    /// Gets or sets the number of paused projections.
    /// </summary>
    public int PausedProjections { get; init; }

    /// <summary>
    /// Gets or sets the number of failed projections.
    /// </summary>
    public int FailedProjections { get; init; }

    /// <summary>
    /// Gets or sets details for each projection.
    /// </summary>
    public Dictionary<string, ProjectionState> ProjectionDetails { get; init; } = new();
}
