// -----------------------------------------------------------------------
// <copyright file="EmbeddingModels.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Models;

/// <summary>
/// Represents a request for text embeddings.
/// </summary>
public sealed record EmbeddingRequest
{
    /// <summary>
    /// Gets the model identifier to use for embeddings.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets the texts to embed.
    /// </summary>
    public required IReadOnlyList<string> Inputs { get; init; }

    /// <summary>
    /// Gets the optional dimensions for the embeddings (if supported by model).
    /// </summary>
    public int? Dimensions { get; init; }

    /// <summary>
    /// Gets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the user ID for usage tracking.
    /// </summary>
    public string? UserId { get; init; }
}

/// <summary>
/// Represents a response from an embedding request.
/// </summary>
public sealed record EmbeddingResponse
{
    /// <summary>
    /// Gets the model that generated the embeddings.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets the generated embeddings.
    /// </summary>
    public required IReadOnlyList<Embedding> Embeddings { get; init; }

    /// <summary>
    /// Gets the usage statistics for this request.
    /// </summary>
    public required EmbeddingUsage Usage { get; init; }
}

/// <summary>
/// Represents a single embedding vector.
/// </summary>
public sealed record Embedding
{
    /// <summary>
    /// Gets the index of the input this embedding corresponds to.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the embedding vector.
    /// </summary>
    public required float[] Vector { get; init; }
}

/// <summary>
/// Represents usage statistics for an embedding request.
/// </summary>
public sealed record EmbeddingUsage
{
    /// <summary>
    /// Gets the number of tokens in the input.
    /// </summary>
    public required int PromptTokens { get; init; }

    /// <summary>
    /// Gets the total number of tokens used.
    /// </summary>
    public int TotalTokens => PromptTokens;

    /// <summary>
    /// Gets the estimated cost in USD (if available).
    /// </summary>
    public decimal? EstimatedCostUsd { get; init; }
}
