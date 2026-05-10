// -----------------------------------------------------------------------
// <copyright file="IndexSettings.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Models;

/// <summary>
/// Represents the configuration of a search index.
/// </summary>
public sealed record IndexSettings
{
    /// <summary>
    /// Gets the attributes considered when matching the query text.
    /// </summary>
    public IReadOnlyList<string> SearchableAttributes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the attributes that may appear in <see cref="SearchQuery.Filters"/>.
    /// </summary>
    public IReadOnlyList<string> FilterableAttributes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the attributes that may be used in <see cref="SearchSort"/>.
    /// </summary>
    public IReadOnlyList<string> SortableAttributes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the optional attribute used for distinct/deduplication.
    /// </summary>
    public string? DistinctAttribute { get; init; }

    /// <summary>
    /// Gets the ordered list of ranking rules applied by the engine.
    /// </summary>
    public IReadOnlyList<string> RankingRules { get; init; } = Array.Empty<string>();
}
