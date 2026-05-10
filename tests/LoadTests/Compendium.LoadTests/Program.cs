// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.LoadTests.Scenarios;
using Compendium.LoadTests.Support;
using NBomber.Contracts;
using NBomber.CSharp;

var options = ArgsParser.Parse(args);
if (options is null)
{
    PrintUsage();
    return 64;
}

ScenarioProps scenario;
try
{
    scenario = options.Scenario switch
    {
        EventStoreAppendScenario.Name => EventStoreAppendScenario.Build(options),
        ProjectionCatchUpScenario.Name => ProjectionCatchUpScenario.Build(options),
        IdempotencyLookupScenario.Name => IdempotencyLookupScenario.Build(options),
        TenantResolutionScenario.Name => TenantResolutionScenario.Build(options),
        _ => null!,
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to build scenario '{options.Scenario}': {ex.Message}");
    return 70;
}

if (scenario is null)
{
    Console.Error.WriteLine($"Unknown scenario '{options.Scenario}'.");
    PrintUsage();
    return 64;
}

Directory.CreateDirectory(options.ArtifactsFolder);

Console.WriteLine($"Compendium load tests — scenario={options.Scenario} duration={options.Duration}");
Console.WriteLine($"Artifacts will be written to {Path.GetFullPath(options.ArtifactsFolder)}");

var stats = NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder(options.ArtifactsFolder)
    .WithReportFileName($"{options.Scenario}-report")
    .Run();

JsonSummaryWriter.Write(options, stats);

Console.WriteLine($"Done. Summary: {Path.Combine(options.ArtifactsFolder, options.Scenario + ".json")}");
return 0;

static void PrintUsage()
{
    Console.WriteLine("""
Compendium load tests — usage:
  dotnet run --project tests/LoadTests/Compendium.LoadTests -c Release -- \
    --scenario <name> [--duration 15s] [--artifacts artifacts/load] [--no-warmup]

Scenarios:
  eventstore   Sustained IEventStore.AppendEventsAsync throughput (in-memory)
  projection   Projection catch-up rate from version 0 to N (in-memory)
  idempotency  IIdempotencyStore exists/set latency under concurrency
  tenant       Composite TenantResolver chain throughput with hits and misses

Each run writes:
  <artifacts>/<scenario>-report.{html,csv,md,txt}    (NBomber native reports)
  <artifacts>/<scenario>.json                        (machine-readable summary)
""");
}

