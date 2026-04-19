// -----------------------------------------------------------------------
// <copyright file="ProvisioningResults.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity;

/// <summary>Result of organization identity provisioning.</summary>
public sealed record OrganizationProvisioningResult(
    string ExternalOrganizationId,
    string ExternalProjectId,
    string ClientId,
    string ClientSecret,
    string AdminUserId);

/// <summary>Result of project identity provisioning.</summary>
public sealed record ProjectProvisioningResult(
    string ExternalProjectId);

/// <summary>Result of OIDC application provisioning.</summary>
public sealed record OidcAppProvisioningResult(
    string ClientId,
    string ClientSecret,
    string? ExternalAppId);

/// <summary>Result of OIDC application secret rotation.</summary>
public sealed record OidcAppSecretRotationResult(
    string ClientSecret);
