// -----------------------------------------------------------------------
// <copyright file="IOrganizationService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity.Models;
using Compendium.Abstractions.Identity.Models.Requests;

namespace Compendium.Abstractions.Identity;

/// <summary>
/// Provides operations for managing organizations in a multi-tenant system.
/// Organizations represent tenants and group users together with shared permissions.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Creates a new organization.
    /// </summary>
    /// <param name="request">The create organization request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created organization or an error.</returns>
    Task<Result<IdentityOrganization>> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an organization by its unique identifier.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the organization or an error if not found.</returns>
    Task<Result<IdentityOrganization>> GetOrganizationAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an organization by name (case-insensitive equals).
    /// </summary>
    /// <param name="name">The organization name to look up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the organization or <see cref="IdentityErrors.OrganizationNotFound"/> if no match.</returns>
    /// <remarks>
    /// Primary use case is recovery from <see cref="ErrorType.Conflict"/> on
    /// <see cref="CreateOrganizationAsync"/>: when the create call fails because the org
    /// already exists, the caller can look up the existing id and reuse it. Identity
    /// providers may legitimately have a single shared organization across many tenants
    /// keyed by name (e.g. a re-run of a saga that previously created the org).
    /// </remarks>
    Task<Result<IdentityOrganization>> GetOrganizationByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user as a member of an organization with specified roles.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to add.</param>
    /// <param name="roles">The roles to assign to the user within the organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> AddMemberAsync(string organizationId, string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> RemoveMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a member's roles within an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roles">The new roles to assign to the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> UpdateMemberRolesAsync(string organizationId, string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all members of an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of organization members or an error.</returns>
    Task<Result<IReadOnlyList<OrganizationMember>>> ListMembersAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an organization, preventing all members from accessing it.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to deactivate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeactivateOrganizationAsync(string organizationId, CancellationToken cancellationToken = default);
}
