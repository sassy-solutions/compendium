// -----------------------------------------------------------------------
// <copyright file="ListUsersRequest.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models.Requests;

/// <summary>
/// Represents a request to list users with pagination and filtering.
/// </summary>
public sealed record ListUsersRequest
{
    /// <summary>
    /// Gets or initializes the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets or initializes the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets or initializes the search query to filter users by name or email.
    /// </summary>
    public string? SearchQuery { get; init; }

    /// <summary>
    /// Gets or initializes the organization ID to filter users by.
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets or initializes the role to filter users by.
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Gets or initializes whether to include only active users. Defaults to null (all users).
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Gets or initializes the field to sort by. Defaults to "email".
    /// </summary>
    public string SortBy { get; init; } = "email";

    /// <summary>
    /// Gets or initializes whether to sort in descending order. Defaults to false.
    /// </summary>
    public bool SortDescending { get; init; }
}
