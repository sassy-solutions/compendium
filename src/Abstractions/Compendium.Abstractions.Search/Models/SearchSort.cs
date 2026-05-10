// -----------------------------------------------------------------------
// <copyright file="SearchSort.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Models;

/// <summary>
/// Specifies the direction of a sort operation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    /// <summary>
    /// Ascending sort order (smallest first).
    /// </summary>
    Asc,

    /// <summary>
    /// Descending sort order (largest first).
    /// </summary>
    Desc,
}

/// <summary>
/// Represents a sort instruction for a search query.
/// </summary>
public sealed record SearchSort
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSort"/> class.
    /// </summary>
    /// <param name="field">The name of the field to sort on.</param>
    /// <param name="direction">The direction of the sort. Defaults to <see cref="SortDirection.Asc"/>.</param>
    public SearchSort(string field, SortDirection direction = SortDirection.Asc)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Sort field must be non-empty.", nameof(field));
        }

        this.Field = field;
        this.Direction = direction;
    }

    /// <summary>
    /// Gets the name of the field to sort on.
    /// </summary>
    public string Field { get; init; }

    /// <summary>
    /// Gets the direction of the sort.
    /// </summary>
    public SortDirection Direction { get; init; }

    /// <summary>
    /// Creates an ascending sort on the given field.
    /// </summary>
    /// <param name="field">The field to sort on.</param>
    /// <returns>A new <see cref="SearchSort"/> sorting ascending.</returns>
    public static SearchSort Ascending(string field) => new(field, SortDirection.Asc);

    /// <summary>
    /// Creates a descending sort on the given field.
    /// </summary>
    /// <param name="field">The field to sort on.</param>
    /// <returns>A new <see cref="SearchSort"/> sorting descending.</returns>
    public static SearchSort Descending(string field) => new(field, SortDirection.Desc);
}
