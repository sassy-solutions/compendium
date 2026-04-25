// -----------------------------------------------------------------------
// <copyright file="IIntegrationEventPublisher.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Choreography;

/// <summary>
/// Underlying transport port used by <see cref="IChoreographyContext"/> to publish
/// integration events. Compendium provides an in-memory implementation for tests; in
/// production this is wired to a transactional outbox + message broker.
/// </summary>
/// <remarks>
/// This is intentionally separate from any aggregate-level event-store concern: the
/// outbox publishes events to other bounded contexts; choreography handlers consume those.
/// Adapters (e.g. <c>Compendium.Adapters.PostgreSQL</c> outbox) implement this contract.
/// </remarks>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes a single integration event to the broker / outbox.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result, or a publishing error.</returns>
    Task<Result> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
