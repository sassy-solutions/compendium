// -----------------------------------------------------------------------
// <copyright file="AIErrors.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI;

/// <summary>
/// Provides standardized error definitions for AI operations.
/// </summary>
public static class AIErrors
{
    /// <summary>
    /// Gets the error code prefix for AI errors.
    /// </summary>
    public const string Prefix = "AI";

    /// <summary>
    /// The requested model was not found or is not available.
    /// </summary>
    public static Error ModelNotFound(string model) =>
        Error.NotFound($"{Prefix}.ModelNotFound", $"Model '{model}' was not found or is not available.");

    /// <summary>
    /// The AI provider is not configured or unavailable.
    /// </summary>
    public static Error ProviderUnavailable(string provider) =>
        Error.Failure($"{Prefix}.ProviderUnavailable", $"AI provider '{provider}' is not available.");

    /// <summary>
    /// The request exceeded the rate limit.
    /// </summary>
    public static Error RateLimitExceeded(TimeSpan? retryAfter = null) =>
        Error.Failure(
            $"{Prefix}.RateLimitExceeded",
            retryAfter.HasValue
                ? $"Rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Rate limit exceeded. Please try again later.");

    /// <summary>
    /// The request exceeded the token limit.
    /// </summary>
    public static Error TokenLimitExceeded(int requested, int maximum) =>
        Error.Validation(
            $"{Prefix}.TokenLimitExceeded",
            $"Token limit exceeded. Requested {requested} tokens but maximum is {maximum}.");

    /// <summary>
    /// The content was filtered for safety reasons.
    /// </summary>
    public static Error ContentFiltered(string? reason = null) =>
        Error.Failure(
            $"{Prefix}.ContentFiltered",
            reason ?? "Content was filtered for safety reasons.");

    /// <summary>
    /// The API key is invalid or missing.
    /// </summary>
    public static Error InvalidApiKey() =>
        Error.Failure($"{Prefix}.InvalidApiKey", "The API key is invalid or missing.");

    /// <summary>
    /// Insufficient credits or quota for the request.
    /// </summary>
    public static Error InsufficientCredits() =>
        Error.Failure($"{Prefix}.InsufficientCredits", "Insufficient credits or quota for this request.");

    /// <summary>
    /// The request timed out.
    /// </summary>
    public static Error Timeout(TimeSpan elapsed) =>
        Error.Failure($"{Prefix}.Timeout", $"Request timed out after {elapsed.TotalSeconds} seconds.");

    /// <summary>
    /// The prompt template was not found.
    /// </summary>
    public static Error PromptNotFound(string promptKey) =>
        Error.NotFound($"{Prefix}.PromptNotFound", $"Prompt template '{promptKey}' was not found.");

    /// <summary>
    /// Required variables are missing from the prompt.
    /// </summary>
    public static Error MissingVariables(IEnumerable<string> variables) =>
        Error.Validation(
            $"{Prefix}.MissingVariables",
            $"Missing required variables: {string.Join(", ", variables)}.");

    /// <summary>
    /// The context could not be built.
    /// </summary>
    public static Error ContextBuildFailed(string reason) =>
        Error.Failure($"{Prefix}.ContextBuildFailed", $"Failed to build context: {reason}.");

    /// <summary>
    /// The streaming connection was interrupted.
    /// </summary>
    public static Error StreamInterrupted(string? reason = null) =>
        Error.Failure(
            $"{Prefix}.StreamInterrupted",
            reason ?? "The streaming connection was interrupted.");

    /// <summary>
    /// An unexpected error occurred with the AI provider.
    /// </summary>
    public static Error ProviderError(string message, string? providerErrorCode = null) =>
        Error.Failure(
            $"{Prefix}.ProviderError",
            providerErrorCode != null
                ? $"Provider error [{providerErrorCode}]: {message}"
                : $"Provider error: {message}");

    /// <summary>
    /// The request validation failed.
    /// </summary>
    public static Error InvalidRequest(string reason) =>
        Error.Validation($"{Prefix}.InvalidRequest", reason);
}
