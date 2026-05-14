// -----------------------------------------------------------------------
// <copyright file="RealtimeErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime;

/// <summary>
/// Provides standardized error definitions for realtime channel operations.
/// </summary>
public static class RealtimeErrors
{
    /// <summary>
    /// Gets the error code prefix for realtime errors.
    /// </summary>
    public const string Prefix = "Realtime";

    /// <summary>
    /// The subscriber is not authorized for the requested channel
    /// (typically a tenant mismatch against the <c>{tenantId}:{scope}</c> prefix).
    /// </summary>
    public static Error ChannelNotAuthorized(string channel) =>
        Error.Failure(
            $"{Prefix}.ChannelNotAuthorized",
            $"Subscriber is not authorized for channel '{channel}'.");

    /// <summary>
    /// The payload exceeds the provider's per-message size limit.
    /// </summary>
    public static Error MessageTooLarge(int sizeBytes, int maximumBytes) =>
        Error.Validation(
            $"{Prefix}.MessageTooLarge",
            $"Message size {sizeBytes} bytes exceeds maximum of {maximumBytes} bytes.");

    /// <summary>
    /// The realtime provider is unreachable.
    /// </summary>
    public static Error ProviderUnreachable(string provider, string? reason = null) =>
        Error.Failure(
            $"{Prefix}.ProviderUnreachable",
            reason is null
                ? $"Realtime provider '{provider}' is unreachable."
                : $"Realtime provider '{provider}' is unreachable: {reason}");

    /// <summary>
    /// The publish request was rejected for exceeding the rate limit.
    /// </summary>
    public static Error RateLimited(TimeSpan? retryAfter = null) =>
        Error.Failure(
            $"{Prefix}.RateLimited",
            retryAfter.HasValue
                ? $"Rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Rate limit exceeded. Please try again later.");
}
