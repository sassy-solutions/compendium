// -----------------------------------------------------------------------
// <copyright file="IIntegrationEvent.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events;

/// <summary>
/// Represents an integration event that is published across bounded contexts.
/// Integration events are used for communication between different bounded contexts or external systems.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier of the integration event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the name of the event type for serialization and routing purposes.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the version of the event schema for backward compatibility.
    /// </summary>
    int EventVersion { get; }

    /// <summary>
    /// Gets the correlation identifier for tracking related events across systems.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the causation identifier linking this event to the command or event that caused it.
    /// </summary>
    string? CausationId { get; }
}
