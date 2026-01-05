// -----------------------------------------------------------------------
// <copyright file="CreateOrganizationRequest.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models.Requests;

/// <summary>
/// Represents a request to create a new organization.
/// </summary>
public sealed record CreateOrganizationRequest
{
    /// <summary>
    /// Gets or initializes the name of the organization.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the domain of the organization.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata to associate with the organization.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
