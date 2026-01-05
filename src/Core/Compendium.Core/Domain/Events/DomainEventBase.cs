// -----------------------------------------------------------------------
// <copyright file="DomainEventBase.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events;

/// <summary>
/// Base class for domain events providing common properties and behavior.
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventBase"/> class.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate that raised the event.</param>
    /// <param name="aggregateType">The type name of the aggregate that raised the event.</param>
    /// <param name="aggregateVersion">The version of the aggregate when the event was raised.</param>
    /// <param name="eventVersion">The version of the event schema (default: 1).</param>
    protected DomainEventBase(string aggregateId, string aggregateType, long aggregateVersion, int eventVersion = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentOutOfRangeException.ThrowIfNegative(aggregateVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventVersion);

        EventId = Guid.NewGuid();
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        AggregateVersion = aggregateVersion;
        EventVersion = eventVersion;
        OccurredOn = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public Guid EventId { get; private init; }

    /// <inheritdoc />
    public string AggregateId { get; private init; }

    /// <inheritdoc />
    public string AggregateType { get; private init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; private init; }

    /// <inheritdoc />
    public long AggregateVersion { get; private init; }

    /// <inheritdoc />
    public int EventVersion { get; private init; }

    /// <summary>
    /// Returns a string representation of the domain event.
    /// </summary>
    /// <returns>A string that represents the current domain event.</returns>
    public override string ToString()
    {
        return $"{GetType().Name} [EventId={EventId}, AggregateId={AggregateId}, AggregateType={AggregateType}, Version={AggregateVersion}, EventVersion={EventVersion}, OccurredOn={OccurredOn:O}]";
    }
}
