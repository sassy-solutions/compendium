// -----------------------------------------------------------------------
// <copyright file="TranslationErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation;

/// <summary>
/// Standardized error definitions for translation operations.
/// </summary>
public static class TranslationErrors
{
    /// <summary>
    /// Gets the error code prefix for translation errors.
    /// </summary>
    public const string Prefix = "Translation";

    /// <summary>
    /// The requested source or target language is not supported by the provider.
    /// </summary>
    public static Error UnsupportedLanguage(string language) =>
        Error.Validation(
            $"{Prefix}.UnsupportedLanguage",
            $"Language '{language}' is not supported by this translation provider.");

    /// <summary>
    /// The input text exceeds the provider's per-request size limit.
    /// </summary>
    public static Error TextTooLong(int length, int maximum) =>
        Error.Validation(
            $"{Prefix}.TextTooLong",
            $"Text length {length} exceeds the maximum of {maximum} characters.");

    /// <summary>
    /// The translation provider could not be reached.
    /// </summary>
    public static Error ProviderUnreachable(string provider, string? reason = null) =>
        Error.Unavailable(
            $"{Prefix}.ProviderUnreachable",
            reason is null
                ? $"Translation provider '{provider}' is unreachable."
                : $"Translation provider '{provider}' is unreachable: {reason}.");

    /// <summary>
    /// The provider rejected the request because the caller exceeded the rate limit.
    /// </summary>
    public static Error RateLimited(TimeSpan? retryAfter = null) =>
        Error.TooManyRequests(
            $"{Prefix}.RateLimited",
            retryAfter.HasValue
                ? $"Translation provider rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Translation provider rate limit exceeded. Please try again later.");
}
