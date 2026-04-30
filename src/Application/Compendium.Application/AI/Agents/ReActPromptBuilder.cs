// -----------------------------------------------------------------------
// <copyright file="ReActPromptBuilder.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Application.AI.Agents;

/// <summary>
/// Builds the system prompt that turns a vanilla LLM into a tool-using ReAct agent.
/// The model is told to emit an <c>```action</c> JSON block when it wants to call a
/// tool; the agent parses that block, dispatches to the registry, feeds the result
/// back, and loops. Public so callers can build custom agents that share the same
/// grammar.
/// </summary>
public static class ReActPromptBuilder
{
    /// <summary>Marker the model emits to signal a tool call.</summary>
    public const string ActionFenceOpen = "```action";

    /// <summary>Closing fence for the action block.</summary>
    public const string ActionFenceClose = "```";

    /// <summary>
    /// Produces the system prompt for an agent run. When the caller provides a custom
    /// <paramref name="basePrompt"/> it is used verbatim as the starting point; the
    /// tool catalog and action grammar are appended after it. When omitted, a
    /// neutral default is used.
    /// </summary>
    public static string Build(string? basePrompt, IReadOnlyList<AgentTool> tools, string? addendum)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.IsNullOrWhiteSpace(basePrompt)
            ? "You are a helpful AI assistant."
            : basePrompt.Trim());
        sb.AppendLine();

        if (tools.Count > 0)
        {
            sb.AppendLine("You have access to tools. To call a tool, respond with a fenced JSON block tagged `action`:");
            sb.AppendLine();
            sb.AppendLine(ActionFenceOpen);
            sb.AppendLine("{");
            sb.AppendLine("  \"tool\": \"<tool name>\",");
            sb.AppendLine("  \"args\": { /* JSON object matching the tool's input schema */ }");
            sb.AppendLine("}");
            sb.AppendLine(ActionFenceClose);
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("- Emit exactly one action block at a time. Wait for the tool result before continuing.");
            sb.AppendLine("- The block must contain a single, valid JSON object — no comments, no trailing commas.");
            sb.AppendLine("- When you have enough information to answer, omit the action block entirely; that final message becomes the user-visible answer.");
            sb.AppendLine("- If a tool returns an error, decide whether to retry, try a different tool, or surface the error to the user.");
            sb.AppendLine();
            sb.AppendLine("Available tools:");
            foreach (var tool in tools)
            {
                sb.Append("- `").Append(tool.Name).Append("`: ").AppendLine(tool.Description);
                if (!string.IsNullOrWhiteSpace(tool.InputSchemaJson))
                {
                    sb.Append("  Input schema: ").AppendLine(tool.InputSchemaJson);
                }
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(addendum))
        {
            sb.AppendLine(addendum.Trim());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + "\n";
    }
}
