// -----------------------------------------------------------------------
// <copyright file="IVectorStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.VectorStore.Models;

namespace Compendium.Abstractions.VectorStore;

/// <summary>
/// Provides storage and similarity search over dense embedding vectors.
/// This port is provider-agnostic and can be implemented by adapters such as
/// pgvector, Qdrant, Weaviate, or Pinecone.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Ensures the named collection exists with the supplied dimension and distance metric.
    /// Implementations must be idempotent.
    /// </summary>
    /// <param name="collection">The collection (a.k.a. index/namespace) name.</param>
    /// <param name="dimension">The required embedding dimension.</param>
    /// <param name="metric">The distance metric to use for similarity search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> EnsureCollectionAsync(
        string collection,
        int dimension,
        DistanceMetric metric,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the supplied records in the named collection.
    /// </summary>
    /// <param name="collection">The collection name.</param>
    /// <param name="records">The records to upsert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> UpsertAsync(
        string collection,
        IReadOnlyList<VectorRecord> records,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes records with the supplied identifiers from the named collection.
    /// </summary>
    /// <param name="collection">The collection name.</param>
    /// <param name="ids">The identifiers of records to delete.</param>
    /// <param name="tenantId">Optional tenant scope; when supplied, only records owned by the tenant are deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> DeleteAsync(
        string collection,
        IReadOnlyList<string> ids,
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the named collection for the records whose embeddings are most similar to <paramref name="query"/>.
    /// </summary>
    /// <param name="collection">The collection name.</param>
    /// <param name="query">The query embedding.</param>
    /// <param name="topK">The maximum number of matches to return.</param>
    /// <param name="filter">Optional metadata filter to restrict the candidate set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the matches ordered by similarity score, or an error.</returns>
    Task<Result<IReadOnlyList<VectorMatch>>> SearchAsync(
        string collection,
        ReadOnlyMemory<float> query,
        int topK,
        VectorFilter? filter = null,
        CancellationToken cancellationToken = default);
}
