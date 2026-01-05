// -----------------------------------------------------------------------
// <copyright file="License.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Models;

/// <summary>
/// Represents the result of a license validation.
/// </summary>
public sealed record LicenseValidationResult
{
    /// <summary>
    /// Gets or initializes whether the license is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets or initializes the license key.
    /// </summary>
    public required string LicenseKey { get; init; }

    /// <summary>
    /// Gets or initializes the license status.
    /// </summary>
    public required LicenseStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or initializes the license details if valid.
    /// </summary>
    public LicenseDetails? License { get; init; }

    /// <summary>
    /// Gets or initializes the instance details if an instance was specified.
    /// </summary>
    public LicenseInstance? Instance { get; init; }

    /// <summary>
    /// Gets or initializes additional metadata from the validation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Meta { get; init; }
}

/// <summary>
/// Represents license details.
/// </summary>
public sealed record LicenseDetails
{
    /// <summary>
    /// Gets or initializes the unique identifier of the license.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the license key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or initializes the store ID.
    /// </summary>
    public string? StoreId { get; init; }

    /// <summary>
    /// Gets or initializes the order ID.
    /// </summary>
    public string? OrderId { get; init; }

    /// <summary>
    /// Gets or initializes the order item ID.
    /// </summary>
    public string? OrderItemId { get; init; }

    /// <summary>
    /// Gets or initializes the product ID.
    /// </summary>
    public string? ProductId { get; init; }

    /// <summary>
    /// Gets or initializes the customer name.
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets or initializes the customer email.
    /// </summary>
    public string? CustomerEmail { get; init; }

    /// <summary>
    /// Gets or initializes the status of the license.
    /// </summary>
    public required LicenseStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the maximum number of activations allowed.
    /// </summary>
    public int? ActivationLimit { get; init; }

    /// <summary>
    /// Gets or initializes the current number of activations.
    /// </summary>
    public int? ActivationCount { get; init; }

    /// <summary>
    /// Gets or initializes when the license was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the license expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the license has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;
}

/// <summary>
/// Represents a license instance/activation.
/// </summary>
public sealed record LicenseInstance
{
    /// <summary>
    /// Gets or initializes the unique identifier of the instance.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the name of the instance.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes when the instance was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Represents a license activation result.
/// </summary>
public sealed record LicenseActivation
{
    /// <summary>
    /// Gets or initializes whether the activation was successful.
    /// </summary>
    public required bool Activated { get; init; }

    /// <summary>
    /// Gets or initializes the instance details if successful.
    /// </summary>
    public LicenseInstance? Instance { get; init; }

    /// <summary>
    /// Gets or initializes the license details.
    /// </summary>
    public LicenseDetails? License { get; init; }

    /// <summary>
    /// Gets or initializes the error message if activation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or initializes additional metadata from the activation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Meta { get; init; }
}

/// <summary>
/// Defines license status values.
/// </summary>
public enum LicenseStatus
{
    /// <summary>
    /// License is inactive.
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// License is active and valid.
    /// </summary>
    Active = 1,

    /// <summary>
    /// License has expired.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// License has been disabled.
    /// </summary>
    Disabled = 3
}
