// -----------------------------------------------------------------------
// <copyright file="SearchQuery.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Models;

/// <summary>
/// Represents a provider-agnostic search query.
/// </summary>
public sealed record SearchQuery
{
    /// <summary>
    /// Gets the free-text query string. May be empty for filter-only queries.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the structured filters applied to the query (attribute → value).
    /// </summary>
    public IReadOnlyDictionary<string, object> Filters { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Gets the attributes to compute facet counts for.
    /// </summary>
    public IReadOnlyList<string> Facets { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the optional sort instruction. When null, the engine's default ranking is used.
    /// </summary>
    public SearchSort? Sort { get; init; }

    /// <summary>
    /// Gets the maximum number of hits to return. Defaults to 20.
    /// </summary>
    public int Limit { get; init; } = 20;

    /// <summary>
    /// Gets the number of hits to skip (for pagination). Defaults to 0.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Gets a value indicating whether highlight fragments should be returned. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Highlight { get; init; }

    /// <summary>
    /// Gets the optional tenant identifier for multi-tenant indexes.
    /// </summary>
    public string? TenantId { get; init; }
}
