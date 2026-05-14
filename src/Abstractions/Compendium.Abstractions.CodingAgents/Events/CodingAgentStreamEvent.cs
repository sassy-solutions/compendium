// -----------------------------------------------------------------------
// <copyright file="CodingAgentStreamEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Events;

/// <summary>
/// A neutral, vendor-agnostic event emitted by an <see cref="ICodingAgentRuntime"/>
/// while streaming a run. Adapters translate vendor-specific stream formats
/// (NDJSON, SSE, custom CLI output) into this discriminated union.
/// </summary>
/// <remarks>
/// The base type is <c>abstract</c> with a sealed set of nested derived
/// records so callers can pattern-match exhaustively. A run terminates with
/// exactly one <see cref="Done"/> event (success or failure) — adapters must
/// not emit further events after <see cref="Done"/>.
/// </remarks>
public abstract record CodingAgentStreamEvent
{
    /// <summary>
    /// Gets the timestamp at which the event was produced by the adapter.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// A chunk of textual output (stdout / chat content) produced by the agent.
    /// </summary>
    /// <param name="Text">The output fragment. Adapters should preserve newlines.</param>
    public sealed record Output(string Text) : CodingAgentStreamEvent;

    /// <summary>
    /// The agent invoked a tool / function. Emitted before the tool runs.
    /// </summary>
    /// <param name="ToolName">The vendor-neutral tool name (e.g. <c>"bash"</c>, <c>"read_file"</c>).</param>
    /// <param name="Arguments">Raw JSON-encoded arguments as supplied by the agent.</param>
    /// <param name="CallId">An adapter-assigned identifier used to correlate with <see cref="ToolResult"/>.</param>
    public sealed record ToolCall(string ToolName, string Arguments, string CallId) : CodingAgentStreamEvent;

    /// <summary>
    /// The result of a previously-emitted <see cref="ToolCall"/>.
    /// </summary>
    /// <param name="CallId">Correlates with <see cref="ToolCall.CallId"/>.</param>
    /// <param name="Result">Raw textual output of the tool.</param>
    /// <param name="IsError">True when the tool reported a failure.</param>
    public sealed record ToolResult(string CallId, string Result, bool IsError) : CodingAgentStreamEvent;

    /// <summary>
    /// A non-fatal error reported by the agent or runtime. The stream may
    /// continue after an <see cref="Error"/> event; a fatal failure is signalled
    /// by <see cref="Done"/> with a non-success error.
    /// </summary>
    /// <param name="Message">A human-readable description.</param>
    /// <param name="Code">An optional adapter-specific error code.</param>
    public sealed record Error(string Message, string? Code = null) : CodingAgentStreamEvent;

    /// <summary>
    /// Terminal event for a run. Always exactly one is emitted.
    /// </summary>
    /// <param name="Success">True if the run completed without a fatal error.</param>
    /// <param name="ExitCode">The CLI exit code, when meaningful.</param>
    /// <param name="Summary">Optional final summary or last assistant message.</param>
    public sealed record Done(bool Success, int? ExitCode = null, string? Summary = null) : CodingAgentStreamEvent;
}
