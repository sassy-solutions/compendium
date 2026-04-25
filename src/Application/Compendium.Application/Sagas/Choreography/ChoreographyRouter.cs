// -----------------------------------------------------------------------
// <copyright file="ChoreographyRouter.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Core.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Application.Sagas.Choreography;

/// <summary>
/// Default <see cref="IChoreographyRouter"/> that resolves all registered
/// <see cref="IHandle{TEvent}"/> implementations from DI and dispatches the event to
/// each. Failures from individual handlers are aggregated into a single error.
/// </summary>
public sealed class ChoreographyRouter : IChoreographyRouter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIntegrationEventPublisher _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChoreographyRouter"/> class.
    /// </summary>
    /// <param name="serviceProvider">DI container used to resolve <see cref="IHandle{TEvent}"/> handlers.</param>
    /// <param name="publisher">Underlying integration-event publisher used to construct the per-handler context.</param>
    public ChoreographyRouter(IServiceProvider serviceProvider, IIntegrationEventPublisher publisher)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <inheritdoc />
    public async Task<Result> DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Resolve handlers by the runtime event type, not the compile-time TEvent — this
        // matters when the caller has a generic IIntegrationEvent reference.
        var runtimeEventType = @event.GetType();
        var handlerInterfaceType = typeof(IHandle<>).MakeGenericType(runtimeEventType);
        var handlersEnumerable = _serviceProvider.GetServices(handlerInterfaceType);

        var correlationId = @event.CorrelationId ?? @event.EventId.ToString();
        var causationId = @event.EventId.ToString();
        var context = new ChoreographyContext(correlationId, causationId, _publisher);

        var failures = new List<Error>();
        foreach (var handler in handlersEnumerable)
        {
            if (handler is null)
            {
                continue;
            }

            var method = handlerInterfaceType.GetMethod(nameof(IHandle<IIntegrationEvent>.HandleAsync));
            if (method is null)
            {
                failures.Add(Error.Failure(
                    "Choreography.HandlerInvalid",
                    $"Handler {handler.GetType().FullName} does not expose HandleAsync."));
                continue;
            }

            try
            {
                var task = (Task<Result>)method.Invoke(handler, new object[] { @event, context, cancellationToken })!;
                var result = await task.ConfigureAwait(false);
                if (result.IsFailure)
                {
                    failures.Add(result.Error);
                }
            }
            catch (TargetInvocationException tie) when (tie.InnerException is OperationCanceledException oce)
            {
                // Reflection wraps the real exception; rethrow cancellation so callers observe it.
                throw oce;
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                failures.Add(Error.Failure(
                    "Choreography.HandlerThrew",
                    $"Handler {handler.GetType().FullName} threw: {inner.Message}"));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(Error.Failure(
                    "Choreography.HandlerThrew",
                    $"Handler {handler.GetType().FullName} threw: {ex.Message}"));
            }
        }

        if (failures.Count == 0)
        {
            return Result.Success();
        }

        var aggregated = string.Join("; ", failures.Select(f => $"[{f.Code}] {f.Message}"));
        return Result.Failure(Error.Failure("Choreography.HandlerFailures", aggregated));
    }
}
