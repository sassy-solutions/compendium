// -----------------------------------------------------------------------
// <copyright file="ILicenseService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing.Models;

namespace Compendium.Abstractions.Billing;

/// <summary>
/// Provides operations for license key validation and activation.
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Validates a license key.
    /// </summary>
    /// <param name="licenseKey">The license key to validate.</param>
    /// <param name="instanceId">Optional instance ID for activation-based validation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the validation result or an error.</returns>
    Task<Result<LicenseValidationResult>> ValidateLicenseAsync(string licenseKey, string? instanceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a license key for a specific instance.
    /// </summary>
    /// <param name="licenseKey">The license key to activate.</param>
    /// <param name="instanceName">A name to identify this instance/activation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the activation result or an error.</returns>
    Task<Result<LicenseActivation>> ActivateLicenseAsync(string licenseKey, string instanceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a license key for a specific instance.
    /// </summary>
    /// <param name="licenseKey">The license key to deactivate.</param>
    /// <param name="instanceId">The instance ID to deactivate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeactivateLicenseAsync(string licenseKey, string instanceId, CancellationToken cancellationToken = default);
}
