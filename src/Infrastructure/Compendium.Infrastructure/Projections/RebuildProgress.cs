// -----------------------------------------------------------------------
// <copyright file="RebuildProgress.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Progress information for projection rebuild operations.
/// Provides real-time feedback on the rebuild process including performance metrics.
/// </summary>
public class RebuildProgress
{
    /// <summary>
    /// Gets or sets the name of the projection being rebuilt.
    /// </summary>
    public string ProjectionName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of events to process.
    /// </summary>
    public long TotalEvents { get; init; }

    /// <summary>
    /// Gets or sets the number of events processed so far.
    /// </summary>
    public long ProcessedEvents { get; init; }

    /// <summary>
    /// Gets the percentage of completion (0-100).
    /// </summary>
    public double PercentComplete => TotalEvents > 0 ? (ProcessedEvents * 100.0) / TotalEvents : 0;

    /// <summary>
    /// Gets or sets the elapsed time since the rebuild started.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets or sets the current processing rate in events per second.
    /// </summary>
    public double EventsPerSecond { get; init; }

    /// <summary>
    /// Gets or sets the estimated time remaining for completion.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets or sets the current batch number being processed.
    /// </summary>
    public int CurrentBatch { get; init; }

    /// <summary>
    /// Gets or sets the size of each processing batch.
    /// </summary>
    public int BatchSize { get; init; }
}
