// -----------------------------------------------------------------------
// <copyright file="IChoreographyRouter.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Choreography;

/// <summary>
/// Dispatches inbound integration events to all registered choreography handlers
/// (<see cref="IHandle{TEvent}"/>) of a matching event type. Wiring is typically
/// done in DI via assembly-scanning extension methods.
/// </summary>
public interface IChoreographyRouter
{
    /// <summary>
    /// Dispatches a single integration event to every registered handler that implements
    /// <see cref="IHandle{TEvent}"/> for that event's runtime type. Errors from individual
    /// handlers are aggregated; one failing handler does not prevent others from running.
    /// </summary>
    /// <typeparam name="TEvent">The compile-time event type.</typeparam>
    /// <param name="event">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result if all handlers succeeded; otherwise an aggregated failure.</returns>
    Task<Result> DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
