// -----------------------------------------------------------------------
// <copyright file="AgentRequest.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Agents.Models;

/// <summary>
/// Input to <see cref="IAgent.RunAsync"/>.
/// </summary>
public sealed record AgentRequest
{
    /// <summary>The user-facing prompt the agent should respond to.</summary>
    public required string UserPrompt { get; init; }

    /// <summary>
    /// The provider model identifier (e.g. <c>"anthropic/claude-3.5-sonnet"</c>).
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Tools the agent may invoke during the loop. The agent renders their names +
    /// descriptions into the system prompt; matching invocations are dispatched
    /// through <see cref="IAgentToolRegistry"/>.
    /// </summary>
    public IReadOnlyList<AgentTool>? Tools { get; init; }

    /// <summary>
    /// Optional key into <see cref="IPromptRegistry"/>. When provided, the agent
    /// resolves the system prompt from the registry instead of using the default
    /// ReAct preamble; <see cref="PromptVariables"/> is passed through for templating.
    /// </summary>
    public string? PromptTemplateKey { get; init; }

    /// <summary>
    /// Variables passed to the prompt template when <see cref="PromptTemplateKey"/>
    /// is set. Ignored otherwise.
    /// </summary>
    public IReadOnlyDictionary<string, object>? PromptVariables { get; init; }

    /// <summary>
    /// Optional system-prompt addendum appended after the ReAct preamble (or after
    /// the registry-resolved prompt). Useful for per-call persona or constraints.
    /// </summary>
    public string? SystemPromptAddendum { get; init; }

    /// <summary>
    /// Sampling temperature passed to the underlying provider. Defaults to 0.2 —
    /// agents typically want determinism.
    /// </summary>
    public float Temperature { get; init; } = 0.2f;

    /// <summary>Loop bounds (max turns, total-token budget, timeout). See <see cref="AgentLoopOptions"/>.</summary>
    public AgentLoopOptions Options { get; init; } = new();

    /// <summary>Optional tenant id forwarded to the provider for usage tracking.</summary>
    public string? TenantId { get; init; }

    /// <summary>Optional user id forwarded to the provider for usage tracking.</summary>
    public string? UserId { get; init; }
}
