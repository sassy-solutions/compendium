// -----------------------------------------------------------------------
// <copyright file="DomainEventTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Domain.Events;

public class DomainEventTests
{
    #region Test Domain Events

    private class TestDomainEventImpl : DomainEventBase
    {
        public TestDomainEventImpl(string aggregateId, string aggregateType, long aggregateVersion, string? data = null)
            : base(aggregateId, aggregateType, aggregateVersion)
        {
            Data = data ?? "Default test data";
        }

        public string Data { get; }
    }

    private class UserCreatedEvent : DomainEventBase
    {
        public UserCreatedEvent(string userId, string email, string name)
            : base(userId, nameof(User), 1)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; }
        public string Name { get; }
    }

    private class OrderPlacedEvent : DomainEventBase
    {
        public OrderPlacedEvent(string orderId, string customerId, decimal amount, long version)
            : base(orderId, nameof(Order), version)
        {
            CustomerId = customerId;
            Amount = amount;
        }

        public string CustomerId { get; }
        public decimal Amount { get; }
    }

    private class TestIntegrationEvent : IIntegrationEvent
    {
        public TestIntegrationEvent(string? correlationId = null, string? causationId = null)
        {
            EventId = Guid.NewGuid();
            OccurredOn = DateTimeOffset.UtcNow;
            EventType = GetType().Name;
            EventVersion = 1;
            CorrelationId = correlationId;
            CausationId = causationId;
        }

        public Guid EventId { get; }
        public DateTimeOffset OccurredOn { get; }
        public string EventType { get; }
        public int EventVersion { get; }
        public string? CorrelationId { get; }
        public string? CausationId { get; }
    }

    // Helper classes for testing
    private class User { }
    private class Order { }

    #endregion

    #region IDomainEvent Interface Tests

    [Fact]
    public void IDomainEvent_HasRequiredProperties()
    {
        // Arrange
        var domainEvent = new TestDomainEventImpl("test-id", "TestAggregate", 1);

        // Act & Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
        domainEvent.AggregateId.Should().Be("test-id");
        domainEvent.AggregateType.Should().Be("TestAggregate");
        domainEvent.AggregateVersion.Should().Be(1);
        domainEvent.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region DomainEventBase Tests

    [Fact]
    public void DomainEventBase_Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var aggregateId = "test-aggregate-123";
        var aggregateType = "TestAggregate";
        var aggregateVersion = 5L;
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestDomainEventImpl(aggregateId, aggregateType, aggregateVersion);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
        domainEvent.AggregateId.Should().Be(aggregateId);
        domainEvent.AggregateType.Should().Be(aggregateType);
        domainEvent.AggregateVersion.Should().Be(aggregateVersion);
        domainEvent.OccurredOn.Should().BeOnOrAfter(beforeCreation);
        domainEvent.OccurredOn.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void DomainEventBase_Constructor_WithNullAggregateId_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestDomainEventImpl(null!, "TestAggregate", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithEmptyAggregateId_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestDomainEventImpl(string.Empty, "TestAggregate", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithWhitespaceAggregateId_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestDomainEventImpl("   ", "TestAggregate", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithNullAggregateType_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestDomainEventImpl("test-id", null!, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithEmptyAggregateType_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestDomainEventImpl("test-id", string.Empty, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithWhitespaceAggregateType_ThrowsArgumentException()
    {
        // Act
        var act = () => new TestDomainEventImpl("test-id", "   ", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithNegativeAggregateVersion_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new TestDomainEventImpl("test-id", "TestAggregate", -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DomainEventBase_Constructor_WithZeroAggregateVersion_DoesNotThrow()
    {
        // Act
        var act = () => new TestDomainEventImpl("test-id", "TestAggregate", 0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void DomainEventBase_EventId_IsUnique()
    {
        // Act
        var event1 = new TestDomainEventImpl("test-id", "TestAggregate", 1);
        var event2 = new TestDomainEventImpl("test-id", "TestAggregate", 1);

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void DomainEventBase_ToString_ReturnsFormattedString()
    {
        // Arrange
        var domainEvent = new TestDomainEventImpl("test-id", "TestAggregate", 5);

        // Act
        var result = domainEvent.ToString();

        // Assert
        result.Should().Contain("TestDomainEventImpl");
        result.Should().Contain($"EventId={domainEvent.EventId}");
        result.Should().Contain("AggregateId=test-id");
        result.Should().Contain("AggregateType=TestAggregate");
        result.Should().Contain("Version=5");
        result.Should().Contain("OccurredOn=");
    }

    #endregion

    #region Specific Domain Event Tests

    [Fact]
    public void UserCreatedEvent_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var userEvent = new UserCreatedEvent(userId, email, name);

        // Assert
        userEvent.AggregateId.Should().Be(userId);
        userEvent.AggregateType.Should().Be(nameof(User));
        userEvent.AggregateVersion.Should().Be(1);
        userEvent.Email.Should().Be(email);
        userEvent.Name.Should().Be(name);
    }

    [Fact]
    public void OrderPlacedEvent_SetsPropertiesCorrectly()
    {
        // Arrange
        var orderId = "order-456";
        var customerId = "customer-789";
        var amount = 99.99m;
        var version = 3L;

        // Act
        var orderEvent = new OrderPlacedEvent(orderId, customerId, amount, version);

        // Assert
        orderEvent.AggregateId.Should().Be(orderId);
        orderEvent.AggregateType.Should().Be(nameof(Order));
        orderEvent.AggregateVersion.Should().Be(version);
        orderEvent.CustomerId.Should().Be(customerId);
        orderEvent.Amount.Should().Be(amount);
    }

    #endregion

    #region IIntegrationEvent Interface Tests

    [Fact]
    public void IIntegrationEvent_HasRequiredProperties()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent();

        // Act & Assert
        integrationEvent.EventId.Should().NotBe(Guid.Empty);
        integrationEvent.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        integrationEvent.EventType.Should().Be(nameof(TestIntegrationEvent));
        integrationEvent.EventVersion.Should().Be(1);
        integrationEvent.CorrelationId.Should().BeNull();
        integrationEvent.CausationId.Should().BeNull();
    }

    [Fact]
    public void TestIntegrationEvent_WithCorrelationAndCausation_SetsPropertiesCorrectly()
    {
        // Arrange
        var correlationId = "correlation-123";
        var causationId = "causation-456";

        // Act
        var integrationEvent = new TestIntegrationEvent(correlationId, causationId);

        // Assert
        integrationEvent.CorrelationId.Should().Be(correlationId);
        integrationEvent.CausationId.Should().Be(causationId);
    }

    [Fact]
    public void TestIntegrationEvent_EventId_IsUnique()
    {
        // Act
        var event1 = new TestIntegrationEvent();
        var event2 = new TestIntegrationEvent();

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    #endregion

    #region Integration with TestData

    [Fact]
    public void TestData_Events_CreateValid_WorksCorrectly()
    {
        // Act
        var domainEvent = TestData.Events.CreateValid();

        // Assert
        domainEvent.Should().NotBeNull();
        domainEvent.EventId.Should().NotBe(Guid.Empty);
        domainEvent.AggregateId.Should().NotBeNullOrEmpty();
        domainEvent.AggregateType.Should().Be(nameof(TestAggregate));
        domainEvent.AggregateVersion.Should().Be(1);
    }

    [Fact]
    public void TestData_Events_CreateValid_WithParameters_WorksCorrectly()
    {
        // Arrange
        var aggregateId = "custom-aggregate-id";
        var data = "custom data";

        // Act
        var domainEvent = TestData.Events.CreateValid(aggregateId, data);

        // Assert
        domainEvent.AggregateId.Should().Be(aggregateId);
        domainEvent.Data.Should().Be(data);
    }

    #endregion

    #region Performance Tests

    [Theory]
    [InlineData(1000)]
    public void DomainEvent_Creation_PerformanceTest(int iterations)
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = new TestDomainEventImpl($"aggregate-{i}", "TestAggregate", i);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Domain event creation should be fast");
    }

    [Theory]
    [InlineData(1000)]
    public void IntegrationEvent_Creation_PerformanceTest(int iterations)
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = new TestIntegrationEvent($"correlation-{i}", $"causation-{i}");
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Integration event creation should be fast");
    }

    [Fact]
    public void DomainEvent_ConcurrentCreation_ThreadSafe()
    {
        // Arrange
        var events = new List<TestDomainEventImpl>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, i =>
        {
            var domainEvent = new TestDomainEventImpl($"aggregate-{i}", "TestAggregate", i);
            lock (lockObject)
            {
                events.Add(domainEvent);
            }
        });

        // Assert
        events.Should().HaveCount(100);
        events.Select(e => e.EventId).Should().OnlyHaveUniqueItems();
        events.Select(e => e.AggregateId).Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DomainEvent_WithVeryLongAggregateId_HandlesCorrectly()
    {
        // Arrange
        var longAggregateId = new string('A', 10000);

        // Act
        var domainEvent = new TestDomainEventImpl(longAggregateId, "TestAggregate", 1);

        // Assert
        domainEvent.AggregateId.Should().Be(longAggregateId);
    }

    [Fact]
    public void DomainEvent_WithVeryLongAggregateType_HandlesCorrectly()
    {
        // Arrange
        var longAggregateType = new string('T', 10000);

        // Act
        var domainEvent = new TestDomainEventImpl("test-id", longAggregateType, 1);

        // Assert
        domainEvent.AggregateType.Should().Be(longAggregateType);
    }

    [Fact]
    public void DomainEvent_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var aggregateId = "test-id-ñáéíóú@#$%^&*()[]{}";
        var aggregateType = "TestAggregate-ñáéíóú@#$%";

        // Act
        var domainEvent = new TestDomainEventImpl(aggregateId, aggregateType, 1);

        // Assert
        domainEvent.AggregateId.Should().Be(aggregateId);
        domainEvent.AggregateType.Should().Be(aggregateType);
    }

    [Fact]
    public void DomainEvent_WithMaxLongVersion_HandlesCorrectly()
    {
        // Arrange
        var maxVersion = long.MaxValue;

        // Act
        var domainEvent = new TestDomainEventImpl("test-id", "TestAggregate", maxVersion);

        // Assert
        domainEvent.AggregateVersion.Should().Be(maxVersion);
    }

    [Fact]
    public void IntegrationEvent_WithNullCorrelationAndCausation_HandlesCorrectly()
    {
        // Act
        var integrationEvent = new TestIntegrationEvent(null, null);

        // Assert
        integrationEvent.CorrelationId.Should().BeNull();
        integrationEvent.CausationId.Should().BeNull();
    }

    [Fact]
    public void IntegrationEvent_WithEmptyCorrelationAndCausation_HandlesCorrectly()
    {
        // Act
        var integrationEvent = new TestIntegrationEvent(string.Empty, string.Empty);

        // Assert
        integrationEvent.CorrelationId.Should().Be(string.Empty);
        integrationEvent.CausationId.Should().Be(string.Empty);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void DomainEvent_OccurredOn_IsUtc()
    {
        // Act
        var domainEvent = new TestDomainEventImpl("test-id", "TestAggregate", 1);

        // Assert
        domainEvent.OccurredOn.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void DomainEvent_OccurredOn_IsReasonablyRecent()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestDomainEventImpl("test-id", "TestAggregate", 1);
        var after = DateTimeOffset.UtcNow;

        // Assert
        domainEvent.OccurredOn.Should().BeOnOrAfter(before);
        domainEvent.OccurredOn.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void IntegrationEvent_OccurredOn_IsUtc()
    {
        // Act
        var integrationEvent = new TestIntegrationEvent();

        // Assert
        integrationEvent.OccurredOn.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void IntegrationEvent_OccurredOn_IsReasonablyRecent()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var integrationEvent = new TestIntegrationEvent();
        var after = DateTimeOffset.UtcNow;

        // Assert
        integrationEvent.OccurredOn.Should().BeOnOrAfter(before);
        integrationEvent.OccurredOn.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Event Ordering Tests

    [Fact]
    public void DomainEvents_CreatedSequentially_HaveIncreasingTimestamps()
    {
        // Act
        var event1 = new TestDomainEventImpl("test-id", "TestAggregate", 1);
        Thread.Sleep(1); // Ensure different timestamps
        var event2 = new TestDomainEventImpl("test-id", "TestAggregate", 2);
        Thread.Sleep(1);
        var event3 = new TestDomainEventImpl("test-id", "TestAggregate", 3);

        // Assert
        event2.OccurredOn.Should().BeOnOrAfter(event1.OccurredOn);
        event3.OccurredOn.Should().BeOnOrAfter(event2.OccurredOn);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void DomainEvent_Properties_AreImmutable()
    {
        // Arrange
        var domainEvent = new TestDomainEventImpl("test-id", "TestAggregate", 1);
        var originalEventId = domainEvent.EventId;
        var originalAggregateId = domainEvent.AggregateId;
        var originalAggregateType = domainEvent.AggregateType;
        var originalVersion = domainEvent.AggregateVersion;
        var originalOccurredOn = domainEvent.OccurredOn;

        // Act & Assert - Properties should not have setters
        domainEvent.EventId.Should().Be(originalEventId);
        domainEvent.AggregateId.Should().Be(originalAggregateId);
        domainEvent.AggregateType.Should().Be(originalAggregateType);
        domainEvent.AggregateVersion.Should().Be(originalVersion);
        domainEvent.OccurredOn.Should().Be(originalOccurredOn);
    }

    #endregion
}
