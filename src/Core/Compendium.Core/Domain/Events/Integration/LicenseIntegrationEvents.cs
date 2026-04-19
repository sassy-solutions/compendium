// -----------------------------------------------------------------------
// <copyright file="LicenseIntegrationEvents.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events.Integration;

/// <summary>
/// Integration event raised when a license is created.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Status">The license status.</param>
/// <param name="ExpiresAt">The expiration date of the license, if applicable.</param>
/// <param name="ActivationLimit">The maximum number of activations allowed.</param>
public sealed record LicenseCreatedEvent(
    string LicenseId,
    string LicenseKey,
    string CustomerId,
    string ProductId,
    string Status,
    DateTimeOffset? ExpiresAt,
    int? ActivationLimit) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.created";
}

/// <summary>
/// Integration event raised when a license is activated.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="InstanceId">The instance identifier for this activation.</param>
/// <param name="InstanceName">The name of the activated instance.</param>
/// <param name="ActivationCount">The current number of activations.</param>
/// <param name="ActivationLimit">The maximum number of activations allowed.</param>
/// <param name="ActivatedAt">The timestamp when the license was activated.</param>
public sealed record LicenseActivatedEvent(
    string LicenseId,
    string LicenseKey,
    string InstanceId,
    string? InstanceName,
    int ActivationCount,
    int? ActivationLimit,
    DateTimeOffset ActivatedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.activated";
}

/// <summary>
/// Integration event raised when a license is deactivated.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="InstanceId">The instance identifier that was deactivated.</param>
/// <param name="ActivationCount">The current number of remaining activations.</param>
/// <param name="DeactivatedAt">The timestamp when the license was deactivated.</param>
public sealed record LicenseDeactivatedEvent(
    string LicenseId,
    string LicenseKey,
    string InstanceId,
    int ActivationCount,
    DateTimeOffset DeactivatedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.deactivated";
}

/// <summary>
/// Integration event raised when a license is validated.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="InstanceId">The instance identifier that validated, if applicable.</param>
/// <param name="IsValid">Whether the license is valid.</param>
/// <param name="ValidationMessage">The validation message.</param>
/// <param name="ValidatedAt">The timestamp when the license was validated.</param>
public sealed record LicenseValidatedEvent(
    string LicenseId,
    string LicenseKey,
    string? InstanceId,
    bool IsValid,
    string? ValidationMessage,
    DateTimeOffset ValidatedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.validated";
}

/// <summary>
/// Integration event raised when a license expires.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="ProductId">The product identifier.</param>
/// <param name="ExpiredAt">The timestamp when the license expired.</param>
public sealed record LicenseExpiredEvent(
    string LicenseId,
    string LicenseKey,
    string CustomerId,
    string ProductId,
    DateTimeOffset ExpiredAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.expired";
}

/// <summary>
/// Integration event raised when a license is renewed.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="PreviousExpiresAt">The previous expiration date.</param>
/// <param name="NewExpiresAt">The new expiration date.</param>
public sealed record LicenseRenewedEvent(
    string LicenseId,
    string LicenseKey,
    string CustomerId,
    DateTimeOffset PreviousExpiresAt,
    DateTimeOffset NewExpiresAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.renewed";
}

/// <summary>
/// Integration event raised when a license is revoked.
/// </summary>
/// <param name="LicenseId">The unique identifier of the license.</param>
/// <param name="LicenseKey">The license key.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="Reason">The reason for revocation.</param>
/// <param name="RevokedAt">The timestamp when the license was revoked.</param>
public sealed record LicenseRevokedEvent(
    string LicenseId,
    string LicenseKey,
    string CustomerId,
    string? Reason,
    DateTimeOffset RevokedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "license.revoked";
}
