// -----------------------------------------------------------------------
// <copyright file="ReActActionParser.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Compendium.Application.AI.Agents;

/// <summary>
/// Parses an <c>```action ... ```</c> block out of an assistant message. Tolerant of
/// surrounding prose and of the model's preferred whitespace conventions; intolerant
/// of malformed JSON inside the block (we surface those as parse errors rather than
/// guessing). Public so callers can build custom agents that share the same grammar.
/// </summary>
public static class ReActActionParser
{
    private const string Fence = "```";
    private const string ActionTag = "action";

    /// <summary>Extracted action: the tool to call and its raw JSON arguments.</summary>
    /// <param name="ToolName">The <c>tool</c> property of the action object.</param>
    /// <param name="ArgumentsJson">The <c>args</c> property serialised back to JSON. Empty <c>{}</c> when absent.</param>
    public sealed record ParsedAction(string ToolName, string ArgumentsJson);

    /// <summary>
    /// Tries to extract an action from <paramref name="content"/>. Returns
    /// <see langword="null"/> when no action block is present (the model's response is
    /// the final answer).
    /// </summary>
    /// <param name="content">The raw assistant message content.</param>
    /// <param name="action">The parsed action when the call returns <see langword="true"/>.</param>
    /// <param name="parseError">A user-facing description of the parse failure when the call returns <see langword="false"/> for a malformed block.</param>
    /// <returns>
    /// <see langword="true"/> when an action was successfully parsed; <see langword="false"/> when the block is present but malformed (caller should surface <paramref name="parseError"/> to the model so it can retry); <see langword="null"/> via the <paramref name="action"/> being <see langword="null"/> when there's no block at all.
    /// </returns>
    public static bool TryParse(string content, out ParsedAction? action, out string? parseError)
    {
        action = null;
        parseError = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // Find the fence that opens with `action` (after the triple-backtick).
        // We look for "```action" optionally followed by a newline.
        var span = content.AsSpan();
        var fenceIndex = -1;
        for (var i = 0; i <= span.Length - (Fence.Length + ActionTag.Length); i++)
        {
            if (!span.Slice(i, Fence.Length).SequenceEqual(Fence)) continue;
            var rest = span[(i + Fence.Length)..];
            // Skip optional whitespace/CR before the tag.
            var afterFence = SkipInlineWhitespace(rest);
            if (afterFence.StartsWith(ActionTag, StringComparison.OrdinalIgnoreCase))
            {
                // Make sure the tag is followed by a non-letter (so "actionable" doesn't match).
                if (afterFence.Length == ActionTag.Length || !char.IsLetterOrDigit(afterFence[ActionTag.Length]))
                {
                    fenceIndex = i;
                    break;
                }
            }
        }

        if (fenceIndex < 0)
        {
            return false;
        }

        // Locate the body: from after the opening fence + tag, up to the next ```.
        var bodyStart = content.IndexOf('\n', fenceIndex);
        if (bodyStart < 0)
        {
            parseError = "Action block is not terminated with a newline after the opening fence.";
            return false;
        }
        bodyStart += 1;

        var closeIndex = content.IndexOf(Fence, bodyStart, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            parseError = "Action block is not closed with a matching ``` fence.";
            return false;
        }

        var body = content.Substring(bodyStart, closeIndex - bodyStart).Trim();

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                parseError = "Action block must contain a JSON object.";
                return false;
            }

            if (!doc.RootElement.TryGetProperty("tool", out var toolEl) || toolEl.ValueKind != JsonValueKind.String)
            {
                parseError = "Action object must include a string `tool` property.";
                return false;
            }

            var toolName = toolEl.GetString();
            if (string.IsNullOrWhiteSpace(toolName))
            {
                parseError = "Action `tool` must be a non-empty string.";
                return false;
            }

            string argsJson = "{}";
            if (doc.RootElement.TryGetProperty("args", out var argsEl))
            {
                if (argsEl.ValueKind == JsonValueKind.Null || argsEl.ValueKind == JsonValueKind.Undefined)
                {
                    argsJson = "{}";
                }
                else if (argsEl.ValueKind == JsonValueKind.Object)
                {
                    argsJson = argsEl.GetRawText();
                }
                else
                {
                    parseError = "Action `args` must be a JSON object when present.";
                    return false;
                }
            }

            action = new ParsedAction(toolName!, argsJson);
            return true;
        }
        catch (JsonException ex)
        {
            parseError = $"Action block JSON is malformed: {ex.Message}";
            return false;
        }
    }

    private static ReadOnlySpan<char> SkipInlineWhitespace(ReadOnlySpan<char> s)
    {
        var i = 0;
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
        return s[i..];
    }
}
