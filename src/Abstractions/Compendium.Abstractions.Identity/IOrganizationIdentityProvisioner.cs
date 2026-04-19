// -----------------------------------------------------------------------
// <copyright file="IOrganizationIdentityProvisioner.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity;

/// <summary>
/// Provisions identity resources (external organization, project, OIDC application, admin user)
/// when a new tenant-level organization is created.
/// </summary>
public interface IOrganizationIdentityProvisioner
{
    /// <summary>
    /// Provisions identity resources for a new organization.
    /// </summary>
    /// <param name="request">The organization provisioning request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the provisioned identity resources or an error.</returns>
    Task<Result<OrganizationProvisioningResult>> ProvisionAsync(
        OrganizationProvisioningRequest request,
        CancellationToken cancellationToken = default);
}
