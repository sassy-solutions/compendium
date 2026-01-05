// -----------------------------------------------------------------------
// <copyright file="IdentityUser.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models;

/// <summary>
/// Represents a user in the identity system.
/// </summary>
public sealed record IdentityUser
{
    /// <summary>
    /// Gets or initializes the unique identifier of the user.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the email address of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the username of the user.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or initializes the first name of the user.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or initializes the last name of the user.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or initializes the display name of the user.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or initializes the phone number of the user.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets or initializes whether the email has been verified.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// Gets or initializes whether the phone number has been verified.
    /// </summary>
    public bool PhoneVerified { get; init; }

    /// <summary>
    /// Gets or initializes whether the user account is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets or initializes the user's preferred language code.
    /// </summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>
    /// Gets or initializes the user's timezone.
    /// </summary>
    public string? Timezone { get; init; }

    /// <summary>
    /// Gets or initializes the URL to the user's profile picture.
    /// </summary>
    public string? ProfilePictureUrl { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata associated with the user.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the user was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the user was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp of the user's last login.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; init; }

    /// <summary>
    /// Gets or initializes the organization ID the user belongs to (for multi-tenancy).
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets or initializes the roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }
}
