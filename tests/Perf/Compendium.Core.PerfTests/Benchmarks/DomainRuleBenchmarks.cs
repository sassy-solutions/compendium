// -----------------------------------------------------------------------
// <copyright file="DomainRuleBenchmarks.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Compendium.Core.Domain.Rules;
using Compendium.Core.Domain.Specifications;

namespace Compendium.Core.PerfTests.Benchmarks;

/// <summary>BusinessRule and Specification benchmarks.</summary>
[MemoryDiagnoser]
public class DomainRuleBenchmarks
{
    private IBusinessRule[] _rules = null!;
    private BrokenRuleHolder _brokenRule = null!;
    private AgeRangeSpec _ageSpec = null!;
    private IdSpec _idSpec = null!;
    private ActiveSpec _activeSpec = null!;
    private SpecTarget _target = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rules =
        [
            new TrueRule(),
            new FalseRule(),
            new ParameterisedRule("hello", 3),
            new ParameterisedRule("hi", 3),
        ];

        _brokenRule = new BrokenRuleHolder(new FalseRule());

        _ageSpec = new AgeRangeSpec(18, 65);
        _idSpec = new IdSpec(1);
        _activeSpec = new ActiveSpec();
        _target = new SpecTarget { Id = 1, Age = 25, IsActive = true };
    }

    [Benchmark]
    public int BusinessRule_Evaluation_Sweep()
    {
        int triggered = 0;
        for (int i = 0; i < _rules.Length; i++)
        {
            var rule = _rules[i];
            if (rule.IsBroken())
            {
                triggered++;
            }

            _ = rule.Message;
            _ = rule.ErrorCode;
        }

        return triggered;
    }

    [Benchmark]
    public BusinessRuleValidationException BusinessRuleValidationException_Creation()
    {
        var ex = new BusinessRuleValidationException(_brokenRule.Rule);
        _ = ex.Message;
        _ = ex.ErrorCode;
        _ = ex.BrokenRule;
        return ex;
    }

    [Benchmark]
    public bool Specification_Evaluation()
        => _ageSpec.IsSatisfiedBy(_target);

    [Benchmark]
    public bool Specification_Composition_AndOrNot()
    {
        var combined = _idSpec.And(_activeSpec).Or(_idSpec.Not());
        return combined.IsSatisfiedBy(_target);
    }

    private sealed record BrokenRuleHolder(IBusinessRule Rule);
}
