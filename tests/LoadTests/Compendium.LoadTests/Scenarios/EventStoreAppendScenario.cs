// -----------------------------------------------------------------------
// <copyright file="EventStoreAppendScenario.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.Infrastructure.EventSourcing;
using Compendium.LoadTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Compendium.LoadTests.Scenarios;

/// <summary>
/// Measures sustained <see cref="IEventStore.AppendEventsAsync(string,IEnumerable{IDomainEvent},long,CancellationToken)"/>
/// throughput against the in-memory implementation. Each iteration appends a
/// 10-event batch to a fresh aggregate so the contention is on the dictionary
/// and the writer lock, not on optimistic-concurrency conflicts.
/// </summary>
public static class EventStoreAppendScenario
{
    /// <summary>
    /// Stable scenario name used in the CLI and in the JSON output filename.
    /// </summary>
    public const string Name = "eventstore";

    private const int EventsPerAppend = 10;
    private const int CopyCount = 32;

    /// <summary>
    /// Builds the scenario without registering it; the caller composes load
    /// simulations and runs it through <see cref="NBomberRunner"/>.
    /// </summary>
    public static ScenarioProps Build(ScenarioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(LoadTestEvent));
        var deserializer = new SecureEventDeserializer(registry);
        var eventStore = new InMemoryEventStore(deserializer, NullLogger<InMemoryEventStore>.Instance);

        var scenario = Scenario.Create(Name, async _ =>
        {
            var aggregateId = $"agg-{Guid.NewGuid():N}";
            var batch = LoadTestEvent.Batch(aggregateId, EventsPerAppend);

            var result = await eventStore.AppendEventsAsync(aggregateId, batch, expectedVersion: -1);

            return result.IsSuccess
                ? Response.Ok(sizeBytes: EventsPerAppend * 128)
                : Response.Fail(message: result.Error.Message);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: CopyCount, during: options.Duration));

        scenario = options.Warmup
            ? scenario.WithWarmUpDuration(TimeSpan.FromSeconds(Math.Min(2, Math.Max(1, (int)options.Duration.TotalSeconds / 2))))
            : scenario.WithoutWarmUp();

        return scenario;
    }
}
