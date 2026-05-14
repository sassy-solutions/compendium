// -----------------------------------------------------------------------
// <copyright file="CodingAgentRunRequest.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Runtime;

/// <summary>
/// The per-invocation input to <see cref="ICodingAgentRuntime.RunAsync"/>:
/// the prompt to give the agent and any optional metadata for tracing,
/// resumption, and tenant scoping.
/// </summary>
public sealed record CodingAgentRunRequest
{
    /// <summary>
    /// Gets the natural-language prompt the agent should act on.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets an optional system prompt / instruction prepended to the run.
    /// Adapters that have a native concept of system prompts (most do) should
    /// honour this; others may concatenate.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Gets an optional session identifier used to resume a prior run when the
    /// engine supports it. Adapters should ignore values they cannot honour.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets an optional correlation identifier propagated through traces and
    /// audit logs.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Gets the tenant identifier the run is performed on behalf of.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the user identifier the run is performed on behalf of.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets free-form metadata attached to the run for observability. Values
    /// must be JSON-serialisable.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}
