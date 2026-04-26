// -----------------------------------------------------------------------
// <copyright file="IHandle.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Choreography;

/// <summary>
/// Handler contract for choreography saga participants. A class can implement
/// <see cref="IHandle{TEvent}"/> several times, once per event type it reacts to.
/// </summary>
/// <typeparam name="TEvent">The integration event type this handler reacts to.</typeparam>
public interface IHandle<in TEvent> : IEventChoreography
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Handle an inbound integration event. Implementations may publish further events
    /// (forward progression) or compensation events (backward progression) via
    /// <paramref name="context"/>. The orchestrator does not interpret the result;
    /// returning a failure surfaces the error to telemetry but does not retry — implement
    /// retry/backoff at the message-broker level or via a transactional outbox.
    /// </summary>
    /// <param name="event">The inbound event.</param>
    /// <param name="context">Publishing/correlation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result on completion; failures are surfaced to telemetry.</returns>
    Task<Result> HandleAsync(TEvent @event, IChoreographyContext context, CancellationToken cancellationToken = default);
}
