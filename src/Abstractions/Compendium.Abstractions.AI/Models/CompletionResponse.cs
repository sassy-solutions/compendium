// -----------------------------------------------------------------------
// <copyright file="CompletionResponse.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Models;

/// <summary>
/// Represents a response from an AI completion request.
/// </summary>
public sealed record CompletionResponse
{
    /// <summary>
    /// Gets the unique identifier for this completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the model that generated the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets the generated content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the reason the generation stopped.
    /// </summary>
    public required FinishReason FinishReason { get; init; }

    /// <summary>
    /// Gets the usage statistics for this completion.
    /// </summary>
    public required UsageStats Usage { get; init; }

    /// <summary>
    /// Gets the timestamp when the completion was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets additional provider-specific metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a chunk of a streaming completion.
/// </summary>
public sealed record CompletionChunk
{
    /// <summary>
    /// Gets the completion ID this chunk belongs to.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the content delta for this chunk.
    /// </summary>
    public required string ContentDelta { get; init; }

    /// <summary>
    /// Gets the index of this chunk in the stream.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gets whether this is the final chunk.
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// Gets the finish reason if this is the final chunk.
    /// </summary>
    public FinishReason? FinishReason { get; init; }

    /// <summary>
    /// Gets the usage statistics (only present in final chunk).
    /// </summary>
    public UsageStats? Usage { get; init; }
}

/// <summary>
/// Represents usage statistics for a completion.
/// </summary>
public sealed record UsageStats
{
    /// <summary>
    /// Gets the number of tokens in the prompt.
    /// </summary>
    public required int PromptTokens { get; init; }

    /// <summary>
    /// Gets the number of tokens in the completion.
    /// </summary>
    public required int CompletionTokens { get; init; }

    /// <summary>
    /// Gets the total number of tokens used.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Gets the estimated cost in USD (if available).
    /// </summary>
    public decimal? EstimatedCostUsd { get; init; }
}

/// <summary>
/// Represents the reason a completion finished.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FinishReason
{
    /// <summary>
    /// The model completed naturally.
    /// </summary>
    Stop,

    /// <summary>
    /// The maximum token limit was reached.
    /// </summary>
    Length,

    /// <summary>
    /// Content was filtered for safety.
    /// </summary>
    ContentFilter,

    /// <summary>
    /// A tool call was requested.
    /// </summary>
    ToolCall,

    /// <summary>
    /// Generation is still in progress (streaming).
    /// </summary>
    InProgress,

    /// <summary>
    /// Unknown or provider-specific reason.
    /// </summary>
    Other
}
