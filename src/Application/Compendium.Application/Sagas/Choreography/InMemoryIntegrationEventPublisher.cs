// -----------------------------------------------------------------------
// <copyright file="InMemoryIntegrationEventPublisher.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Core.Domain.Events;

namespace Compendium.Application.Sagas.Choreography;

/// <summary>
/// In-memory <see cref="IIntegrationEventPublisher"/> intended for tests, samples, and
/// quick prototypes. Buffers published events for inspection and optionally re-routes
/// them through a <see cref="IChoreographyRouter"/> for end-to-end fan-out.
/// </summary>
public sealed class InMemoryIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ConcurrentQueue<IIntegrationEvent> _published = new();
    private readonly Func<IChoreographyRouter?>? _routerAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryIntegrationEventPublisher"/> class.
    /// </summary>
    /// <param name="routerAccessor">
    /// Optional accessor to a <see cref="IChoreographyRouter"/>; if supplied, every published
    /// event is also dispatched through the router (in-process choreography). Use a Func to
    /// break the circular dependency between publisher and router during DI construction.
    /// </param>
    public InMemoryIntegrationEventPublisher(Func<IChoreographyRouter?>? routerAccessor = null)
    {
        _routerAccessor = routerAccessor;
    }

    /// <summary>
    /// Gets the snapshot of every event published so far. Test-only.
    /// </summary>
    public IReadOnlyCollection<IIntegrationEvent> Published => _published.ToArray();

    /// <inheritdoc />
    public async Task<Result> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        _published.Enqueue(@event);

        var router = _routerAccessor?.Invoke();
        if (router is null)
        {
            return Result.Success();
        }

        return await router.DispatchAsync(@event, cancellationToken).ConfigureAwait(false);
    }
}
