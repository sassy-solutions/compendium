// -----------------------------------------------------------------------
// <copyright file="ClaudeCodeStreamParser.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Compendium.Adapters.ClaudeCode.Runtime;

/// <summary>
/// Translates lines from <c>claude --output-format stream-json</c> into neutral
/// <see cref="CodingAgentStreamEvent"/>s. The Claude CLI emits one JSON object
/// per line with a <c>type</c> discriminator. This parser handles the subset
/// the runtime needs: <c>assistant</c>, <c>user</c> (for tool results),
/// <c>result</c> (terminal), and <c>system</c> banners (skipped). stderr
/// lines are mapped to <see cref="CodingAgentStreamEvent.Error"/> with a
/// well-known failure code when they match known patterns
/// (auth, rate-limit, network).
/// </summary>
internal static class ClaudeCodeStreamParser
{
    internal const string CodeAuth = "auth_failed";
    internal const string CodeRateLimit = "rate_limit";
    internal const string CodeNetwork = "network";
    internal const string CodeStderr = "stderr";
    internal const string CodeBadJson = "bad_json";

    public static CodingAgentStreamEvent? Parse(CliStreamLine line)
    {
        if (string.IsNullOrWhiteSpace(line.Text))
        {
            return null;
        }

        if (line.Stream == CliStream.Stderr)
        {
            return new CodingAgentStreamEvent.Error(line.Text, Code: ClassifyStderr(line.Text));
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(line.Text);
        }
        catch (JsonException)
        {
            // CLI may emit non-JSON banners before stream-json kicks in;
            // surface as Output so callers can inspect, but flag the line.
            return new CodingAgentStreamEvent.Output(line.Text);
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return null;
            }

            var type = typeElement.GetString();
            return type switch
            {
                "assistant" => ParseAssistant(doc.RootElement),
                "user" => ParseUserToolResult(doc.RootElement),
                "result" => ParseResult(doc.RootElement),
                _ => null,
            };
        }
    }

    internal static string ClassifyStderr(string text)
    {
        if (text.Contains("invalid api key", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
        {
            return CodeAuth;
        }

        if (text.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("429", StringComparison.Ordinal))
        {
            return CodeRateLimit;
        }

        if (text.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ENOTFOUND", StringComparison.Ordinal) ||
            text.Contains("ECONNREFUSED", StringComparison.Ordinal))
        {
            return CodeNetwork;
        }

        return CodeStderr;
    }

    private static CodingAgentStreamEvent? ParseAssistant(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var message))
        {
            return null;
        }

        if (!message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var block in content.EnumerateArray())
        {
            if (!block.TryGetProperty("type", out var blockType))
            {
                continue;
            }

            switch (blockType.GetString())
            {
                case "text":
                    if (block.TryGetProperty("text", out var text))
                    {
                        return new CodingAgentStreamEvent.Output(text.GetString() ?? string.Empty);
                    }

                    break;
                case "tool_use":
                    return new CodingAgentStreamEvent.ToolCall(
                        ToolName: block.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
                        Arguments: block.TryGetProperty("input", out var input) ? input.GetRawText() : "{}",
                        CallId: block.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty);
            }
        }

        return null;
    }

    private static CodingAgentStreamEvent? ParseUserToolResult(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var message))
        {
            return null;
        }

        if (!message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var block in content.EnumerateArray())
        {
            if (!block.TryGetProperty("type", out var blockType) ||
                blockType.GetString() != "tool_result")
            {
                continue;
            }

            var callId = block.TryGetProperty("tool_use_id", out var tid) ? tid.GetString() ?? string.Empty : string.Empty;
            var isError = block.TryGetProperty("is_error", out var err) && err.ValueKind == JsonValueKind.True;
            string result = string.Empty;
            if (block.TryGetProperty("content", out var inner))
            {
                result = inner.ValueKind switch
                {
                    JsonValueKind.String => inner.GetString() ?? string.Empty,
                    _ => inner.GetRawText(),
                };
            }

            return new CodingAgentStreamEvent.ToolResult(callId, result, isError);
        }

        return null;
    }

    private static CodingAgentStreamEvent ParseResult(JsonElement root)
    {
        var isError = root.TryGetProperty("is_error", out var err) && err.ValueKind == JsonValueKind.True;
        var subtype = root.TryGetProperty("subtype", out var s) ? s.GetString() : null;
        var summary = root.TryGetProperty("result", out var r) && r.ValueKind == JsonValueKind.String
            ? r.GetString()
            : null;

        // Subtype != "success" means the run halted (max-turns, etc.) — treat as failure.
        var success = !isError && (subtype is null || subtype == "success");
        return new CodingAgentStreamEvent.Done(Success: success, ExitCode: success ? 0 : null, Summary: summary);
    }
}
