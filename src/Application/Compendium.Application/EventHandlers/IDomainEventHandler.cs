// -----------------------------------------------------------------------
// <copyright file="IDomainEventHandler.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;

namespace Compendium.Application.EventHandlers;

/// <summary>
/// Handles a domain event of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The domain event type handled by this handler.</typeparam>
/// <remarks>
/// Minimal in-process handler contract. Handlers are registered against their closed-generic
/// interface in DI and invoked directly. Auto-dispatch from the event store pipeline is not
/// wired up yet and is intentionally out of scope for POM-120.
/// </remarks>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
