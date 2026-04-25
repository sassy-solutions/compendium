// -----------------------------------------------------------------------
// <copyright file="ChoreographyContext.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Core.Domain.Events;

namespace Compendium.Application.Sagas.Choreography;

/// <summary>
/// Default <see cref="IChoreographyContext"/> implementation. Forwards published events
/// to an injected <see cref="IIntegrationEventPublisher"/> and tags them as
/// "compensation" or "forward" steps for telemetry.
/// </summary>
internal sealed class ChoreographyContext : IChoreographyContext
{
    private readonly IIntegrationEventPublisher _publisher;

    public ChoreographyContext(string correlationId, string causationId, IIntegrationEventPublisher publisher)
    {
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        CausationId = causationId ?? throw new ArgumentNullException(nameof(causationId));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public string CorrelationId { get; }

    public string CausationId { get; }

    public Task<Result> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        return _publisher.PublishAsync(@event, cancellationToken);
    }

    public Task<Result> PublishCompensationAsync<TEvent>(TEvent compensationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        // Compensation publishing follows the same path; the distinction is purely a
        // telemetry tag (added by the dispatcher's activity wrapping).
        return _publisher.PublishAsync(compensationEvent, cancellationToken);
    }
}
