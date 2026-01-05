// -----------------------------------------------------------------------
// <copyright file="EventTypeRegistryTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Core.EventSourcing;
using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.EventSourcing;

public class EventTypeRegistryTests : IDisposable
{
    private readonly EventTypeRegistry _registry;

    public EventTypeRegistryTests()
    {
        _registry = new EventTypeRegistry();
    }

    [Fact]
    public void RegisterEventType_WithValidDomainEvent_RegistersSuccessfully()
    {
        // Arrange
        var eventType = typeof(TestDomainEvent);

        // Act
        _registry.RegisterEventType(eventType);

        // Assert
        var typeName = eventType.AssemblyQualifiedName!;
        _registry.IsWhitelisted(typeName).Should().BeTrue();
        _registry.GetWhitelistedType(typeName).Should().Be(eventType);
        _registry.Count.Should().Be(1);
    }

    [Fact]
    public void RegisterEventType_WithNonDomainEventType_ThrowsArgumentException()
    {
        // Arrange
        var nonEventType = typeof(string);

        // Act & Assert
        var action = () => _registry.RegisterEventType(nonEventType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must implement IDomainEvent*");
    }

    [Fact]
    public void RegisterEventType_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _registry.RegisterEventType(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsWhitelisted_WithNonRegisteredType_ReturnsFalse()
    {
        // Arrange
        var typeName = typeof(TestDomainEvent).AssemblyQualifiedName!;

        // Act & Assert
        _registry.IsWhitelisted(typeName).Should().BeFalse();
    }

    [Fact]
    public void IsWhitelisted_WithNullOrEmptyTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        var action1 = () => _registry.IsWhitelisted(null!);
        action1.Should().Throw<ArgumentException>();

        var action2 = () => _registry.IsWhitelisted("");
        action2.Should().Throw<ArgumentException>();

        var action3 = () => _registry.IsWhitelisted("   ");
        action3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetWhitelistedType_WithNonRegisteredType_ReturnsNull()
    {
        // Arrange
        var typeName = typeof(TestDomainEvent).AssemblyQualifiedName!;

        // Act & Assert
        _registry.GetWhitelistedType(typeName).Should().BeNull();
    }

    [Fact]
    public void GetWhitelistedType_WithNullOrEmptyTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        var action1 = () => _registry.GetWhitelistedType(null!);
        action1.Should().Throw<ArgumentException>();

        var action2 = () => _registry.GetWhitelistedType("");
        action2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterEventTypes_WithMultipleValidTypes_RegistersAll()
    {
        // Arrange
        var eventTypes = new[] { typeof(TestDomainEvent) };

        // Act
        _registry.RegisterEventTypes(eventTypes);

        // Assert
        _registry.Count.Should().Be(1);
        _registry.IsWhitelisted(typeof(TestDomainEvent).AssemblyQualifiedName!).Should().BeTrue();
    }

    [Fact]
    public void RegisterEventTypes_WithEmptyCollection_DoesNothing()
    {
        // Arrange
        var eventTypes = Array.Empty<Type>();

        // Act
        _registry.RegisterEventTypes(eventTypes);

        // Assert
        _registry.Count.Should().Be(0);
    }

    [Fact]
    public void RegisterEventTypes_WithInvalidType_ThrowsArgumentException()
    {
        // Arrange
        var eventTypes = new[] { typeof(string) };

        // Act & Assert
        var action = () => _registry.RegisterEventTypes(eventTypes);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must implement IDomainEvent*");
    }

    [Fact]
    public void AutoRegisterFromAssemblies_WithValidAssembly_RegistersDomainEvents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        _registry.AutoRegisterFromAssemblies(assembly);

        // Assert
        _registry.Count.Should().BeGreaterThan(0);
        _registry.IsWhitelisted(typeof(TestDomainEvent).AssemblyQualifiedName!).Should().BeTrue();
    }

    [Fact]
    public void AutoRegisterFromAssemblies_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _registry.AutoRegisterFromAssemblies(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetRegisteredTypes_ReturnsAllRegisteredTypes()
    {
        // Arrange
        _registry.RegisterEventType(typeof(TestDomainEvent));

        // Act
        var registeredTypes = _registry.GetRegisteredTypes();

        // Assert
        registeredTypes.Should().HaveCount(1);
        registeredTypes.Should().Contain(typeof(TestDomainEvent));
    }

    [Fact]
    public void Clear_RemovesAllRegisteredTypes()
    {
        // Arrange
        _registry.RegisterEventType(typeof(TestDomainEvent));
        _registry.Count.Should().Be(1);

        // Act
        _registry.Clear();

        // Assert
        _registry.Count.Should().Be(0);
        _registry.IsWhitelisted(typeof(TestDomainEvent).AssemblyQualifiedName!).Should().BeFalse();
    }

    [Fact]
    public void RegisterEventType_SameTypeTwice_OnlyRegistersOnce()
    {
        // Arrange
        var eventType = typeof(TestDomainEvent);

        // Act
        _registry.RegisterEventType(eventType);
        _registry.RegisterEventType(eventType);

        // Assert
        _registry.Count.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var eventTypes = new[] { typeof(TestDomainEvent) };

        // Act - Concurrent registration and reading
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _registry.RegisterEventTypes(eventTypes)));
            tasks.Add(Task.Run(() => _registry.IsWhitelisted(typeof(TestDomainEvent).AssemblyQualifiedName!)));
            tasks.Add(Task.Run(() => _registry.GetRegisteredTypes()));
        }

        // Assert - All tasks complete without exceptions
        var aggregateTask = Task.WhenAll(tasks);
        await aggregateTask.WaitAsync(TimeSpan.FromSeconds(5));
        _registry.Count.Should().Be(1);
    }

    [Fact]
    public void Dispose_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _registry.Dispose();

        // Act & Assert
        var action1 = () => _registry.RegisterEventType(typeof(TestDomainEvent));
        action1.Should().Throw<ObjectDisposedException>();

        var action2 = () => _registry.IsWhitelisted("test");
        action2.Should().Throw<ObjectDisposedException>();

        var action3 = () => _registry.GetWhitelistedType("test");
        action3.Should().Throw<ObjectDisposedException>();

        var action4 = () => _registry.GetRegisteredTypes();
        action4.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _registry?.Dispose();
    }
}
