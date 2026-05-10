// -----------------------------------------------------------------------
// <copyright file="SearchResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Models;

/// <summary>
/// Represents the result of a search query.
/// </summary>
/// <typeparam name="T">The deserialized document type.</typeparam>
public sealed record SearchResult<T>
{
    /// <summary>
    /// Gets the hits returned for the current page.
    /// </summary>
    public IReadOnlyList<SearchHit<T>> Hits { get; init; } = Array.Empty<SearchHit<T>>();

    /// <summary>
    /// Gets the total number of documents matching the query (across all pages).
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// Gets the facet counts (attribute → value → count) when facets were requested.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> FacetCounts { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, long>>();

    /// <summary>
    /// Gets the time the search engine spent processing the query.
    /// </summary>
    public TimeSpan Took { get; init; }
}
