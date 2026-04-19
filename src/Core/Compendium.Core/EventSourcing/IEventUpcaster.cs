// -----------------------------------------------------------------------
// <copyright file="IEventUpcaster.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Interface for upcasting events from an old schema version to a new version.
/// Event upcasters enable schema evolution while maintaining backward compatibility.
/// </summary>
/// <remarks>
/// When implementing an upcaster:
/// 1. Specify the source event version (SourceVersion)
/// 2. Specify the target event version (TargetVersion)
/// 3. Implement the Upcast method to transform old events to new structure
/// 4. Register the upcaster with the EventVersionMigrator
///
/// Example:
/// <code>
/// public class ConfigurationCreatedV1ToV2Upcaster : IEventUpcaster&lt;ConfigurationCreatedV1, ConfigurationCreatedV2&gt;
/// {
///     public int SourceVersion => 1;
///     public int TargetVersion => 2;
///
///     public ConfigurationCreatedV2 Upcast(ConfigurationCreatedV1 oldEvent)
///     {
///         return new ConfigurationCreatedV2(
///             oldEvent.ConfigurationId,
///             oldEvent.Key,
///             oldEvent.Value,
///             oldEvent.CreatedBy,
///             oldEvent.CreatedAt,
///             environment: "production" // New field with default value
///         );
///     }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TSource">The source (old) event type.</typeparam>
/// <typeparam name="TTarget">The target (new) event type.</typeparam>
public interface IEventUpcaster<in TSource, out TTarget>
    where TSource : IDomainEvent
    where TTarget : IDomainEvent
{
    /// <summary>
    /// Gets the source event version that this upcaster can migrate from.
    /// </summary>
    int SourceVersion { get; }

    /// <summary>
    /// Gets the target event version that this upcaster migrates to.
    /// </summary>
    int TargetVersion { get; }

    /// <summary>
    /// Upcasts an old event to a new event version.
    /// </summary>
    /// <param name="sourceEvent">The source event to upcast.</param>
    /// <returns>The upcasted event with the new schema.</returns>
    TTarget Upcast(TSource sourceEvent);
}

/// <summary>
/// Non-generic interface for event upcasters.
/// Used for registration and discovery of upcasters at runtime.
/// </summary>
public interface IEventUpcaster
{
    /// <summary>
    /// Gets the source event type.
    /// </summary>
    Type SourceEventType { get; }

    /// <summary>
    /// Gets the target event type.
    /// </summary>
    Type TargetEventType { get; }

    /// <summary>
    /// Gets the source event version.
    /// </summary>
    int SourceVersion { get; }

    /// <summary>
    /// Gets the target event version.
    /// </summary>
    int TargetVersion { get; }

    /// <summary>
    /// Determines if this upcaster can migrate the specified event type and version.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <param name="eventVersion">The event version to check.</param>
    /// <returns>True if this upcaster can handle the migration, false otherwise.</returns>
    bool CanUpcast(Type eventType, int eventVersion);

    /// <summary>
    /// Upcasts an event from the old version to the new version.
    /// </summary>
    /// <param name="sourceEvent">The source event to upcast.</param>
    /// <returns>The upcasted event.</returns>
    IDomainEvent Upcast(IDomainEvent sourceEvent);
}
