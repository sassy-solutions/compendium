// -----------------------------------------------------------------------
// <copyright file="AIModel.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Models;

/// <summary>
/// Represents an AI model available from a provider.
/// </summary>
public sealed record AIModel
{
    /// <summary>
    /// Gets the unique identifier of the model.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the model.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider of the model (e.g., "anthropic", "openai", "meta").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets the context window size in tokens.
    /// </summary>
    public int? ContextWindow { get; init; }

    /// <summary>
    /// Gets the maximum output tokens.
    /// </summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// Gets whether the model supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; init; } = true;

    /// <summary>
    /// Gets whether the model supports embeddings.
    /// </summary>
    public bool SupportsEmbeddings { get; init; }

    /// <summary>
    /// Gets whether the model supports vision/images.
    /// </summary>
    public bool SupportsVision { get; init; }

    /// <summary>
    /// Gets whether the model supports tool/function calling.
    /// </summary>
    public bool SupportsTools { get; init; }

    /// <summary>
    /// Gets the pricing per million input tokens in USD.
    /// </summary>
    public decimal? PricingInputPerMillion { get; init; }

    /// <summary>
    /// Gets the pricing per million output tokens in USD.
    /// </summary>
    public decimal? PricingOutputPerMillion { get; init; }

    /// <summary>
    /// Gets additional metadata about the model.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
