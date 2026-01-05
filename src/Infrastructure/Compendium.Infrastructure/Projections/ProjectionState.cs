// -----------------------------------------------------------------------
// <copyright file="ProjectionState.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Represents the current state and metadata of a projection.
/// </summary>
public class ProjectionState
{
    /// <summary>
    /// Gets or sets the name of the projection.
    /// </summary>
    public string ProjectionName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the projection schema.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Gets or sets the last processed global position.
    /// </summary>
    public long LastProcessedPosition { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the projection was last processed.
    /// </summary>
    public DateTime LastProcessedAt { get; init; }

    /// <summary>
    /// Gets or sets the current status of the projection.
    /// </summary>
    public ProjectionStatus Status { get; init; }

    /// <summary>
    /// Gets or sets an error message if the projection is in a failed state.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Enumeration of possible projection statuses.
/// </summary>
public enum ProjectionStatus
{
    /// <summary>The projection is idle and ready to process events.</summary>
    Idle,

    /// <summary>The projection is actively building from new events.</summary>
    Building,

    /// <summary>The projection is rebuilding from scratch.</summary>
    Rebuilding,

    /// <summary>The projection is paused and not processing events.</summary>
    Paused,

    /// <summary>The projection has failed and needs attention.</summary>
    Failed,

    /// <summary>The projection has completed its rebuild operation.</summary>
    Completed
}
