// -----------------------------------------------------------------------
// <copyright file="UpdateUserRequest.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models.Requests;

/// <summary>
/// Represents a request to update an existing user.
/// Only non-null properties will be updated.
/// </summary>
public sealed record UpdateUserRequest
{
    /// <summary>
    /// Gets or initializes the new username for the user.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or initializes the new first name for the user.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or initializes the new last name for the user.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or initializes the new display name for the user.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or initializes the new phone number for the user.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets or initializes the new preferred language code for the user.
    /// </summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>
    /// Gets or initializes the new timezone for the user.
    /// </summary>
    public string? Timezone { get; init; }

    /// <summary>
    /// Gets or initializes the new profile picture URL for the user.
    /// </summary>
    public string? ProfilePictureUrl { get; init; }

    /// <summary>
    /// Gets or initializes the new roles to assign to the user.
    /// If provided, replaces all existing roles.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata to merge with existing metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
