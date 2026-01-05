// -----------------------------------------------------------------------
// <copyright file="TestFixtures.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing.Attributes;

namespace Compendium.Core.Tests.TestHelpers;

/// <summary>
/// Test implementation of Entity for testing purposes.
/// </summary>
public class TestEntity : Entity<Guid>
{
    public TestEntity(Guid id, string name = "Test Entity") : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public void TestCheckRule(IBusinessRule rule) => CheckRule(rule);

    public void TestTouch() => Touch();

    public void TestClearBrokenRules() => ClearBrokenRules();

    public void UpdateName(string name)
    {
        Name = name;
        Touch();
    }
}

/// <summary>
/// Test implementation of AggregateRoot for testing purposes.
/// </summary>
public class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate(Guid id, string name = "Test Aggregate") : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public void TestAddDomainEvent(IDomainEvent @event) => AddDomainEvent(@event);

    public void TestRemoveDomainEvent(IDomainEvent @event) => RemoveDomainEvent(@event);

    public void TestIncrementVersion() => IncrementVersion();

    public void TestSetVersion(long version) => SetVersion(version);

    public void UpdateName(string name)
    {
        Name = name;
        AddDomainEvent(new TestDomainEvent(Id.ToString(), nameof(TestAggregate), Version + 1, $"Name updated to {name}"));
        IncrementVersion();
    }
}

/// <summary>
/// Test implementation of ValueObject for testing purposes.
/// </summary>
public class TestValueObject : ValueObject
{
    public TestValueObject(string? stringValue, int intValue)
    {
        StringValue = stringValue;
        IntValue = intValue;
    }

    public string? StringValue { get; }
    public int IntValue { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StringValue;
        yield return IntValue;
    }
}

/// <summary>
/// Test implementation of DomainEvent for testing purposes.
/// </summary>
[AutoRegisterEvent(Priority = 100)]
public class TestDomainEvent : DomainEventBase
{
    public TestDomainEvent(string aggregateId, string aggregateType, long aggregateVersion, string? data = null)
        : base(aggregateId, aggregateType, aggregateVersion)
    {
        Data = data ?? "Test event data";
    }

    public string Data { get; }
}

/// <summary>
/// Test implementation of IBusinessRule for testing purposes.
/// </summary>
public class TestBusinessRule : IBusinessRule
{
    private readonly bool _isBroken;

    public TestBusinessRule(bool isBroken, string message = "Test rule violation", string errorCode = "TEST_001")
    {
        _isBroken = isBroken;
        Message = message;
        ErrorCode = errorCode;
    }

    public string Message { get; }
    public string ErrorCode { get; }

    public bool IsBroken() => _isBroken;
}

/// <summary>
/// Test implementation of Specification for testing purposes.
/// </summary>
public class TestSpecification : Specification<int>
{
    public TestSpecification(Func<int, bool> predicate) : base(x => predicate(x))
    {
    }
}

/// <summary>
/// Expression-based specification for testing.
/// </summary>
public class ExpressionSpecification<T> : Specification<T>
{
    public ExpressionSpecification(System.Linq.Expressions.Expression<Func<T, bool>> criteria) : base(criteria)
    {
    }
}

/// <summary>
/// Test data factory for creating test objects.
/// </summary>
public static class TestData
{
    public static class Entities
    {
        public static TestEntity CreateValid(Guid? id = null, string? name = null)
            => new(id ?? Guid.NewGuid(), name ?? "Valid Entity");

        public static TestEntity CreateWithEmptyId()
            => new(Guid.Empty, "Invalid Entity");
    }

    public static class Aggregates
    {
        public static TestAggregate CreateValid(Guid? id = null, string? name = null)
            => new(id ?? Guid.NewGuid(), name ?? "Valid Aggregate");

        public static TestAggregate CreateWithEvents(int eventCount = 3)
        {
            var aggregate = CreateValid();
            for (int i = 0; i < eventCount; i++)
            {
                aggregate.TestAddDomainEvent(new TestDomainEvent(
                    aggregate.Id.ToString(),
                    nameof(TestAggregate),
                    i,
                    $"Event {i}"));
            }
            return aggregate;
        }
    }

    public static class ValueObjects
    {
        public static TestValueObject CreateValid(string? stringValue = null, int? intValue = null)
            => new(stringValue ?? "Valid Value", intValue ?? 42);

        public static TestValueObject CreateWithNullString()
            => new(null, 42);
    }

    public static class Events
    {
        public static TestDomainEvent CreateValid(string? aggregateId = null, string? data = null)
            => new(aggregateId ?? Guid.NewGuid().ToString(), nameof(TestAggregate), 1, data);
    }

    public static class BusinessRules
    {
        public static TestBusinessRule CreateValid() => new(false);
        public static TestBusinessRule CreateBroken(string? message = null) => new(true, message ?? "Rule is broken");
    }

    public static class Errors
    {
        public static Error CreateValidation(string? code = null, string? message = null)
            => Error.Validation(code ?? "VAL_001", message ?? "Validation failed");

        public static Error CreateNotFound(string? code = null, string? message = null)
            => Error.NotFound(code ?? "NOT_FOUND", message ?? "Resource not found");

        public static Error CreateConflict(string? code = null, string? message = null)
            => Error.Conflict(code ?? "CONFLICT", message ?? "Resource conflict");
    }
}
