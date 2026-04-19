// -----------------------------------------------------------------------
// <copyright file="IGeneratedEventRegistry.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Interface for generated event registry implementations.
/// Source generators implement this interface to provide compile-time safe event registration.
/// </summary>
public interface IGeneratedEventRegistry
{
    /// <summary>
    /// Registers all discovered event types into the provided event type registry.
    /// This method is generated at compile time based on attributes and assembly scanning.
    /// </summary>
    /// <param name="registry">The event type registry to populate.</param>
    void RegisterEvents(IEventTypeRegistry registry);

    /// <summary>
    /// Gets metadata about the registered event types.
    /// </summary>
    /// <returns>A collection of event type metadata.</returns>
    IReadOnlyCollection<EventTypeMetadata> GetEventMetadata();
}

/// <summary>
/// Metadata about a registered event type.
/// </summary>
public sealed class EventTypeMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeMetadata"/> class.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="typeName">The assembly qualified type name.</param>
    /// <param name="priority">The registration priority.</param>
    /// <param name="sourceAssembly">The source assembly name.</param>
    public EventTypeMetadata(Type type, string typeName, int priority, string sourceAssembly)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        Priority = priority;
        SourceAssembly = sourceAssembly ?? throw new ArgumentNullException(nameof(sourceAssembly));
    }

    /// <summary>
    /// Gets the event type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the assembly qualified type name.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the registration priority.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets the source assembly name.
    /// </summary>
    public string SourceAssembly { get; }
}
