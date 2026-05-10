// -----------------------------------------------------------------------
// <copyright file="SecureEventDeserializerMigrationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Core.EventSourcing;
using NSubstitute;

namespace Compendium.Core.Tests.EventSourcing;

/// <summary>
/// Covers the integration between <see cref="SecureEventDeserializer"/> and
/// <see cref="IEventVersionMigrator"/> — including success migration, failed migration,
/// and the optional migrator-not-supplied branch.
/// </summary>
public class SecureEventDeserializerMigrationTests
{
    private sealed class SimpleEvent : DomainEventBase
    {
        public SimpleEvent(string aggregateId, string data)
            : base(aggregateId, "Simple", 0)
        {
            Data = data;
        }

        public string Data { get; init; } = string.Empty;
    }

    [Fact]
    public void TryDeserializeEvent_WithMigrator_AppliesMigrationAndReturnsMigratedEvent()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(SimpleEvent));

        var migratedEvent = new SimpleEvent("agg-1", "migrated");
        var migrator = Substitute.For<IEventVersionMigrator>();
        migrator.MigrateToLatest(Arg.Any<IDomainEvent>())
            .Returns(_ => Result.Success<IDomainEvent>(migratedEvent));

        var deserializer = new SecureEventDeserializer(registry, migrator);

        var original = new SimpleEvent("agg-1", "original");
        var json = JsonSerializer.Serialize(original);

        // Act
        var result = deserializer.TryDeserializeEvent(json, typeof(SimpleEvent).AssemblyQualifiedName!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(migratedEvent);
        migrator.Received(1).MigrateToLatest(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public void TryDeserializeEvent_WhenMigrationFails_PropagatesFailureResult()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(SimpleEvent));

        var migrationError = Error.Failure("Migration.Boom", "could not migrate");
        var migrator = Substitute.For<IEventVersionMigrator>();
        migrator.MigrateToLatest(Arg.Any<IDomainEvent>())
            .Returns(_ => Result.Failure<IDomainEvent>(migrationError));

        Exception? loggedException = null;
        string? loggedMessage = null;
        var deserializer = new SecureEventDeserializer(
            registry,
            migrator,
            logError: (ex, msg) =>
            {
                loggedException = ex;
                loggedMessage = msg;
            });

        var original = new SimpleEvent("agg-1", "original");
        var json = JsonSerializer.Serialize(original);

        // Act
        var result = deserializer.TryDeserializeEvent(json, typeof(SimpleEvent).AssemblyQualifiedName!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(migrationError);
        loggedException.Should().NotBeNull();
        loggedMessage.Should().Contain("could not migrate");
    }

    [Fact]
    public void TryDeserializeEvent_WhenInputDeserializesToNull_ReturnsFailureResult()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(SimpleEvent));
        Exception? loggedException = null;
        var deserializer = new SecureEventDeserializer(
            registry,
            logError: (ex, _) => loggedException = ex);

        // "null" JSON literal will deserialize to a null reference for the event type.
        const string nullJson = "null";

        // Act
        var result = deserializer.TryDeserializeEvent(nullJson, typeof(SimpleEvent).AssemblyQualifiedName!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventDeserializer.DeserializationFailed");
        loggedException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeEvent_WithGenericOverload_NonWhitelisted_LogsWarningAndReturnsNull()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        string? warning = null;
        var deserializer = new SecureEventDeserializer(
            registry,
            logWarning: msg => warning = msg);

        var src = new SimpleEvent("agg-1", "data");
        var json = JsonSerializer.Serialize(src);

        // Act
        var result = deserializer.DeserializeEvent<SimpleEvent>(json);

        // Assert
        result.Should().BeNull();
        warning.Should().NotBeNull();
        warning.Should().Contain("non-whitelisted");
    }

    [Fact]
    public void DeserializeEvent_WithGenericOverload_Whitelisted_ReturnsTypedEvent()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(SimpleEvent));
        var deserializer = new SecureEventDeserializer(registry);

        var src = new SimpleEvent("agg-1", "hello");
        var json = JsonSerializer.Serialize(src);

        // Act
        var result = deserializer.DeserializeEvent<SimpleEvent>(json);

        // Assert
        result.Should().NotBeNull();
        result!.AggregateId.Should().Be("agg-1");
        result.Data.Should().Be("hello");
    }

    [Fact]
    public void DeserializeEvent_WithGenericOverload_AndInvalidJson_LogsErrorAndReturnsNull()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(SimpleEvent));
        Exception? loggedException = null;
        var deserializer = new SecureEventDeserializer(
            registry,
            logError: (ex, _) => loggedException = ex);

        // Act
        var result = deserializer.DeserializeEvent<SimpleEvent>("{bad json");

        // Assert
        result.Should().BeNull();
        loggedException.Should().BeOfType<JsonException>();
    }

    [Fact]
    public void DeserializeEvent_WithGenericOverload_NullOrWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        var deserializer = new SecureEventDeserializer(registry);

        // Act
        var act = () => deserializer.DeserializeEvent<SimpleEvent>("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SecureEventDeserializer(eventTypeRegistry: null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventTypeRegistry");
    }

    [Fact]
    public void Constructor_WithCustomJsonOptions_UsesProvidedOptions()
    {
        // Arrange
        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(SimpleEvent));
        var customOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserializer = new SecureEventDeserializer(registry, jsonOptions: customOptions);

        var src = new SimpleEvent("agg-1", "abc");
        var json = JsonSerializer.Serialize(src);

        // Act
        var result = deserializer.TryDeserializeEvent(json, typeof(SimpleEvent).AssemblyQualifiedName!);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
