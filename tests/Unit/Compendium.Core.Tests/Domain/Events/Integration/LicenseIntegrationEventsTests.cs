// -----------------------------------------------------------------------
// <copyright file="LicenseIntegrationEventsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.Tests.Domain.Events.Integration;

/// <summary>
/// Unit tests for license integration event records.
/// </summary>
public class LicenseIntegrationEventsTests
{
    [Fact]
    public void LicenseValidatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var validatedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new LicenseValidatedEvent(
            LicenseId: "lic-1",
            LicenseKey: "AAAA-BBBB",
            InstanceId: "inst-1",
            IsValid: true,
            ValidationMessage: "ok",
            ValidatedAt: validatedAt);

        // Assert
        evt.EventType.Should().Be("license.validated");
        evt.LicenseId.Should().Be("lic-1");
        evt.LicenseKey.Should().Be("AAAA-BBBB");
        evt.InstanceId.Should().Be("inst-1");
        evt.IsValid.Should().BeTrue();
        evt.ValidationMessage.Should().Be("ok");
        evt.ValidatedAt.Should().Be(validatedAt);
    }

    [Fact]
    public void LicenseValidatedEvent_WithNullableNulls_AllowsNullValues()
    {
        // Act
        var evt = new LicenseValidatedEvent("lic-1", "K", InstanceId: null, IsValid: false, ValidationMessage: null, ValidatedAt: DateTimeOffset.UtcNow);

        // Assert
        evt.InstanceId.Should().BeNull();
        evt.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public void LicenseExpiredEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var expiredAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new LicenseExpiredEvent("lic-1", "K", "cust-1", "prod-1", expiredAt);

        // Assert
        evt.EventType.Should().Be("license.expired");
        evt.CustomerId.Should().Be("cust-1");
        evt.ProductId.Should().Be("prod-1");
        evt.ExpiredAt.Should().Be(expiredAt);
    }

    [Fact]
    public void LicenseRenewedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var prev = DateTimeOffset.UtcNow;
        var next = prev.AddYears(1);

        // Act
        var evt = new LicenseRenewedEvent("lic-1", "K", "cust-1", prev, next);

        // Assert
        evt.EventType.Should().Be("license.renewed");
        evt.PreviousExpiresAt.Should().Be(prev);
        evt.NewExpiresAt.Should().Be(next);
    }

    [Fact]
    public void LicenseRevokedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var revokedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new LicenseRevokedEvent("lic-1", "K", "cust-1", "Fraud", revokedAt);

        // Assert
        evt.EventType.Should().Be("license.revoked");
        evt.Reason.Should().Be("Fraud");
        evt.RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public void LicenseRevokedEvent_WithNullReason_AllowsNull()
    {
        // Act
        var evt = new LicenseRevokedEvent("lic-1", "K", "cust-1", Reason: null, DateTimeOffset.UtcNow);

        // Assert
        evt.Reason.Should().BeNull();
    }

    [Fact]
    public void LicenseCreatedEvent_WithNullableNulls_AllowsNulls()
    {
        // Act
        var evt = new LicenseCreatedEvent(
            LicenseId: "lic-1",
            LicenseKey: "K",
            CustomerId: "cust-1",
            ProductId: "prod-1",
            Status: "issued",
            ExpiresAt: null,
            ActivationLimit: null);

        // Assert
        evt.EventType.Should().Be("license.created");
        evt.ExpiresAt.Should().BeNull();
        evt.ActivationLimit.Should().BeNull();
    }

    [Fact]
    public void LicenseActivatedEvent_WithNullableNulls_AllowsNulls()
    {
        // Act
        var evt = new LicenseActivatedEvent(
            LicenseId: "lic-1",
            LicenseKey: "K",
            InstanceId: "inst-1",
            InstanceName: null,
            ActivationCount: 1,
            ActivationLimit: null,
            ActivatedAt: DateTimeOffset.UtcNow);

        // Assert
        evt.InstanceName.Should().BeNull();
        evt.ActivationLimit.Should().BeNull();
    }
}
