// -----------------------------------------------------------------------
// <copyright file="CodingAgentRuntimeOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.CodingAgents.Sandbox;

namespace Compendium.Abstractions.CodingAgents.Runtime;

/// <summary>
/// Configures a single coding-agent run: which engine to use, where it runs,
/// what skills/MCP servers are mounted, and how it authenticates.
/// </summary>
/// <remarks>
/// The shape is deliberately neutral: <see cref="Engine"/> identifies the
/// adapter (<c>"claude-code"</c>, <c>"codex"</c>, <c>"cursor"</c>, ...) and
/// <see cref="Parameters"/> is the bag of adapter-specific knobs. Cross-cutting
/// concerns (sandbox, skills, auth) are first-class so the host can apply
/// uniform policy regardless of vendor.
/// </remarks>
public sealed record CodingAgentRuntimeOptions
{
    /// <summary>
    /// Gets the engine identifier this options object targets — for example
    /// <c>"claude-code"</c> or <c>"codex"</c>. Adapters use this to decide
    /// whether they can handle a given run.
    /// </summary>
    public required string Engine { get; init; }

    /// <summary>
    /// Gets the engine version (CLI version, model alias, or release tag).
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets adapter-specific parameters (model name, temperature, max-turns,
    /// CLI flags, ...). Values are passed through as-is by the abstraction layer.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>
    /// Gets the sandbox configuration the agent runs in. Required.
    /// </summary>
    public required SandboxOptions Sandbox { get; init; }

    /// <summary>
    /// Gets the skill / MCP-server identifiers to mount into the agent's
    /// environment. The runtime is responsible for resolving these to concrete
    /// configuration before launching the CLI.
    /// </summary>
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the authentication material for the engine (API keys, OAuth
    /// tokens, ...). Names are adapter-specific (e.g. <c>"ANTHROPIC_API_KEY"</c>).
    /// Values are treated as secrets — the abstraction layer never logs them.
    /// </summary>
    public IReadOnlyDictionary<string, string> Auth { get; init; }
        = new Dictionary<string, string>();
}
