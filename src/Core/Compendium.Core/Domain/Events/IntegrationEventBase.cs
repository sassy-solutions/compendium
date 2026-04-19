// -----------------------------------------------------------------------
// <copyright file="IntegrationEventBase.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events;

/// <summary>
/// Base class for integration events providing common properties and behavior.
/// Integration events are used for communication between different bounded contexts or external systems.
/// </summary>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventBase"/> class.
    /// </summary>
    /// <param name="eventVersion">The version of the event schema (default: 1).</param>
    /// <param name="correlationId">The correlation identifier for tracking related events.</param>
    /// <param name="causationId">The causation identifier linking this event to its cause.</param>
    protected IntegrationEventBase(int eventVersion = 1, string? correlationId = null, string? causationId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventVersion);

        EventId = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        EventVersion = eventVersion;
        CorrelationId = correlationId;
        CausationId = causationId;
    }

    /// <inheritdoc />
    public Guid EventId { get; private init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; private init; }

    /// <inheritdoc />
    public abstract string EventType { get; }

    /// <inheritdoc />
    public int EventVersion { get; private init; }

    /// <inheritdoc />
    public string? CorrelationId { get; private init; }

    /// <inheritdoc />
    public string? CausationId { get; private init; }

    /// <summary>
    /// Gets or sets the tenant identifier for multi-tenant systems.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the source system that produced this event.
    /// </summary>
    public string? SourceSystem { get; init; }
}
