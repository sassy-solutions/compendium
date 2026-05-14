// -----------------------------------------------------------------------
// <copyright file="ClaudeCodeRuntimeDefaults.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.ClaudeCode.Runtime;

/// <summary>
/// Constants used by the Claude Code adapter: the engine identifier exposed
/// via <see cref="ICodingAgentRuntime.Engine"/>, the default executable name,
/// and the recognised parameter keys read from
/// <see cref="CodingAgentRuntimeOptions.Parameters"/>.
/// </summary>
public static class ClaudeCodeRuntimeDefaults
{
    /// <summary>The engine identifier this adapter handles.</summary>
    public const string EngineId = "claude-code";

    /// <summary>The default executable name, resolved via <c>PATH</c>.</summary>
    public const string DefaultExecutable = "claude";

    /// <summary>Parameter key: override the executable path or name.</summary>
    public const string ParamExecutable = "executable";

    /// <summary>Parameter key: model alias (e.g. <c>sonnet</c>, <c>opus</c>).</summary>
    public const string ParamModel = "model";

    /// <summary>Parameter key: max conversation turns before the run halts.</summary>
    public const string ParamMaxTurns = "max_turns";

    /// <summary>Parameter key: comma-separated tools to allow (e.g. <c>"Bash,Read"</c>).</summary>
    public const string ParamAllowedTools = "allowed_tools";

    /// <summary>Parameter key: comma-separated tools to deny.</summary>
    public const string ParamDisallowedTools = "disallowed_tools";

    /// <summary>Parameter key: path to an MCP server configuration JSON file.</summary>
    public const string ParamMcpConfig = "mcp_config";

    /// <summary>Parameter key: permission mode (e.g. <c>acceptEdits</c>, <c>bypassPermissions</c>).</summary>
    public const string ParamPermissionMode = "permission_mode";

    /// <summary>Auth key: the Anthropic API key forwarded as <c>ANTHROPIC_API_KEY</c>.</summary>
    public const string AuthAnthropicApiKey = "ANTHROPIC_API_KEY";
}
