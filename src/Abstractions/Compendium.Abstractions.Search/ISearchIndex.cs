// -----------------------------------------------------------------------
// <copyright file="ISearchIndex.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Search.Models;

namespace Compendium.Abstractions.Search;

/// <summary>
/// Provides search-engine-agnostic indexing and querying operations.
/// Adapter targets include Meilisearch, Typesense, Elasticsearch, and OpenSearch.
/// </summary>
public interface ISearchIndex
{
    /// <summary>
    /// Ensures the named index exists with the supplied settings.
    /// Implementations should be idempotent: calling twice with the same arguments must succeed.
    /// </summary>
    /// <param name="index">The index name.</param>
    /// <param name="settings">The desired index settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result, or a failure describing why the index could not be created or updated.</returns>
    Task<Result> EnsureIndexAsync(
        string index,
        IndexSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes (creates or updates) a document in the named index.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="index">The index name.</param>
    /// <param name="id">The unique document identifier.</param>
    /// <param name="document">The document to index.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result, or a failure describing the indexing error.</returns>
    Task<Result> IndexAsync<T>(
        string index,
        string id,
        T document,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Deletes a document from the named index.
    /// </summary>
    /// <param name="index">The index name.</param>
    /// <param name="id">The unique document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result, or a failure describing the deletion error.</returns>
    Task<Result> DeleteAsync(
        string index,
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a search query against the named index.
    /// </summary>
    /// <typeparam name="T">The deserialized document type.</typeparam>
    /// <param name="index">The index name.</param>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the search response, or a failure describing the search error.</returns>
    Task<Result<SearchResult<T>>> SearchAsync<T>(
        string index,
        SearchQuery query,
        CancellationToken cancellationToken = default)
        where T : class;
}
