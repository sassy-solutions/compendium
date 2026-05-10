// -----------------------------------------------------------------------
// <copyright file="AnalyticsErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Analytics;

/// <summary>
/// Provides standardized error definitions for product analytics operations.
/// </summary>
public static class AnalyticsErrors
{
    /// <summary>
    /// Gets the error code prefix for analytics errors.
    /// </summary>
    public const string Prefix = "Analytics";

    /// <summary>
    /// The provided event payload failed validation.
    /// </summary>
    /// <param name="reason">A short explanation of why the event was rejected.</param>
    public static Error InvalidEvent(string reason) =>
        Error.Validation($"{Prefix}.InvalidEvent", $"Invalid analytics event: {reason}.");

    /// <summary>
    /// The analytics provider is unreachable or returned a transport-level failure.
    /// </summary>
    /// <param name="provider">The identifier of the analytics provider.</param>
    public static Error ProviderUnreachable(string provider) =>
        Error.Failure(
            $"{Prefix}.ProviderUnreachable",
            $"Analytics provider '{provider}' is unreachable.");

    /// <summary>
    /// The analytics provider returned a rate-limit response.
    /// </summary>
    /// <param name="retryAfter">Optional duration to wait before retrying.</param>
    public static Error RateLimited(TimeSpan? retryAfter = null) =>
        Error.Failure(
            $"{Prefix}.RateLimited",
            retryAfter.HasValue
                ? $"Analytics provider rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Analytics provider rate limit exceeded. Please try again later.");
}
