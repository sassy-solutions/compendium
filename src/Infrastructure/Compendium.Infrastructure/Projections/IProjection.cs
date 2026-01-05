// -----------------------------------------------------------------------
// <copyright file="IProjection.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Base interface for all projections that handle domain events.
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Gets the unique name identifying this projection.
    /// </summary>
    string ProjectionName { get; }

    /// <summary>
    /// Gets the version of this projection schema.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Resets the projection to its initial state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed projection interface for handling specific event types.
/// </summary>
/// <typeparam name="TEvent">The type of domain event this projection handles.</typeparam>
public interface IProjection<in TEvent> : IProjection
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Applies a domain event to update the projection state.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    /// <param name="metadata">The event metadata containing position and timing information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ApplyAsync(TEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default);
}
