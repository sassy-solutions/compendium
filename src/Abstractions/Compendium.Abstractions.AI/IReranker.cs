// -----------------------------------------------------------------------
// <copyright file="IReranker.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Models;

namespace Compendium.Abstractions.AI;

/// <summary>
/// Provides relevance-based reranking of a set of documents against a query.
/// Implementations may delegate to providers such as Cohere Rerank, Voyage,
/// or local cross-encoder models. This port is consumed by RAG pipelines
/// to improve retrieval quality after an initial vector search.
/// </summary>
public interface IReranker
{
    /// <summary>
    /// Reranks the supplied documents against the query and returns them ordered by descending relevance.
    /// </summary>
    /// <param name="query">The query used to score document relevance.</param>
    /// <param name="documents">The candidate documents to rerank.</param>
    /// <param name="opts">Options controlling model selection, top-N truncation, and whether to echo documents back.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the reranked documents ordered by descending relevance, or an error.</returns>
    Task<Result<IReadOnlyList<RerankedDocument>>> RerankAsync(
        string query,
        IReadOnlyList<string> documents,
        RerankOptions opts,
        CancellationToken ct);
}
