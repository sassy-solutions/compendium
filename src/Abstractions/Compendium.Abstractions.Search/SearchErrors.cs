// -----------------------------------------------------------------------
// <copyright file="SearchErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search;

/// <summary>
/// Provides standardized error definitions for search operations.
/// </summary>
public static class SearchErrors
{
    /// <summary>
    /// Gets the error code prefix for search errors.
    /// </summary>
    public const string Prefix = "Search";

    /// <summary>
    /// The requested index does not exist.
    /// </summary>
    /// <param name="index">The name of the missing index.</param>
    /// <returns>A not-found error.</returns>
    public static Error IndexNotFound(string index) =>
        Error.NotFound($"{Prefix}.IndexNotFound", $"Search index '{index}' was not found.");

    /// <summary>
    /// The query is malformed or otherwise invalid for the engine.
    /// </summary>
    /// <param name="reason">A human-readable reason describing why the query is invalid.</param>
    /// <returns>A validation error.</returns>
    public static Error InvalidQuery(string reason) =>
        Error.Validation($"{Prefix}.InvalidQuery", $"Invalid search query: {reason}.");

    /// <summary>
    /// A filter referenced an attribute that is not declared as filterable on the index.
    /// </summary>
    /// <param name="attribute">The attribute that was used as a filter.</param>
    /// <returns>A validation error.</returns>
    public static Error AttributeNotFilterable(string attribute) =>
        Error.Validation(
            $"{Prefix}.AttributeNotFilterable",
            $"Attribute '{attribute}' is not configured as filterable on this index.");

    /// <summary>
    /// A sort referenced an attribute that is not declared as sortable on the index.
    /// </summary>
    /// <param name="attribute">The attribute that was used to sort.</param>
    /// <returns>A validation error.</returns>
    public static Error AttributeNotSortable(string attribute) =>
        Error.Validation(
            $"{Prefix}.AttributeNotSortable",
            $"Attribute '{attribute}' is not configured as sortable on this index.");

    /// <summary>
    /// The request was rejected because the engine is rate-limiting the caller.
    /// </summary>
    /// <param name="retryAfter">Optional duration after which the caller may retry.</param>
    /// <returns>A too-many-requests error.</returns>
    public static Error Throttled(TimeSpan? retryAfter = null) =>
        Error.TooManyRequests(
            $"{Prefix}.Throttled",
            retryAfter.HasValue
                ? $"Search engine throttled the request. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Search engine throttled the request. Please try again later.");
}
