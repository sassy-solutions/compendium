// -----------------------------------------------------------------------
// <copyright file="ICodingAgentRuntime.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.CodingAgents.Events;

namespace Compendium.Abstractions.CodingAgents.Runtime;

/// <summary>
/// Provides streaming execution of a coding agent. Implementations wrap a
/// concrete engine (Claude Code, Codex, Cursor CLI, ...) and translate its
/// output into the neutral <see cref="CodingAgentStreamEvent"/> stream.
/// </summary>
/// <remarks>
/// The runtime owns the agent's lifecycle for the duration of the call:
/// it provisions the sandbox, spawns the CLI, parses output, and tears the
/// sandbox down before the enumeration completes. Cancellation must abort the
/// underlying process and dispose the sandbox.
/// </remarks>
public interface ICodingAgentRuntime
{
    /// <summary>
    /// Gets the engine identifier this runtime handles (e.g.
    /// <c>"claude-code"</c>). Used to dispatch a run to the right adapter.
    /// </summary>
    string Engine { get; }

    /// <summary>
    /// Runs a coding agent and yields a neutral event stream. Exactly one
    /// <see cref="CodingAgentStreamEvent.Done"/> event is produced as the
    /// terminal item.
    /// </summary>
    /// <param name="options">The runtime configuration (engine, sandbox, skills, auth).</param>
    /// <param name="request">The per-call input (prompt, session, metadata).</param>
    /// <param name="cancellationToken">A cancellation token. Cancellation aborts the agent and tears down the sandbox.</param>
    /// <returns>An async stream of <see cref="CodingAgentStreamEvent"/>.</returns>
    IAsyncEnumerable<CodingAgentStreamEvent> RunAsync(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request,
        CancellationToken cancellationToken = default);
}
