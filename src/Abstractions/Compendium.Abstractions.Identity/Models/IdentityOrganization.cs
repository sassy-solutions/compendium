// -----------------------------------------------------------------------
// <copyright file="IdentityOrganization.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models;

/// <summary>
/// Represents an organization in the identity system for multi-tenancy support.
/// </summary>
public sealed record IdentityOrganization
{
    /// <summary>
    /// Gets or initializes the unique identifier of the organization.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the name of the organization.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the domain of the organization.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Gets or initializes whether the organization is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets or initializes custom metadata associated with the organization.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the organization was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the organization was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Represents a member of an organization.
/// </summary>
public sealed record OrganizationMember
{
    /// <summary>
    /// Gets or initializes the user ID of the member.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets or initializes the email of the member.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the display name of the member.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or initializes the roles assigned to the member within the organization.
    /// </summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>
    /// Gets or initializes when the member joined the organization.
    /// </summary>
    public DateTimeOffset JoinedAt { get; init; }

    /// <summary>
    /// Gets or initializes whether the member is active in the organization.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
