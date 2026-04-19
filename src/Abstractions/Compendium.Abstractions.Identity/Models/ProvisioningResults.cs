// -----------------------------------------------------------------------
// <copyright file="ProvisioningResults.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
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
