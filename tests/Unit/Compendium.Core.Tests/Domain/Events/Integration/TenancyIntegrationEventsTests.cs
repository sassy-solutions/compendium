// -----------------------------------------------------------------------
// <copyright file="TenancyIntegrationEventsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.Tests.Domain.Events.Integration;

/// <summary>
/// Unit tests for tenancy integration event records.
/// </summary>
public class TenancyIntegrationEventsTests
{
    [Fact]
    public void TenantUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var changed = new[] { "Name", "Identifier" };

        // Act
        var evt = new TenantUpdatedEvent("tenant-1", "Acme", "acme", changed);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.updated");
        evt.Identifier.Should().Be("acme");
        evt.ChangedFields.Should().BeEquivalentTo(changed);
    }

    [Fact]
    public void TenantSuspendedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var suspended = DateTimeOffset.UtcNow;
        var until = suspended.AddDays(30);

        // Act
        var evt = new TenantSuspendedEvent("tenant-1", "Acme", "non-payment", suspended, until);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.suspended");
        evt.Reason.Should().Be("non-payment");
        evt.SuspendedAt.Should().Be(suspended);
        evt.SuspendedUntil.Should().Be(until);
    }

    [Fact]
    public void TenantSuspendedEvent_WithNullSuspendedUntil_AllowsNull()
    {
        // Act
        var evt = new TenantSuspendedEvent("tenant-1", "Acme", "fraud", DateTimeOffset.UtcNow, SuspendedUntil: null);

        // Assert
        evt.SuspendedUntil.Should().BeNull();
    }

    [Fact]
    public void TenantReactivatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var reactivatedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TenantReactivatedEvent("tenant-1", "Acme", reactivatedAt);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.reactivated");
        evt.ReactivatedAt.Should().Be(reactivatedAt);
    }

    [Fact]
    public void TenantDeletedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var deletedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TenantDeletedEvent("tenant-1", "Acme", deletedAt, IsSoftDelete: true);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.deleted");
        evt.DeletedAt.Should().Be(deletedAt);
        evt.IsSoftDelete.Should().BeTrue();
    }

    [Fact]
    public void TenantUserAddedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var addedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TenantUserAddedEvent("tenant-1", "user-1", "u@x.com", "Owner", addedAt, "actor-1");

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.user_added");
        evt.UserId.Should().Be("user-1");
        evt.Role.Should().Be("Owner");
        evt.AddedAt.Should().Be(addedAt);
        evt.AddedBy.Should().Be("actor-1");
    }

    [Fact]
    public void TenantUserRemovedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var removedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TenantUserRemovedEvent("tenant-1", "user-1", "u@x.com", removedAt, "actor-1");

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.user_removed");
        evt.RemovedAt.Should().Be(removedAt);
        evt.RemovedBy.Should().Be("actor-1");
    }

    [Fact]
    public void TenantUserRemovedEvent_WithNullRemovedBy_AllowsNull()
    {
        // Act
        var evt = new TenantUserRemovedEvent("tenant-1", "user-1", "u@x.com", DateTimeOffset.UtcNow, RemovedBy: null);

        // Assert
        evt.RemovedBy.Should().BeNull();
    }

    [Fact]
    public void TenantUserRoleChangedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var changedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TenantUserRoleChangedEvent("tenant-1", "user-1", "u@x.com", "Member", "Owner", changedAt, "actor-1");

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.user_role_changed");
        evt.OldRole.Should().Be("Member");
        evt.NewRole.Should().Be("Owner");
        evt.ChangedAt.Should().Be(changedAt);
        evt.ChangedBy.Should().Be("actor-1");
    }

    [Fact]
    public void TenantSettingsUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            ["theme"] = "dark",
            ["timezone"] = null
        };
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new TenantSettingsUpdatedEvent("tenant-1", "Acme", "general", settings, updatedAt, "actor-1");

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.settings_updated");
        evt.SettingsCategory.Should().Be("general");
        evt.ChangedSettings.Should().BeEquivalentTo(settings);
        evt.UpdatedAt.Should().Be(updatedAt);
        evt.UpdatedBy.Should().Be("actor-1");
    }

    [Fact]
    public void TenantSettingsUpdatedEvent_WithNullUpdatedBy_AllowsNull()
    {
        // Arrange
        var settings = new Dictionary<string, string?> { ["k"] = "v" };

        // Act
        var evt = new TenantSettingsUpdatedEvent(
            "tenant-1",
            "Acme",
            "general",
            settings,
            DateTimeOffset.UtcNow,
            UpdatedBy: null);

        // Assert
        evt.UpdatedBy.Should().BeNull();
    }
}
