// -----------------------------------------------------------------------
// <copyright file="RerankOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Models;

/// <summary>
/// Represents options for a rerank request.
/// </summary>
public sealed record RerankOptions
{
    /// <summary>
    /// Gets the optional model identifier to use for reranking.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the optional maximum number of top documents to return.
    /// </summary>
    public int? TopN { get; init; }

    /// <summary>
    /// Gets a value indicating whether the reranked documents should be returned with their original text.
    /// </summary>
    public bool ReturnDocuments { get; init; }
}
