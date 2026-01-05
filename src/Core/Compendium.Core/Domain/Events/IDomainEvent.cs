// -----------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events;

/// <summary>
/// Represents a domain event that occurred within the domain.
/// Domain events are used to communicate changes within the same bounded context.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the identifier of the aggregate that raised the event.
    /// </summary>
    string AggregateId { get; }

    /// <summary>
    /// Gets the type name of the aggregate that raised the event.
    /// </summary>
    string AggregateType { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the version of the aggregate when the event was raised.
    /// </summary>
    long AggregateVersion { get; }

    /// <summary>
    /// Gets the version of the event schema.
    /// This allows for event schema evolution while maintaining backward compatibility.
    /// Default value is 1 for events created before versioning was introduced.
    /// </summary>
    int EventVersion { get; }
}
