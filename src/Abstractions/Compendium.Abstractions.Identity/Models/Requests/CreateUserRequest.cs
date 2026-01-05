// -----------------------------------------------------------------------
// <copyright file="CreateUserRequest.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models.Requests;

/// <summary>
/// Represents a request to create a new user.
/// </summary>
public sealed record CreateUserRequest
{
    /// <summary>
    /// Gets or initializes the email address for the new user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the username for the new user.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or initializes the first name of the new user.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or initializes the last name of the new user.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or initializes the display name of the new user.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or initializes the phone number of the new user.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets or initializes the initial password for the new user.
    /// If not provided, the identity provider may send a password reset email.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets or initializes the user's preferred language code.
    /// </summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>
    /// Gets or initializes the user's timezone.
    /// </summary>
    public string? Timezone { get; init; }

    /// <summary>
    /// Gets or initializes the organization ID to associate the user with.
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets or initializes the initial roles to assign to the user.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata to associate with the user.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or initializes whether to send a verification email to the user.
    /// </summary>
    public bool SendVerificationEmail { get; init; } = true;
}
