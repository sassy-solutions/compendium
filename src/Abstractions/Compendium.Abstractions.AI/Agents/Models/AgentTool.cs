// -----------------------------------------------------------------------
// <copyright file="AgentTool.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Agents.Models;

/// <summary>
/// Description of a tool that an <see cref="IAgent"/> may invoke. Rendered into the
/// system prompt so the model can decide when to call it.
/// </summary>
/// <param name="Name">Stable identifier (no spaces). Used both in the system prompt and as the dispatch key on <see cref="IAgentToolRegistry.InvokeAsync"/>.</param>
/// <param name="Description">Human-readable description. Should answer "when would the model want this tool?".</param>
/// <param name="InputSchemaJson">Optional JSON Schema describing the arguments object the tool expects. When provided, the agent surfaces it to the model so the action payload is well-formed; when omitted, the model is free to invent the shape (best-effort tools).</param>
public sealed record AgentTool(
    string Name,
    string Description,
    string? InputSchemaJson = null);
