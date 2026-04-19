// -----------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
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
