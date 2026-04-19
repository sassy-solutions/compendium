// -----------------------------------------------------------------------
// <copyright file="CreateOrganizationRequest.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
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
