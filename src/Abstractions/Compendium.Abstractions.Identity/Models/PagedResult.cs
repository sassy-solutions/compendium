// -----------------------------------------------------------------------
// <copyright file="PagedResult.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Gets or initializes the items in this page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets or initializes the total count of items across all pages.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets or initializes the current page number (1-based).
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Gets or initializes the page size.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>An empty paged result.</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
    {
        return new PagedResult<T>
        {
            Items = Array.Empty<T>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
    }
}
