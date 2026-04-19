// -----------------------------------------------------------------------
// <copyright file="IEventTypeRegistry.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Registry for whitelisted event types to prevent deserialization attacks.
/// </summary>
public interface IEventTypeRegistry
{
    /// <summary>
    /// Checks if an event type is whitelisted for safe deserialization.
    /// </summary>
    /// <param name="typeName">The type name to check.</param>
    /// <returns>True if the type is whitelisted, false otherwise.</returns>
    bool IsWhitelisted(string typeName);

    /// <summary>
    /// Gets the Type object for a whitelisted event type.
    /// </summary>
    /// <param name="typeName">The type name to resolve.</param>
    /// <returns>The Type object if whitelisted, null otherwise.</returns>
    Type? GetWhitelistedType(string typeName);

    /// <summary>
    /// Registers an event type in the whitelist.
    /// </summary>
    /// <param name="eventType">The event type to register.</param>
    void RegisterEventType(Type eventType);

    /// <summary>
    /// Gets all registered event types.
    /// </summary>
    /// <returns>A collection of all registered event types.</returns>
    IReadOnlyCollection<Type> GetRegisteredTypes();
}
