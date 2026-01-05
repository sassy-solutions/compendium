// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyLicenseService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Http;
using Compendium.Adapters.LemonSqueezy.Http.Models;

namespace Compendium.Adapters.LemonSqueezy.Services;

/// <summary>
/// Implements license service using LemonSqueezy REST API.
/// </summary>
internal sealed class LemonSqueezyLicenseService : ILicenseService
{
    private readonly LemonSqueezyHttpClient _httpClient;
    private readonly LemonSqueezyOptions _options;
    private readonly ILogger<LemonSqueezyLicenseService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LemonSqueezyLicenseService"/> class.
    /// </summary>
    public LemonSqueezyLicenseService(
        LemonSqueezyHttpClient httpClient,
        IOptions<LemonSqueezyOptions> options,
        ILogger<LemonSqueezyLicenseService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<LicenseValidationResult>> ValidateLicenseAsync(
        string licenseKey,
        string? instanceId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(licenseKey);

        _logger.LogDebug("Validating license key {LicenseKeyShort}", GetKeyShort(licenseKey));

        var request = new LsValidateLicenseRequest
        {
            LicenseKey = licenseKey,
            InstanceId = instanceId
        };

        var result = await _httpClient.ValidateLicenseAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to validate license: {Error}", result.Error.Message);
            return result.Error;
        }

        var response = result.Value;
        var validationResult = MapToValidationResult(response, licenseKey);

        _logger.LogDebug("License validation result: IsValid={IsValid}, Status={Status}",
            validationResult.IsValid, validationResult.Status);

        return validationResult;
    }

    /// <inheritdoc />
    public async Task<Result<LicenseActivation>> ActivateLicenseAsync(
        string licenseKey,
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(licenseKey);
        ArgumentNullException.ThrowIfNull(instanceName);

        _logger.LogInformation("Activating license key {LicenseKeyShort} for instance {InstanceName}",
            GetKeyShort(licenseKey), instanceName);

        var request = new LsActivateLicenseRequest
        {
            LicenseKey = licenseKey,
            InstanceName = instanceName
        };

        var result = await _httpClient.ActivateLicenseAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to activate license: {Error}", result.Error.Message);
            return result.Error;
        }

        var response = result.Value;

        if (response.Activated != true)
        {
            var errorMessage = response.Error ?? "Activation failed";

            if (errorMessage.Contains("activation limit", StringComparison.OrdinalIgnoreCase))
            {
                return BillingErrors.LicenseActivationLimitReached(licenseKey);
            }

            return BillingErrors.InvalidLicense(licenseKey);
        }

        var activation = new LicenseActivation
        {
            Activated = true,
            Instance = response.Instance is not null
                ? new LicenseInstance
                {
                    Id = response.Instance.Id ?? string.Empty,
                    Name = response.Instance.Name ?? instanceName,
                    CreatedAt = response.Instance.CreatedAt ?? DateTimeOffset.UtcNow
                }
                : null,
            License = response.LicenseKey is not null
                ? MapToLicenseDetails(response.LicenseKey, licenseKey)
                : null,
            Meta = response.Meta
        };

        _logger.LogInformation("Activated license key {LicenseKeyShort} with instance ID {InstanceId}",
            GetKeyShort(licenseKey), activation.Instance?.Id);

        return activation;
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateLicenseAsync(
        string licenseKey,
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(licenseKey);
        ArgumentNullException.ThrowIfNull(instanceId);

        _logger.LogInformation("Deactivating license key {LicenseKeyShort} for instance {InstanceId}",
            GetKeyShort(licenseKey), instanceId);

        var request = new LsDeactivateLicenseRequest
        {
            LicenseKey = licenseKey,
            InstanceId = instanceId
        };

        var result = await _httpClient.DeactivateLicenseAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to deactivate license: {Error}", result.Error.Message);
            return result.Error;
        }

        var response = result.Value;

        if (response.Deactivated != true)
        {
            var errorMessage = response.Error ?? "Deactivation failed";

            if (errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return BillingErrors.LicenseInstanceNotFound(instanceId);
            }

            return Error.Failure("Billing.DeactivationFailed", errorMessage);
        }

        _logger.LogInformation("Deactivated license key {LicenseKeyShort} instance {InstanceId}",
            GetKeyShort(licenseKey), instanceId);

        return Result.Success();
    }

    // ============================================================================
    // Mapping Helpers
    // ============================================================================

    private static LicenseValidationResult MapToValidationResult(
        LsValidateLicenseResponse response,
        string licenseKey)
    {
        var isValid = response.Valid == true;
        var licenseData = response.LicenseKey;

        var status = DetermineLicenseStatus(isValid, licenseData);

        return new LicenseValidationResult
        {
            IsValid = isValid,
            LicenseKey = licenseKey,
            Status = status,
            ErrorMessage = response.Error,
            License = licenseData is not null
                ? MapToLicenseDetails(licenseData, licenseKey)
                : null,
            Instance = response.Instance is not null
                ? new LicenseInstance
                {
                    Id = response.Instance.Id ?? string.Empty,
                    Name = response.Instance.Name ?? string.Empty,
                    CreatedAt = response.Instance.CreatedAt ?? DateTimeOffset.MinValue
                }
                : null,
            Meta = response.Meta
        };
    }

    private static LicenseStatus DetermineLicenseStatus(bool isValid, LsLicenseKeyData? licenseData)
    {
        if (!isValid)
        {
            if (licenseData?.ExpiresAt.HasValue == true &&
                licenseData.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                return LicenseStatus.Expired;
            }

            return LicenseStatus.Inactive;
        }

        return licenseData?.Status?.ToLowerInvariant() switch
        {
            "active" => LicenseStatus.Active,
            "inactive" => LicenseStatus.Inactive,
            "expired" => LicenseStatus.Expired,
            "disabled" => LicenseStatus.Disabled,
            _ => LicenseStatus.Active
        };
    }

    private static LicenseDetails MapToLicenseDetails(LsLicenseKeyData licenseData, string licenseKey)
    {
        return new LicenseDetails
        {
            Id = licenseData.Id?.ToString() ?? string.Empty,
            Key = licenseData.Key ?? licenseKey,
            Status = licenseData.Status?.ToLowerInvariant() switch
            {
                "active" => LicenseStatus.Active,
                "inactive" => LicenseStatus.Inactive,
                "expired" => LicenseStatus.Expired,
                "disabled" => LicenseStatus.Disabled,
                _ => LicenseStatus.Active
            },
            ActivationLimit = licenseData.ActivationLimit,
            ActivationCount = licenseData.ActivationUsage,
            CreatedAt = licenseData.CreatedAt ?? DateTimeOffset.MinValue,
            ExpiresAt = licenseData.ExpiresAt
        };
    }

    private static string GetKeyShort(string licenseKey)
    {
        if (string.IsNullOrEmpty(licenseKey) || licenseKey.Length < 8)
        {
            return "****";
        }

        return $"{licenseKey[..4]}...{licenseKey[^4..]}";
    }
}
