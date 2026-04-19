// -----------------------------------------------------------------------
// <copyright file="EventUpcasterBase.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Abstract base class for event upcasters.
/// Provides common implementation of the non-generic IEventUpcaster interface.
/// </summary>
/// <typeparam name="TSource">The source (old) event type.</typeparam>
/// <typeparam name="TTarget">The target (new) event type.</typeparam>
public abstract class EventUpcasterBase<TSource, TTarget> : IEventUpcaster<TSource, TTarget>, IEventUpcaster
    where TSource : IDomainEvent
    where TTarget : IDomainEvent
{
    /// <inheritdoc />
    public abstract int SourceVersion { get; }

    /// <inheritdoc />
    public abstract int TargetVersion { get; }

    /// <inheritdoc />
    public Type SourceEventType => typeof(TSource);

    /// <inheritdoc />
    public Type TargetEventType => typeof(TTarget);

    /// <inheritdoc />
    public abstract TTarget Upcast(TSource sourceEvent);

    /// <inheritdoc />
    public bool CanUpcast(Type eventType, int eventVersion)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return eventType == typeof(TSource) && eventVersion == SourceVersion;
    }

    /// <inheritdoc />
    public IDomainEvent Upcast(IDomainEvent sourceEvent)
    {
        ArgumentNullException.ThrowIfNull(sourceEvent);

        if (sourceEvent is not TSource typedSourceEvent)
        {
            throw new InvalidOperationException(
                $"Cannot upcast event of type {sourceEvent.GetType().Name}. Expected type: {typeof(TSource).Name}");
        }

        if (sourceEvent.EventVersion != SourceVersion)
        {
            throw new InvalidOperationException(
                $"Cannot upcast event version {sourceEvent.EventVersion}. Expected version: {SourceVersion}");
        }

        return Upcast(typedSourceEvent);
    }
}
