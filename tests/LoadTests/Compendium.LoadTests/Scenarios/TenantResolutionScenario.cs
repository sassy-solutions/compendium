// -----------------------------------------------------------------------
// <copyright file="TenantResolutionScenario.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.LoadTests.Support;
using Compendium.Multitenancy;
using Compendium.Multitenancy.Stores;
using Microsoft.Extensions.Logging.Abstractions;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Compendium.LoadTests.Scenarios;

/// <summary>
/// Measures the throughput of a realistic tenant-resolution chain on the
/// request hot path: a header resolver followed by a host resolver, both
/// backed by an <see cref="InMemoryTenantStore"/> pre-populated with a
/// modest number of tenants. The mix purposefully includes cache misses
/// (unknown ids / hosts) so the cost of failing through the chain is also
/// captured in the percentile metrics.
/// </summary>
public static class TenantResolutionScenario
{
    /// <summary>
    /// Stable scenario name used in the CLI and in the JSON output filename.
    /// </summary>
    public const string Name = "tenant";

    private const int CopyCount = 64;
    private const int TenantCount = 200;

    /// <summary>
    /// Builds the scenario without registering it.
    /// </summary>
    public static ScenarioProps Build(ScenarioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var store = new InMemoryTenantStore();
        for (var i = 0; i < TenantCount; i++)
        {
            var tenant = new TenantInfo
            {
                Id = $"tenant-{i:D4}",
                Name = $"acme-{i:D4}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            _ = store.SaveAsync(tenant).GetAwaiter().GetResult();
            store.AddIdentifierMapping($"tenant-{i:D4}.example.com", tenant.Id);
        }

        var headerResolver = new HeaderTenantResolver(
            store,
            new HeaderTenantResolverOptions { HeaderName = "X-Tenant-ID" },
            NullLogger<HeaderTenantResolver>.Instance);

        var hostResolver = new HostTenantResolver(
            store,
            new HostTenantResolverOptions { UseSubdomain = false },
            NullLogger<HostTenantResolver>.Instance);

        var composite = new CompositeTenantResolver(
            new ITenantResolver[] { headerResolver, hostResolver },
            NullLogger<CompositeTenantResolver>.Instance);

        var scenario = Scenario.Create(Name, async _ =>
        {
            var rng = Random.Shared;
            var roll = rng.Next(10);
            var ctx = BuildContext(roll, rng);

            var result = await composite.ResolveTenantAsync(ctx);
            if (result.IsFailure)
            {
                return Response.Fail(message: result.Error.Message);
            }

            // We treat both "tenant resolved" and "no tenant" as Ok — a
            // production resolver returning null on a public endpoint is not
            // a failure, only an internal exception or store error is.
            return Response.Ok(sizeBytes: 64);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: CopyCount, during: options.Duration));

        scenario = options.Warmup
            ? scenario.WithWarmUpDuration(TimeSpan.FromSeconds(Math.Min(2, Math.Max(1, (int)options.Duration.TotalSeconds / 2))))
            : scenario.WithoutWarmUp();

        return scenario;
    }

    /// <summary>
    /// Returns a context that exercises a representative mix of resolution paths.
    /// </summary>
    private static TenantResolutionContext BuildContext(int roll, Random rng)
    {
        // 60% header hits, 20% host hits, 20% misses.
        if (roll < 6)
        {
            return new TenantResolutionContext
            {
                Headers = { ["X-Tenant-ID"] = $"tenant-{rng.Next(TenantCount):D4}" },
            };
        }

        if (roll < 8)
        {
            return new TenantResolutionContext
            {
                Host = $"tenant-{rng.Next(TenantCount):D4}.example.com",
            };
        }

        return new TenantResolutionContext
        {
            Host = $"unknown-{rng.Next(1_000_000):D6}.example.com",
        };
    }
}
