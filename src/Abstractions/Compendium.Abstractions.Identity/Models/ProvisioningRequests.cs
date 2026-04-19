// -----------------------------------------------------------------------
// <copyright file="ProvisioningRequests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity;

/// <summary>Request to provision identity resources for a new organization.</summary>
public sealed record OrganizationProvisioningRequest(
    string OrganizationId,
    string Name,
    string? DisplayName,
    string PlanId,
    AdminUserProvisioningRequest AdminUser);

/// <summary>Request describing the admin user to create alongside a provisioned organization.</summary>
public sealed record AdminUserProvisioningRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Password,
    string? PreferredLanguage);

/// <summary>Request to provision an external project within an organization.</summary>
public sealed record ProjectProvisioningRequest(
    string ProjectId,
    string OrganizationId,
    string ExternalOrganizationId,
    string ProjectName);

/// <summary>Request to create an OIDC application within an external project.</summary>
public sealed record OidcAppProvisioningRequest(
    string ExternalProjectId,
    string ExternalOrganizationId,
    string AppName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris);

/// <summary>Request to update an OIDC application's redirect URIs.</summary>
public sealed record OidcAppUpdateRequest(
    string ExternalProjectId,
    string ExternalAppId,
    string ExternalOrganizationId,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> PostLogoutRedirectUris);

/// <summary>Request to delete an OIDC application.</summary>
public sealed record OidcAppDeleteRequest(
    string ExternalProjectId,
    string ExternalAppId,
    string ExternalOrganizationId);

/// <summary>Request to rotate an OIDC application's client secret.</summary>
public sealed record OidcAppSecretRotationRequest(
    string ExternalProjectId,
    string ExternalAppId,
    string ExternalOrganizationId);
