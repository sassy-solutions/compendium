// -----------------------------------------------------------------------
// <copyright file="AgentLoopOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Agents.Models;

/// <summary>
/// Bounds + observability hooks for an agent loop. Defaults are conservative enough
/// to be safe in tests and small demos; production callers should tune them per
/// workload.
/// </summary>
public sealed record AgentLoopOptions
{
    /// <summary>
    /// Maximum number of assistant turns before the loop terminates with
    /// <see cref="AgentTerminationReason.MaxTurnsReached"/>. Default 10.
    /// </summary>
    public int MaxTurns { get; init; } = 10;

    /// <summary>
    /// Optional total-token budget (across all turns). When the cumulative provider
    /// usage exceeds this value, the loop terminates with
    /// <see cref="AgentTerminationReason.TokenBudgetExhausted"/>. Default <see langword="null"/> (unbounded).
    /// </summary>
    public int? MaxTotalTokens { get; init; }

    /// <summary>
    /// Wall-clock timeout for the whole loop. When elapsed, the loop terminates with
    /// <see cref="AgentTerminationReason.Timeout"/> after the in-flight call returns.
    /// Default 5 minutes.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Optional callback invoked after each turn completes. Useful for streaming UIs
    /// or telemetry sinks that want progress events without buffering the whole run.
    /// Exceptions thrown from the callback are logged and swallowed to keep the loop
    /// running.
    /// </summary>
    public Action<AgentTurn>? OnTurnCompleted { get; init; }
}
