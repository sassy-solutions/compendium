// -----------------------------------------------------------------------
// <copyright file="IAIProvider.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Models;

namespace Compendium.Abstractions.AI;

/// <summary>
/// Provides AI/LLM completion and embedding operations.
/// This interface is provider-agnostic and can be implemented by various AI providers
/// such as OpenRouter, OpenAI, Anthropic, Azure OpenAI, or local models.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Gets the unique identifier of this AI provider.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Generates a completion for the given request.
    /// </summary>
    /// <param name="request">The completion request containing messages and parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the completion response or an error.</returns>
    Task<Result<CompletionResponse>> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming completion for the given request.
    /// </summary>
    /// <param name="request">The completion request containing messages and parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of completion chunks.</returns>
    IAsyncEnumerable<Result<CompletionChunk>> StreamCompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for the given input texts.
    /// </summary>
    /// <param name="request">The embedding request containing texts to embed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the embedding response or an error.</returns>
    Task<Result<EmbeddingResponse>> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available models for this provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of available models or an error.</returns>
    Task<Result<IReadOnlyList<AIModel>>> ListModelsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider is healthy and accessible.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default);
}
