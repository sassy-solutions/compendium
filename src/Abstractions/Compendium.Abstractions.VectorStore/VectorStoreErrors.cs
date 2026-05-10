// -----------------------------------------------------------------------
// <copyright file="VectorStoreErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore;

/// <summary>
/// Provides standardized error definitions for vector store operations.
/// </summary>
public static class VectorStoreErrors
{
    /// <summary>
    /// Gets the error code prefix for vector store errors.
    /// </summary>
    public const string Prefix = "VectorStore";

    /// <summary>
    /// The requested collection does not exist.
    /// </summary>
    public static Error CollectionNotFound(string collection) =>
        Error.NotFound($"{Prefix}.CollectionNotFound", $"Collection '{collection}' was not found.");

    /// <summary>
    /// The supplied embedding dimension does not match the collection configuration.
    /// </summary>
    public static Error DimensionMismatch(int expected, int actual) =>
        Error.Validation(
            $"{Prefix}.DimensionMismatch",
            $"Embedding dimension mismatch: expected {expected}, got {actual}.");

    /// <summary>
    /// No record with the supplied identifier exists in the collection.
    /// </summary>
    public static Error IdNotFound(string id) =>
        Error.NotFound($"{Prefix}.IdNotFound", $"Record with id '{id}' was not found.");

    /// <summary>
    /// The supplied filter could not be translated or is malformed.
    /// </summary>
    public static Error InvalidFilter(string reason) =>
        Error.Validation($"{Prefix}.InvalidFilter", $"Invalid filter: {reason}.");

    /// <summary>
    /// The vector store rejected the request due to throttling/rate limits.
    /// </summary>
    public static Error Throttled(TimeSpan? retryAfter = null) =>
        Error.TooManyRequests(
            $"{Prefix}.Throttled",
            retryAfter.HasValue
                ? $"Vector store throttled the request. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Vector store throttled the request. Please try again later.");
}
