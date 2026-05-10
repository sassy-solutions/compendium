// -----------------------------------------------------------------------
// <copyright file="FeatureFlagErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.FeatureFlags;

/// <summary>
/// Provides standardized error definitions for feature flag operations.
/// </summary>
public static class FeatureFlagErrors
{
    /// <summary>
    /// Gets the error code prefix for feature flag errors.
    /// </summary>
    public const string Prefix = "FeatureFlags";

    /// <summary>
    /// The requested flag key was not registered with the provider.
    /// </summary>
    public static Error FlagNotFound(string flagKey) =>
        Error.NotFound($"{Prefix}.FlagNotFound", $"Feature flag '{flagKey}' was not found.");

    /// <summary>
    /// The supplied <see cref="Models.FlagContext"/> did not satisfy provider requirements.
    /// </summary>
    public static Error InvalidContext(string reason) =>
        Error.Validation($"{Prefix}.InvalidContext", $"Invalid flag context: {reason}.");

    /// <summary>
    /// The feature flag provider could not be reached.
    /// </summary>
    public static Error ProviderUnreachable(string provider) =>
        Error.Unavailable(
            $"{Prefix}.ProviderUnreachable",
            $"Feature flag provider '{provider}' is unreachable.");

    /// <summary>
    /// The feature flag provider rejected the request because of rate limiting.
    /// </summary>
    public static Error RateLimited(TimeSpan? retryAfter = null) =>
        Error.TooManyRequests(
            $"{Prefix}.RateLimited",
            retryAfter.HasValue
                ? $"Feature flag provider rate limited the request. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Feature flag provider rate limited the request. Please try again later.");
}
