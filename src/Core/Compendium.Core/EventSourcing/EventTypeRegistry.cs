// -----------------------------------------------------------------------
// <copyright file="EventTypeRegistry.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Frozen;
using System.Reflection;
using Compendium.Core.Domain.Events;
using Compendium.Core.Domain.Primitives;

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Thread-safe registry for whitelisted event types using .NET 9 frozen collections for optimal performance.
/// Prevents deserialization attacks by maintaining a strict whitelist of allowed domain event types.
/// </summary>
public sealed class EventTypeRegistry : IEventTypeRegistry, IDisposable
{
    private readonly ILockingStrategy _lockingStrategy;
    private readonly Dictionary<string, Type> _registeredTypes = new();
    private FrozenDictionary<string, Type>? _frozenCache;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeRegistry"/> class.
    /// </summary>
    /// <param name="lockingStrategy">The locking strategy to use for thread safety.</param>
    public EventTypeRegistry(ILockingStrategy? lockingStrategy = null)
    {
        _lockingStrategy = lockingStrategy ?? new ReaderWriterLockStrategy();
    }

    /// <inheritdoc />
    public bool IsWhitelisted(string typeName)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        return _lockingStrategy.ExecuteRead(() =>
        {
            var cache = _frozenCache ??= _registeredTypes.ToFrozenDictionary();
            return cache.ContainsKey(typeName);
        });
    }

    /// <inheritdoc />
    public Type? GetWhitelistedType(string typeName)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        return _lockingStrategy.ExecuteRead(() =>
        {
            var cache = _frozenCache ??= _registeredTypes.ToFrozenDictionary();
            cache.TryGetValue(typeName, out var type);
            return type;
        });
    }

    /// <inheritdoc />
    public void RegisterEventType(Type eventType)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(eventType);

        if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException($"Type {eventType.FullName} must implement {nameof(IDomainEvent)}", nameof(eventType));
        }

        _lockingStrategy.ExecuteWrite(() =>
        {
            var typeName = eventType.AssemblyQualifiedName!;
            _registeredTypes[typeName] = eventType;

            // Invalidate cache to force recreation
            _frozenCache = null;
        });
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Type> GetRegisteredTypes()
    {
        ThrowIfDisposed();

        return _lockingStrategy.ExecuteRead(() =>
        {
            var cache = _frozenCache ??= _registeredTypes.ToFrozenDictionary();
            return cache.Values.ToFrozenSet();
        });
    }

    /// <summary>
    /// Registers multiple event types at once for efficient batch registration.
    /// </summary>
    /// <param name="eventTypes">The event types to register.</param>
    public void RegisterEventTypes(IEnumerable<Type> eventTypes)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(eventTypes);

        var types = eventTypes.ToList();
        if (types.Count == 0)
        {
            return;
        }

        // Validate all types first
        foreach (var eventType in types)
        {
            if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
            {
                throw new ArgumentException($"Type {eventType.FullName} must implement {nameof(IDomainEvent)}", nameof(eventTypes));
            }
        }

        _lockingStrategy.ExecuteWrite(() =>
        {
            foreach (var eventType in types)
            {
                var typeName = eventType.AssemblyQualifiedName!;
                _registeredTypes[typeName] = eventType;
            }

            // Invalidate cache to force recreation
            _frozenCache = null;
        });
    }

    /// <summary>
    /// Automatically discovers and registers all domain event types from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for domain events.</param>
    public void AutoRegisterFromAssemblies(params Assembly[] assemblies)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(assemblies);

        var eventTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IDomainEvent).IsAssignableFrom(type) &&
                          !type.IsAbstract &&
                          !type.IsInterface)
            .ToList();

        RegisterEventTypes(eventTypes);
    }

    /// <summary>
    /// Clears all registered event types. Use with caution.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();

        _lockingStrategy.ExecuteWrite(() =>
        {
            _registeredTypes.Clear();
            _frozenCache = null;
        });
    }

    /// <summary>
    /// Gets the number of registered event types.
    /// </summary>
    public int Count => _lockingStrategy.ExecuteRead(() => _registeredTypes.Count);

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventTypeRegistry));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _lockingStrategy.Dispose();
            _registeredTypes.Clear();
            _frozenCache = null;
            _disposed = true;
        }
    }
}
