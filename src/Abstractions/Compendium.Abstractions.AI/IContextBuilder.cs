// -----------------------------------------------------------------------
// <copyright file="IContextBuilder.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Models;

namespace Compendium.Abstractions.AI;

/// <summary>
/// Builds dynamic context for AI completions based on user, tenant, and request parameters.
/// Enables contextual prompt augmentation with user preferences, history, and domain knowledge.
/// </summary>
public interface IContextBuilder
{
    /// <summary>
    /// Builds a context string based on the provided request.
    /// </summary>
    /// <param name="request">The context building request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the built context or an error.</returns>
    Task<Result<ContextResult>> BuildContextAsync(
        ContextRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a context provider for a specific context type.
    /// </summary>
    /// <param name="contextType">The type of context this provider supplies.</param>
    /// <param name="provider">The context provider function.</param>
    void RegisterProvider(string contextType, IContextProvider provider);
}

/// <summary>
/// Provides context data for a specific context type.
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Gets the type of context this provider supplies.
    /// </summary>
    string ContextType { get; }

    /// <summary>
    /// Gets the priority of this provider (higher = included first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Provides context data based on the request.
    /// </summary>
    /// <param name="request">The context request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the context data or an error.</returns>
    Task<Result<ContextData>> ProvideAsync(
        ContextRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a request for context building.
/// </summary>
public sealed record ContextRequest
{
    /// <summary>
    /// Gets the user ID for personalized context.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the tenant ID for tenant-specific context.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the type of request (e.g., "chat", "analysis", "generation").
    /// </summary>
    public string? RequestType { get; init; }

    /// <summary>
    /// Gets the context types to include (e.g., "user_preferences", "conversation_history").
    /// If null, includes all available contexts.
    /// </summary>
    public IReadOnlyList<string>? IncludeContextTypes { get; init; }

    /// <summary>
    /// Gets the context types to exclude.
    /// </summary>
    public IReadOnlyList<string>? ExcludeContextTypes { get; init; }

    /// <summary>
    /// Gets the maximum tokens for the context.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Gets additional parameters for context providers.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Represents the result of context building.
/// </summary>
public sealed record ContextResult
{
    /// <summary>
    /// Gets the built context string ready for inclusion in a prompt.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Gets the individual context data pieces that were combined.
    /// </summary>
    public required IReadOnlyList<ContextData> ContextPieces { get; init; }

    /// <summary>
    /// Gets the estimated token count of the context.
    /// </summary>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Gets metadata about the context building process.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a piece of context data from a provider.
/// </summary>
public sealed record ContextData
{
    /// <summary>
    /// Gets the type of this context piece.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the content of this context piece.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the priority for ordering (higher = earlier in context).
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Gets the estimated token count.
    /// </summary>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Gets the source of this context (e.g., "database", "cache", "api").
    /// </summary>
    public string? Source { get; init; }
}
