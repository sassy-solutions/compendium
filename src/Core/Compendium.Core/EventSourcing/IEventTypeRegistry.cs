// -----------------------------------------------------------------------
// <copyright file="IEventTypeRegistry.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
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
