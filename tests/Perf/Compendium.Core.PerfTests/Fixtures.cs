// -----------------------------------------------------------------------
// <copyright file="Fixtures.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Domain.Rules;
using Compendium.Core.Domain.Specifications;

namespace Compendium.Core.PerfTests;

// Minimal test fixtures used by the benchmark classes. Kept in this project
// (rather than referencing Compendium.Core.Tests) so the perf project does
// not depend on xUnit. Mirrors the equivalents in TestHelpers/TestFixtures.cs.

internal sealed class BenchValueObject : ValueObject
{
    public BenchValueObject(string? stringValue, int intValue)
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

internal sealed class BenchEntity : Entity<Guid>
{
    public BenchEntity(Guid id)
        : base(id)
    {
    }
}

internal sealed class BenchAggregate : AggregateRoot<Guid>
{
    public BenchAggregate(Guid id)
        : base(id)
    {
    }

    public void AppendEvent(IDomainEvent @event) => AddDomainEvent(@event);
}

internal sealed class BenchDomainEvent : DomainEventBase
{
    public BenchDomainEvent(string aggregateId, string aggregateType, long version)
        : base(aggregateId, aggregateType, version)
    {
    }
}

internal sealed class TrueRule : IBusinessRule
{
    public string Message => "always satisfied";

    public string ErrorCode => "RULE.OK";

    public bool IsBroken() => false;
}

internal sealed class FalseRule : IBusinessRule
{
    public string Message => "always broken";

    public string ErrorCode => "RULE.KO";

    public bool IsBroken() => true;
}

internal sealed class ParameterisedRule : IBusinessRule
{
    private readonly string _value;
    private readonly int _minLength;

    public ParameterisedRule(string value, int minLength)
    {
        _value = value;
        _minLength = minLength;
    }

    public string Message => $"value must be at least {_minLength} chars";

    public string ErrorCode => "RULE.LEN";

    public bool IsBroken() => _value.Length < _minLength;
}

internal sealed class SpecTarget
{
    public int Id { get; init; }

    public int Age { get; init; }

    public bool IsActive { get; init; }
}

internal sealed class AgeRangeSpec : Specification<SpecTarget>
{
    public AgeRangeSpec(int min, int max)
        : base(x => x.Age >= min && x.Age <= max)
    {
    }
}

internal sealed class IdSpec : Specification<SpecTarget>
{
    public IdSpec(int id)
        : base(x => x.Id == id)
    {
    }
}

internal sealed class ActiveSpec : Specification<SpecTarget>
{
    public ActiveSpec()
        : base(x => x.IsActive)
    {
    }
}
