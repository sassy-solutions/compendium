// -----------------------------------------------------------------------
// <copyright file="DomainPrimitiveBenchmarks.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Compendium.Core.PerfTests.Benchmarks;

/// <summary>Entity, ValueObject, and AggregateRoot benchmarks.</summary>
[MemoryDiagnoser]
public class DomainPrimitiveBenchmarks
{
    private BenchValueObject[] _valueObjects = null!;
    private BenchEntity[] _entities = null!;
    private BenchAggregate _aggregate = null!;
    private BenchDomainEvent[] _events = null!;

    [Params(100, 1000)]
    public int Items { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _valueObjects = Enumerable.Range(0, 100)
            .Select(i => new BenchValueObject($"Value{i}", i))
            .ToArray();

        _entities = Enumerable.Range(0, 100)
            .Select(_ => new BenchEntity(Guid.NewGuid()))
            .ToArray();

        _aggregate = new BenchAggregate(Guid.NewGuid());

        _events = Enumerable.Range(0, Items)
            .Select(i => new BenchDomainEvent(_aggregate.Id.ToString(), nameof(BenchAggregate), i))
            .ToArray();
    }

    [Benchmark]
    public int ValueObject_GetHashCode()
    {
        int sum = 0;
        for (int i = 0; i < _valueObjects.Length; i++)
        {
            sum += _valueObjects[i].GetHashCode();
        }

        return sum;
    }

    [Benchmark]
    public int Entity_Equals_NxN_Pairs()
    {
        int matches = 0;
        var take = Math.Min(10, _entities.Length);
        for (int i = 0; i < take; i++)
        {
            for (int j = 0; j < take; j++)
            {
                if (_entities[i].Equals(_entities[j]))
                {
                    matches++;
                }
            }
        }

        return matches;
    }

    [Benchmark]
    public int AggregateRoot_AddDomainEvent()
    {
        var aggregate = new BenchAggregate(Guid.NewGuid());
        for (int i = 0; i < _events.Length; i++)
        {
            aggregate.AppendEvent(_events[i]);
        }

        return aggregate.DomainEvents.Count;
    }
}
