// -----------------------------------------------------------------------
// <copyright file="CompletionRequest.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Models;

/// <summary>
/// Represents a request for AI completion.
/// </summary>
public sealed record CompletionRequest
{
    /// <summary>
    /// Gets the model identifier to use for completion.
    /// Examples: "anthropic/claude-3.5-sonnet", "openai/gpt-4o", "meta-llama/llama-3.1-70b-instruct".
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets the messages to send to the model.
    /// </summary>
    public required IReadOnlyList<Message> Messages { get; init; }

    /// <summary>
    /// Gets the optional system prompt to prepend.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Gets the temperature for randomness (0.0 to 2.0). Default is 0.7.
    /// </summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>
    /// Gets the maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Gets the top-p (nucleus) sampling value.
    /// </summary>
    public float? TopP { get; init; }

    /// <summary>
    /// Gets the frequency penalty (-2.0 to 2.0).
    /// </summary>
    public float? FrequencyPenalty { get; init; }

    /// <summary>
    /// Gets the presence penalty (-2.0 to 2.0).
    /// </summary>
    public float? PresencePenalty { get; init; }

    /// <summary>
    /// Gets the stop sequences that will halt generation.
    /// </summary>
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>
    /// Gets additional provider-specific parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object>? AdditionalParameters { get; init; }

    /// <summary>
    /// Gets the optional request ID for tracking and idempotency.
    /// </summary>
    public string? RequestId { get; init; }

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
/// Represents a message in a conversation.
/// </summary>
public sealed record Message
{
    /// <summary>
    /// Gets the role of the message sender.
    /// </summary>
    public required MessageRole Role { get; init; }

    /// <summary>
    /// Gets the content of the message.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the optional name of the participant.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Creates a system message.
    /// </summary>
    public static Message System(string content) => new() { Role = MessageRole.System, Content = content };

    /// <summary>
    /// Creates a user message.
    /// </summary>
    public static Message User(string content) => new() { Role = MessageRole.User, Content = content };

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    public static Message Assistant(string content) => new() { Role = MessageRole.Assistant, Content = content };
}

/// <summary>
/// Represents the role of a message sender.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageRole
{
    /// <summary>
    /// System message providing instructions to the model.
    /// </summary>
    System,

    /// <summary>
    /// User message from the human.
    /// </summary>
    User,

    /// <summary>
    /// Assistant message from the AI.
    /// </summary>
    Assistant,

    /// <summary>
    /// Tool/function result message.
    /// </summary>
    Tool
}
