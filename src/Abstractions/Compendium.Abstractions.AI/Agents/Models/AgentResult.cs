// -----------------------------------------------------------------------
// <copyright file="AgentResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Models;

namespace Compendium.Abstractions.AI.Agents.Models;

/// <summary>
/// Outcome of an <see cref="IAgent"/> run.
/// </summary>
public sealed record AgentResult
{
    /// <summary>The text the agent ultimately produced (post tool-use loop).</summary>
    public required string FinalOutput { get; init; }

    /// <summary>The turn-by-turn audit trail. Each entry captures one assistant + optional tool round-trip.</summary>
    public required IReadOnlyList<AgentTurn> Turns { get; init; }

    /// <summary>Why the loop stopped — see <see cref="AgentTerminationReason"/>.</summary>
    public required AgentTerminationReason TerminationReason { get; init; }

    /// <summary>Sum of provider usage across all turns.</summary>
    public required UsageStats TotalUsage { get; init; }
}

/// <summary>
/// Why an agent loop terminated.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentTerminationReason
{
    /// <summary>The model produced a final answer (no further tool call).</summary>
    Completed,

    /// <summary>The loop hit <see cref="AgentLoopOptions.MaxTurns"/> before the model concluded.</summary>
    MaxTurnsReached,

    /// <summary>Cumulative usage exceeded <see cref="AgentLoopOptions.MaxTotalTokens"/>.</summary>
    TokenBudgetExhausted,

    /// <summary>The configured timeout elapsed.</summary>
    Timeout,

    /// <summary>Cancellation was requested by the caller.</summary>
    Cancelled,
}
