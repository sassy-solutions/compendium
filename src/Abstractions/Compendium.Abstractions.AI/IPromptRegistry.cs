// -----------------------------------------------------------------------
// <copyright file="IPromptRegistry.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Models;

namespace Compendium.Abstractions.AI;

/// <summary>
/// Provides registration and retrieval of prompt templates.
/// Enables centralized management of meta-prompts, system prompts, and prompt chains.
/// </summary>
public interface IPromptRegistry
{
    /// <summary>
    /// Retrieves a prompt template by its key.
    /// </summary>
    /// <param name="promptKey">The unique key identifying the prompt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the prompt template or an error if not found.</returns>
    Task<Result<PromptTemplate>> GetPromptAsync(
        string promptKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a prompt template by key and renders it with variables.
    /// </summary>
    /// <param name="promptKey">The unique key identifying the prompt.</param>
    /// <param name="variables">Variables to substitute in the template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the rendered prompt or an error.</returns>
    Task<Result<string>> RenderPromptAsync(
        string promptKey,
        IReadOnlyDictionary<string, object> variables,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers or updates a prompt template.
    /// </summary>
    /// <param name="template">The prompt template to register.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> RegisterPromptAsync(
        PromptTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a prompt template.
    /// </summary>
    /// <param name="promptKey">The unique key identifying the prompt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeletePromptAsync(
        string promptKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available prompt templates.
    /// </summary>
    /// <param name="category">Optional category to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of prompts or an error.</returns>
    Task<Result<IReadOnlyList<PromptTemplate>>> ListPromptsAsync(
        string? category = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a prompt template with metadata and variable placeholders.
/// </summary>
public sealed record PromptTemplate
{
    /// <summary>
    /// Gets the unique key identifying this prompt.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the display name of the prompt.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the template content with variable placeholders (e.g., {{variable}}).
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// Gets the optional description of the prompt.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the category for organization (e.g., "customer-support", "content-generation").
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the recommended model for this prompt (optional).
    /// </summary>
    public string? RecommendedModel { get; init; }

    /// <summary>
    /// Gets the recommended temperature for this prompt.
    /// </summary>
    public float? RecommendedTemperature { get; init; }

    /// <summary>
    /// Gets the list of required variable names.
    /// </summary>
    public IReadOnlyList<string>? RequiredVariables { get; init; }

    /// <summary>
    /// Gets the list of optional variable names with default values.
    /// </summary>
    public IReadOnlyDictionary<string, string>? OptionalVariables { get; init; }

    /// <summary>
    /// Gets the version of this prompt template.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets the tenant ID this prompt belongs to (null for global prompts).
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets when the prompt was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets when the prompt was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
