// -----------------------------------------------------------------------
// <copyright file="StorageErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage;

/// <summary>
/// Provides standardized error definitions for object-storage operations.
/// </summary>
public static class StorageErrors
{
    /// <summary>
    /// Gets the error code prefix for storage errors.
    /// </summary>
    public const string Prefix = "Storage";

    /// <summary>
    /// The requested object key was not found in the bucket.
    /// </summary>
    public static Error NotFound(string key) =>
        Error.NotFound($"{Prefix}.NotFound", $"Object '{key}' was not found.");

    /// <summary>
    /// The caller does not have permission to perform the requested operation.
    /// </summary>
    public static Error AccessDenied(string key) =>
        Error.Forbidden($"{Prefix}.AccessDenied", $"Access denied for object '{key}'.");

    /// <summary>
    /// The bucket name is missing, malformed, or does not exist.
    /// </summary>
    public static Error InvalidBucket(string bucket) =>
        Error.Validation($"{Prefix}.InvalidBucket", $"Bucket '{bucket}' is invalid or does not exist.");

    /// <summary>
    /// The provider rejected the request because too many requests have been issued.
    /// </summary>
    public static Error Throttled(TimeSpan? retryAfter = null) =>
        Error.TooManyRequests(
            $"{Prefix}.Throttled",
            retryAfter.HasValue
                ? $"Storage provider throttled the request. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Storage provider throttled the request. Please try again later.");

    /// <summary>
    /// The object payload exceeds the maximum allowed size.
    /// </summary>
    public static Error ContentTooLarge(long size, long maximum) =>
        Error.Validation(
            $"{Prefix}.ContentTooLarge",
            $"Object size {size} bytes exceeds the maximum of {maximum} bytes.");

    /// <summary>
    /// An object with the same key already exists and the operation requires it to be absent.
    /// </summary>
    public static Error ConflictExists(string key) =>
        Error.Conflict($"{Prefix}.ConflictExists", $"Object '{key}' already exists.");
}
