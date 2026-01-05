// -----------------------------------------------------------------------
// <copyright file="SecureEventDeserializerTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Core.EventSourcing;
using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.EventSourcing;

public class SecureEventDeserializerTests
{
    private readonly EventTypeRegistry _registry;
    private readonly SecureEventDeserializer _deserializer;
    private readonly JsonSerializerOptions _jsonOptions;

    public SecureEventDeserializerTests()
    {
        _registry = new EventTypeRegistry();
        _deserializer = new SecureEventDeserializer(_registry);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    [Fact]
    public void TryDeserializeEvent_WithWhitelistedType_ReturnsSuccess()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventTypeName = typeof(TestDomainEvent).AssemblyQualifiedName!;
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        _registry.RegisterEventType(typeof(TestDomainEvent));

        // Act
        var result = _deserializer.TryDeserializeEvent(eventData, eventTypeName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<TestDomainEvent>();
        var deserializedEvent = (TestDomainEvent)result.Value;
        deserializedEvent.AggregateId.Should().Be("test-id");
        deserializedEvent.Data.Should().Be("test data");
    }

    [Fact]
    public void TryDeserializeEvent_WithNonWhitelistedType_ReturnsFailure()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventTypeName = typeof(TestDomainEvent).AssemblyQualifiedName!;
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        // Don't register the type in the registry

        // Act
        var result = _deserializer.TryDeserializeEvent(eventData, eventTypeName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventDeserializer.TypeNotWhitelisted");
        result.Error.Message.Should().Contain("not whitelisted for deserialization");
    }

    [Fact]
    public void TryDeserializeEvent_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var eventTypeName = typeof(TestDomainEvent).AssemblyQualifiedName!;
        var invalidJson = "{ invalid json }";

        _registry.RegisterEventType(typeof(TestDomainEvent));

        // Act
        var result = _deserializer.TryDeserializeEvent(invalidJson, eventTypeName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventDeserializer.JsonError");
    }

    [Fact]
    public void TryDeserializeEvent_WithNullOrEmptyEventData_ReturnsFailure()
    {
        // Arrange
        var eventTypeName = typeof(TestDomainEvent).AssemblyQualifiedName!;
        _registry.RegisterEventType(typeof(TestDomainEvent));

        // Act & Assert
        var result1 = _deserializer.TryDeserializeEvent("", eventTypeName);
        result1.IsFailure.Should().BeTrue();
        result1.Error.Code.Should().Be("EventDeserializer.InvalidArguments");

        var result2 = _deserializer.TryDeserializeEvent(null!, eventTypeName);
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("EventDeserializer.InvalidArguments");
    }

    [Fact]
    public void TryDeserializeEvent_WithNullOrEmptyTypeName_ReturnsFailure()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        // Act & Assert
        var result1 = _deserializer.TryDeserializeEvent(eventData, "");
        result1.IsFailure.Should().BeTrue();
        result1.Error.Code.Should().Be("EventDeserializer.InvalidArguments");

        var result2 = _deserializer.TryDeserializeEvent(eventData, null!);
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("EventDeserializer.InvalidArguments");
    }

    [Fact]
    public void DeserializeEvent_Generic_WithWhitelistedType_ReturnsEvent()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        _registry.RegisterEventType(typeof(TestDomainEvent));

        // Act
        var result = _deserializer.DeserializeEvent<TestDomainEvent>(eventData);

        // Assert
        result.Should().NotBeNull();
        result!.AggregateId.Should().Be("test-id");
        result.Data.Should().Be("test data");
    }

    [Fact]
    public void DeserializeEvent_Generic_WithNonWhitelistedType_ReturnsNull()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        // Don't register the type

        // Act
        var result = _deserializer.DeserializeEvent<TestDomainEvent>(eventData);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DeserializeEvent_WithWhitelistedType_ReturnsEvent()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventTypeName = typeof(TestDomainEvent).AssemblyQualifiedName!;
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        _registry.RegisterEventType(typeof(TestDomainEvent));

        // Act
        var result = _deserializer.DeserializeEvent(eventData, eventTypeName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestDomainEvent>();
    }

    [Fact]
    public void DeserializeEvent_WithNonWhitelistedType_ReturnsNull()
    {
        // Arrange
        var testEvent = new TestDomainEvent("test-id", "TestAggregate", 1, "test data");
        var eventTypeName = typeof(TestDomainEvent).AssemblyQualifiedName!;
        var eventData = JsonSerializer.Serialize(testEvent, _jsonOptions);

        // Don't register the type

        // Act
        var result = _deserializer.DeserializeEvent(eventData, eventTypeName);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new SecureEventDeserializer(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTypeRegistry");
    }
}
