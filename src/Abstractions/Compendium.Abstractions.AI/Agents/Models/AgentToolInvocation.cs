// -----------------------------------------------------------------------
// <copyright file="AgentToolInvocation.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Agents.Models;

/// <summary>
/// Audit record for a single tool call performed during one <see cref="AgentTurn"/>.
/// </summary>
/// <param name="ToolName">The tool that was invoked.</param>
/// <param name="ArgumentsJson">The JSON arguments object the agent extracted from the model's action block.</param>
/// <param name="ResultText">The text the registry returned (or the error message when <paramref name="IsError"/> is true).</param>
/// <param name="IsError">Whether the tool itself reported a failure. Errors are fed back into the loop so the model can react; they do not abort the run.</param>
/// <param name="Latency">Wall-clock latency of the tool call.</param>
public sealed record AgentToolInvocation(
    string ToolName,
    string ArgumentsJson,
    string ResultText,
    bool IsError,
    TimeSpan Latency);

/// <summary>
/// Output of <see cref="IAgentToolRegistry.InvokeAsync"/>.
/// </summary>
/// <param name="Content">Text the agent will feed back to the model (verbatim).</param>
/// <param name="IsError">Whether to mark the result as an error in the audit trail and in the prompt fed to the model.</param>
public sealed record AgentToolResult(
    string Content,
    bool IsError = false);
