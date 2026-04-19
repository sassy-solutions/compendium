// -----------------------------------------------------------------------
// <copyright file="IEventDeserializer.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;
using Compendium.Core.Results;

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Secure event deserializer that prevents deserialization attacks using whitelisted types.
/// </summary>
public interface IEventDeserializer
{
    /// <summary>
    /// Safely deserializes an event from JSON using the whitelisted type registry.
    /// </summary>
    /// <param name="eventData">The serialized event data.</param>
    /// <param name="eventTypeName">The event type name to deserialize to.</param>
    /// <returns>The deserialized domain event if successful and whitelisted, null otherwise.</returns>
    IDomainEvent? DeserializeEvent(string eventData, string eventTypeName);

    /// <summary>
    /// Safely deserializes an event from JSON using the whitelisted type registry.
    /// </summary>
    /// <typeparam name="T">The expected event type.</typeparam>
    /// <param name="eventData">The serialized event data.</param>
    /// <returns>The deserialized domain event if successful and whitelisted, null otherwise.</returns>
    T? DeserializeEvent<T>(string eventData) where T : class, IDomainEvent;

    /// <summary>
    /// Attempts to safely deserialize an event and returns a result indicating success or failure.
    /// </summary>
    /// <param name="eventData">The serialized event data.</param>
    /// <param name="eventTypeName">The event type name to deserialize to.</param>
    /// <returns>A result containing the deserialized event or error information.</returns>
    Result<IDomainEvent> TryDeserializeEvent(string eventData, string eventTypeName);
}
