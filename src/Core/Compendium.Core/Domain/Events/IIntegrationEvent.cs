// -----------------------------------------------------------------------
// <copyright file="IIntegrationEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
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
