// -----------------------------------------------------------------------
// <copyright file="SearchHit.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Models;

/// <summary>
/// Represents a single document returned by a search query.
/// </summary>
/// <typeparam name="T">The deserialized document type.</typeparam>
public sealed record SearchHit<T>
{
    /// <summary>
    /// Gets the matched document.
    /// </summary>
    public required T Document { get; init; }

    /// <summary>
    /// Gets the relevance score assigned by the search engine. Higher is more relevant.
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Gets the highlight fragments per attribute (attribute name → highlighted snippets).
    /// Empty when highlighting is disabled.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Highlights { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
}
