// -----------------------------------------------------------------------
// <copyright file="SecureEventDeserializer.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Core.Domain.Events;
using Compendium.Core.Results;

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Secure event deserializer that prevents deserialization attacks by using a whitelist-based approach.
/// Only registered domain event types can be deserialized, preventing arbitrary code execution.
/// Automatically applies event version migrations using registered upcasters.
/// </summary>
public sealed class SecureEventDeserializer : IEventDeserializer
{
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IEventVersionMigrator? _eventVersionMigrator;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Action<string>? _logWarning;
    private readonly Action<Exception, string>? _logError;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureEventDeserializer"/> class.
    /// </summary>
    /// <param name="eventTypeRegistry">The event type registry for whitelisted types.</param>
    /// <param name="eventVersionMigrator">Optional event version migrator for automatic upcasting.</param>
    /// <param name="logWarning">Optional warning logger action.</param>
    /// <param name="logError">Optional error logger action.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    public SecureEventDeserializer(
        IEventTypeRegistry eventTypeRegistry,
        IEventVersionMigrator? eventVersionMigrator = null,
        Action<string>? logWarning = null,
        Action<Exception, string>? logError = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        _eventTypeRegistry = eventTypeRegistry ?? throw new ArgumentNullException(nameof(eventTypeRegistry));
        _eventVersionMigrator = eventVersionMigrator;
        _logWarning = logWarning;
        _logError = logError;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public IDomainEvent? DeserializeEvent(string eventData, string eventTypeName)
    {
        var result = TryDeserializeEvent(eventData, eventTypeName);
        return result.IsSuccess ? result.Value : null;
    }

    /// <inheritdoc />
    public T? DeserializeEvent<T>(string eventData) where T : class, IDomainEvent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventData);

        var eventTypeName = typeof(T).AssemblyQualifiedName!;

        // Security check: Verify type is whitelisted
        if (!_eventTypeRegistry.IsWhitelisted(eventTypeName))
        {
            _logWarning?.Invoke($"Attempted to deserialize non-whitelisted event type: {eventTypeName}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(eventData, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logError?.Invoke(ex, $"Failed to deserialize event of type {eventTypeName}");
            return null;
        }
        catch (Exception ex)
        {
            _logError?.Invoke(ex, $"Unexpected error deserializing event of type {eventTypeName}");
            return null;
        }
    }

    /// <inheritdoc />
    public Result<IDomainEvent> TryDeserializeEvent(string eventData, string eventTypeName)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventData);
            ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);

            // Security check: Verify type is whitelisted
            var eventType = _eventTypeRegistry.GetWhitelistedType(eventTypeName);
            if (eventType == null)
            {
                var error = $"Event type '{eventTypeName}' is not whitelisted for deserialization";
                _logWarning?.Invoke($"Security violation: {error}");

                return Result.Failure<IDomainEvent>(
                    Error.Validation("EventDeserializer.TypeNotWhitelisted", error));
            }

            // Perform safe deserialization
            var domainEvent = JsonSerializer.Deserialize(eventData, eventType, _jsonOptions) as IDomainEvent;
            if (domainEvent == null)
            {
                var error = $"Failed to deserialize event data to type '{eventTypeName}'";
                _logError?.Invoke(new InvalidOperationException(error), error);

                return Result.Failure<IDomainEvent>(
                    Error.Failure("EventDeserializer.DeserializationFailed", error));
            }

            // Apply event version migration if migrator is configured
            if (_eventVersionMigrator != null)
            {
                var migrationResult = _eventVersionMigrator.MigrateToLatest(domainEvent);
                if (migrationResult.IsFailure)
                {
                    _logError?.Invoke(
                        new InvalidOperationException(migrationResult.Error.Message),
                        $"Event migration failed: {migrationResult.Error.Message}");

                    return migrationResult;
                }

                domainEvent = migrationResult.Value;
            }

            return Result.Success<IDomainEvent>(domainEvent);
        }
        catch (JsonException ex)
        {
            var error = $"JSON deserialization error for type '{eventTypeName}': {ex.Message}";
            _logError?.Invoke(ex, error);

            return Result.Failure<IDomainEvent>(
                Error.Failure("EventDeserializer.JsonError", error));
        }
        catch (ArgumentException ex)
        {
            _logError?.Invoke(ex, "Invalid arguments for event deserialization");

            return Result.Failure<IDomainEvent>(
                Error.Validation("EventDeserializer.InvalidArguments", ex.Message));
        }
        catch (Exception ex)
        {
            var error = $"Unexpected error deserializing event type '{eventTypeName}': {ex.Message}";
            _logError?.Invoke(ex, error);

            return Result.Failure<IDomainEvent>(
                Error.Failure("EventDeserializer.UnexpectedError", error));
        }
    }
}
