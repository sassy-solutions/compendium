// -----------------------------------------------------------------------
// <copyright file="E2EEventDeserializer.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Core.Results;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.Events;

namespace Compendium.IntegrationTests.EndToEnd.Infrastructure;

/// <summary>
/// Event deserializer for E2E test events.
/// Handles deserialization of Order aggregate events.
/// </summary>
public sealed class E2EEventDeserializer : IEventDeserializer
{
    private readonly JsonSerializerOptions _jsonOptions;

    public E2EEventDeserializer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public Result<IDomainEvent> TryDeserializeEvent(string eventData, string eventType)
    {
        try
        {
            // Handle different event types
            if (eventType.Contains(nameof(OrderPlacedEvent)))
            {
                var @event = JsonSerializer.Deserialize<OrderPlacedEvent>(eventData, _jsonOptions);
                return Result.Success<IDomainEvent>(@event!);
            }

            if (eventType.Contains(nameof(OrderLineAddedEvent)))
            {
                var @event = JsonSerializer.Deserialize<OrderLineAddedEvent>(eventData, _jsonOptions);
                return Result.Success<IDomainEvent>(@event!);
            }

            if (eventType.Contains(nameof(OrderCompletedEvent)))
            {
                var @event = JsonSerializer.Deserialize<OrderCompletedEvent>(eventData, _jsonOptions);
                return Result.Success<IDomainEvent>(@event!);
            }

            return Result.Failure<IDomainEvent>(
                Error.Validation("Deserializer.UnknownType", $"Unknown event type: {eventType}"));
        }
        catch (Exception ex)
        {
            return Result.Failure<IDomainEvent>(
                Error.Failure("Deserializer.Failed", $"Failed to deserialize event: {ex.Message}"));
        }
    }

    public IDomainEvent? DeserializeEvent(string eventData, string eventTypeName)
    {
        var result = TryDeserializeEvent(eventData, eventTypeName);
        return result.IsSuccess ? result.Value : null;
    }

    public T? DeserializeEvent<T>(string eventData) where T : class, IDomainEvent
    {
        var result = TryDeserializeEvent(eventData, typeof(T).AssemblyQualifiedName ?? typeof(T).Name);
        return result.IsSuccess ? result.Value as T : null;
    }
}
