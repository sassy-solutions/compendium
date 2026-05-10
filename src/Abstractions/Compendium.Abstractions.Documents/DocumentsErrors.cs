// -----------------------------------------------------------------------
// <copyright file="DocumentsErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents;

/// <summary>
/// Provides standardized error definitions for document parsing operations.
/// </summary>
public static class DocumentsErrors
{
    /// <summary>
    /// Gets the error code prefix for document errors.
    /// </summary>
    public const string Prefix = "Documents";

    /// <summary>
    /// The supplied MIME type / format is not supported by the parser.
    /// </summary>
    public static Error UnsupportedFormat(string mimeType) =>
        Error.Validation(
            $"{Prefix}.UnsupportedFormat",
            $"Document MIME type '{mimeType}' is not supported.");

    /// <summary>
    /// The document exceeds the provider's size limit.
    /// </summary>
    public static Error DocumentTooLarge(long sizeBytes, long maxBytes) =>
        Error.Validation(
            $"{Prefix}.DocumentTooLarge",
            $"Document size {sizeBytes} bytes exceeds the maximum of {maxBytes} bytes.");

    /// <summary>
    /// The document parsing provider is unreachable (network / DNS / 5xx).
    /// </summary>
    public static Error ProviderUnreachable(string provider, string? reason = null) =>
        Error.Failure(
            $"{Prefix}.ProviderUnreachable",
            reason != null
                ? $"Document parsing provider '{provider}' is unreachable: {reason}."
                : $"Document parsing provider '{provider}' is unreachable.");

    /// <summary>
    /// The provider rate-limited the request.
    /// </summary>
    public static Error RateLimited(TimeSpan? retryAfter = null) =>
        Error.Failure(
            $"{Prefix}.RateLimited",
            retryAfter.HasValue
                ? $"Document parsing rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Document parsing rate limit exceeded. Please try again later.");
}
