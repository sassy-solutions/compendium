// -----------------------------------------------------------------------
// <copyright file="IdempotencyLookupScenario.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Application.Idempotency;
using Compendium.LoadTests.Support;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Compendium.LoadTests.Scenarios;

/// <summary>
/// Stress-tests the <see cref="IIdempotencyStore"/> abstraction with a mix of
/// reads and writes. Roughly 80&#160;% of iterations are <c>ExistsAsync</c>
/// (the hot path on every command handler) and 20&#160;% are <c>SetAsync</c>
/// to simulate the first-time write that follows a cache miss.
/// </summary>
public static class IdempotencyLookupScenario
{
    /// <summary>
    /// Stable scenario name used in the CLI and in the JSON output filename.
    /// </summary>
    public const string Name = "idempotency";

    private const int CopyCount = 64;
    private const int PrePopulatedKeys = 10_000;

    /// <summary>
    /// Builds the scenario without registering it.
    /// </summary>
    public static ScenarioProps Build(ScenarioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var store = new InMemoryIdempotencyStore();
        var keys = new string[PrePopulatedKeys];
        for (var i = 0; i < PrePopulatedKeys; i++)
        {
            keys[i] = $"idem-{i:D6}";
            var seed = store.SetAsync(keys[i], true, TimeSpan.FromHours(1)).GetAwaiter().GetResult();
            if (seed.IsFailure)
            {
                throw new InvalidOperationException($"Seeding failed: {seed.Error.Message}");
            }
        }

        var scenario = Scenario.Create(Name, async _ =>
        {
            var rng = Random.Shared;
            var key = keys[rng.Next(PrePopulatedKeys)];

            // 80 / 20 split between read and write.
            if (rng.Next(10) < 8)
            {
                var existsResult = await store.ExistsAsync(key);
                return existsResult.IsSuccess
                    ? Response.Ok(sizeBytes: 32)
                    : Response.Fail(message: existsResult.Error.Message);
            }

            var setResult = await store.SetAsync($"{key}-{Guid.NewGuid():N}", true, TimeSpan.FromMinutes(5));
            return setResult.IsSuccess
                ? Response.Ok(sizeBytes: 64)
                : Response.Fail(message: setResult.Error.Message);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: CopyCount, during: options.Duration));

        scenario = options.Warmup
            ? scenario.WithWarmUpDuration(TimeSpan.FromSeconds(Math.Min(2, Math.Max(1, (int)options.Duration.TotalSeconds / 2))))
            : scenario.WithoutWarmUp();

        return scenario;
    }
}
