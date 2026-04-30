// -----------------------------------------------------------------------
// <copyright file="IAgent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Abstractions.AI.Agents;

/// <summary>
/// An AI agent: takes a user prompt + a tool registry, runs a multi-turn loop with
/// the underlying <see cref="IAIProvider"/>, and returns the final answer plus a
/// turn-by-turn audit trail.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation (<c>StandardAgent</c> in <c>Compendium.Application</c>)
/// uses ReAct-style prompt injection — tool descriptions and an action grammar are
/// rendered into the system prompt, and the agent parses an <c>```action</c> block
/// out of the model's response to decide whether to invoke a tool. This keeps the
/// agent layer decoupled from any provider's native tool-calling format.
/// </para>
/// <para>
/// <see cref="AgentResult.Turns"/> is the audit trail you persist for observability,
/// debugging, and replay. Each turn carries the model output, tool invocations,
/// usage stats, and latency.
/// </para>
/// </remarks>
public interface IAgent
{
    /// <summary>
    /// Runs the agent for a single user prompt. The agent may take multiple turns
    /// internally (capped by <see cref="AgentLoopOptions.MaxTurns"/>) before producing
    /// a final answer.
    /// </summary>
    /// <param name="request">The user prompt + tool registry + per-call options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> wrapping the <see cref="AgentResult"/> on success or
    /// an error from the underlying provider / tool registry / parser.
    /// </returns>
    Task<Result<AgentResult>> RunAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
