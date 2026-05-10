// -----------------------------------------------------------------------
// <copyright file="ProjectionCatchUpScenario.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing;
using Compendium.Infrastructure.EventSourcing;
using Compendium.LoadTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Compendium.LoadTests.Scenarios;

/// <summary>
/// Measures the rate at which <see cref="ProjectionManager"/> can catch a
/// projection up from version 0 to version <c>N</c>. Each NBomber iteration
/// rebuilds a freshly-reset projection over a pre-populated aggregate so the
/// hot path being timed is purely the replay loop.
/// </summary>
public static class ProjectionCatchUpScenario
{
    /// <summary>
    /// Stable scenario name used in the CLI and in the JSON output filename.
    /// </summary>
    public const string Name = "projection";

    private const int EventCountPerAggregate = 5_000;
    private const int CopyCount = 1;

    /// <summary>
    /// Builds the scenario without registering it. A single copy is used
    /// because each iteration is intentionally heavy (replays N events) and
    /// throughput is best read as <c>events / iteration_duration</c>.
    /// </summary>
    public static ScenarioProps Build(ScenarioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var registry = new EventTypeRegistry();
        registry.RegisterEventType(typeof(LoadTestEvent));
        var deserializer = new SecureEventDeserializer(registry);
        var eventStore = new InMemoryEventStore(deserializer, NullLogger<InMemoryEventStore>.Instance);

        const string aggregateId = "projection-loadtest-aggregate";
        var batch = LoadTestEvent.Batch(aggregateId, EventCountPerAggregate);
        var seedResult = eventStore.AppendEventsAsync(aggregateId, batch, expectedVersion: -1).GetAwaiter().GetResult();
        if (seedResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to seed projection events: {seedResult.Error.Message}");
        }

        var projectionManager = new ProjectionManager(eventStore, NullLogger<ProjectionManager>.Instance);
        var projection = new CountingProjection();
        projectionManager.RegisterProjection(projection);

        var scenario = Scenario.Create(Name, async _ =>
        {
            await projection.ResetAsync();

            var result = await projectionManager.RebuildProjectionAsync(projection.ProjectionId, aggregateId);

            // NBomber already times each iteration end-to-end and surfaces
            // mean / p50 / p99 latencies; events-per-second is simply
            // N / mean-latency in the report.
            return result.IsSuccess
                ? Response.Ok(sizeBytes: EventCountPerAggregate * 128)
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
