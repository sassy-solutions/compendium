// -----------------------------------------------------------------------
// <copyright file="IProjectIdentityProvisioner.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity;

/// <summary>
/// Provisions identity resources for a project within an organization.
/// Creates an external project and manages OIDC applications.
/// </summary>
public interface IProjectIdentityProvisioner
{
    /// <summary>
    /// Creates an external project within an organization.
    /// </summary>
    /// <param name="request">The project provisioning request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the provisioned project identity details.</returns>
    Task<Result<ProjectProvisioningResult>> ProvisionProjectAsync(
        ProjectProvisioningRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an OIDC application within an external project.
    /// </summary>
    /// <param name="request">The OIDC app provisioning request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created OIDC app details.</returns>
    Task<Result<OidcAppProvisioningResult>> CreateOidcAppAsync(
        OidcAppProvisioningRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an OIDC application's redirect URIs.
    /// </summary>
    /// <param name="request">The OIDC app update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> UpdateOidcAppAsync(
        OidcAppUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an OIDC application.
    /// </summary>
    /// <param name="request">The OIDC app delete request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeleteOidcAppAsync(
        OidcAppDeleteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the client secret of an OIDC application.
    /// </summary>
    /// <param name="request">The OIDC app secret rotation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the new client secret or an error.</returns>
    Task<Result<OidcAppSecretRotationResult>> RotateOidcAppSecretAsync(
        OidcAppSecretRotationRequest request,
        CancellationToken cancellationToken = default);
}
