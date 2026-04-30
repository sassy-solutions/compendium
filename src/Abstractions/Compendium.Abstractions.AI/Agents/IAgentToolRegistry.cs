// -----------------------------------------------------------------------
// <copyright file="IAgentToolRegistry.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Abstractions.AI.Agents;

/// <summary>
/// Source of tools an agent can call during its loop. Implementations may aggregate
/// tools from multiple providers (e.g. a Nexus MCP-backed registry, an in-process
/// HTTP-tool registry, etc.).
/// </summary>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Lists every tool currently exposed to agents. Called once per
    /// <see cref="IAgent.RunAsync"/> to assemble the system prompt or tool catalog.
    /// </summary>
    /// <returns>An immutable snapshot of the available tools.</returns>
    IReadOnlyList<AgentTool> Discover();

    /// <summary>
    /// Invokes a tool by name with a JSON-serialised arguments payload.
    /// </summary>
    /// <param name="toolName">The exact tool name, as advertised by <see cref="Discover"/>.</param>
    /// <param name="argumentsJson">A JSON object literal containing the tool inputs. May be empty (<c>{}</c>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> wrapping <see cref="AgentToolResult"/>. Tool-internal failures
    /// (e.g. an HTTP 404 from the underlying API) should be surfaced as a successful result with
    /// <see cref="AgentToolResult.IsError"/> = <c>true</c> so the agent can let the model see the
    /// error and react. A <see cref="Result.Failure"/> here means the registry itself failed
    /// (unknown tool, parser error, …) and should abort the agent run.
    /// </returns>
    Task<Result<AgentToolResult>> InvokeAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default);
}
