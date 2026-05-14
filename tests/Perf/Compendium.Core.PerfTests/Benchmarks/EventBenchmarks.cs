// -----------------------------------------------------------------------
// <copyright file="EventBenchmarks.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.PerfTests.Benchmarks;

/// <summary>Domain event and integration event creation benchmarks.</summary>
[MemoryDiagnoser]
public class EventBenchmarks
{
    [Benchmark]
    public object DomainEvent_Creation()
        => new BenchDomainEvent("aggregate-1", "BenchAggregate", 1);

    [Benchmark]
    public SubscriptionCreatedEvent IntegrationEvent_Creation()
        => new SubscriptionCreatedEvent(
            SubscriptionId: "sub-1",
            CustomerId: "cust-1",
            PlanId: "plan-123",
            Status: "active",
            BillingPeriodStart: DateTimeOffset.UtcNow,
            BillingPeriodEnd: DateTimeOffset.UtcNow.AddMonths(1));
}
