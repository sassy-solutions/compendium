// -----------------------------------------------------------------------
// <copyright file="CountingProjection.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.EventSourcing;

namespace Compendium.LoadTests.Support;

/// <summary>
/// Snapshot record carried by <see cref="CountingProjection"/>.
/// </summary>
public sealed record CountingProjectionSnapshot
{
    /// <summary>
    /// Number of events applied since the last reset.
    /// </summary>
    public long Processed { get; init; }
}

/// <summary>
/// Trivial projection that just increments a counter — used to measure the
/// per-event overhead of <see cref="IProjectionManager"/> without letting
/// projection-side work dominate the timing.
/// </summary>
public sealed class CountingProjection : ProjectionBase<LoadTestEvent, CountingProjectionSnapshot>
{
    /// <inheritdoc />
    public override string ProjectionId => "loadtests-counting-projection";

    /// <inheritdoc />
    protected override Task HandleEventAsync(LoadTestEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var current = State;
        UpdateState(current with { Processed = current.Processed + 1 });
        return Task.CompletedTask;
    }
}
