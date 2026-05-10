// -----------------------------------------------------------------------
// <copyright file="RerankedDocument.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Models;

/// <summary>
/// Represents a single reranked document with its relevance score.
/// </summary>
public sealed record RerankedDocument
{
    /// <summary>
    /// Gets the index of the document in the original input list.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the relevance score for this document, where higher values indicate greater relevance.
    /// </summary>
    public required double RelevanceScore { get; init; }

    /// <summary>
    /// Gets the original document text, when requested via <see cref="RerankOptions.ReturnDocuments"/>.
    /// </summary>
    public string? Document { get; init; }
}
