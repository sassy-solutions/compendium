// -----------------------------------------------------------------------
// <copyright file="IChoreographyContext.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Choreography;

/// <summary>
/// Per-invocation context handed to a <see cref="IHandle{TEvent}"/> to publish forward
/// or compensation events while preserving correlation identifiers across the chain.
/// </summary>
public interface IChoreographyContext
{
    /// <summary>
    /// Gets the correlation identifier shared across the entire saga chain. Propagated to
    /// every event published through this context.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the identifier of the event that triggered the current handler invocation —
    /// used to populate the next event's <see cref="IIntegrationEvent.CausationId"/>.
    /// </summary>
    string CausationId { get; }

    /// <summary>
    /// Publishes the next event in the choreography chain via the underlying integration-event
    /// publisher (typically a transactional outbox).
    /// </summary>
    /// <remarks>
    /// <see cref="IIntegrationEvent"/> exposes <see cref="IIntegrationEvent.CorrelationId"/>
    /// and <see cref="IIntegrationEvent.CausationId"/> as read-only properties, so callers
    /// must populate them on the event being constructed — typically by reading
    /// <see cref="CorrelationId"/> and <see cref="CausationId"/> from this context. Custom
    /// publishers that prefer envelope-based propagation can override this contract.
    /// </remarks>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result, or a publishing error.</returns>
    Task<Result> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Publishes a compensation event. Semantically equivalent to <see cref="PublishAsync{TEvent}"/>
    /// but tagged in telemetry as a compensation step so dashboards can distinguish forward
    /// progress from rollback.
    /// </summary>
    /// <typeparam name="TEvent">The compensation event type.</typeparam>
    /// <param name="compensationEvent">The compensation event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result, or a publishing error.</returns>
    Task<Result> PublishCompensationAsync<TEvent>(TEvent compensationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
