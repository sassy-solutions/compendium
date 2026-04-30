// -----------------------------------------------------------------------
// <copyright file="AgentTurn.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Models;

namespace Compendium.Abstractions.AI.Agents.Models;

/// <summary>
/// One round of the agent loop: an assistant response, optionally followed by tool
/// invocations whose results were fed back into the next turn.
/// </summary>
public sealed record AgentTurn
{
    /// <summary>1-based index of this turn within the run.</summary>
    public required int Index { get; init; }

    /// <summary>The raw assistant content (the model's textual output for this turn, including any action block).</summary>
    public required string AssistantContent { get; init; }

    /// <summary>
    /// Tool invocations the agent executed in response to this assistant turn.
    /// Empty when the model produced a final answer (no action block).
    /// </summary>
    public IReadOnlyList<AgentToolInvocation> ToolInvocations { get; init; } = Array.Empty<AgentToolInvocation>();

    /// <summary>Provider usage statistics for the assistant call that produced this turn.</summary>
    public UsageStats? Usage { get; init; }

    /// <summary>Wall-clock latency for this turn (assistant call + any tool invocations).</summary>
    public TimeSpan Latency { get; init; }

    /// <summary>UTC timestamp at which the turn started.</summary>
    public DateTime StartedAt { get; init; }
}
