// -----------------------------------------------------------------------
// <copyright file="IdentityIntegrationEventsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.Tests.Domain.Events.Integration;

/// <summary>
/// Unit tests for identity integration event records.
/// </summary>
public class IdentityIntegrationEventsTests
{
    [Fact]
    public void UserUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var changed = new[] { "Email", "FirstName" };

        // Act
        var evt = new UserUpdatedEvent("user-1", "u@x.com", "alice", "Alice", "Doe", changed);

        // Assert
        evt.EventType.Should().Be("identity.user.updated");
        evt.UserId.Should().Be("user-1");
        evt.Email.Should().Be("u@x.com");
        evt.Username.Should().Be("alice");
        evt.FirstName.Should().Be("Alice");
        evt.LastName.Should().Be("Doe");
        evt.ChangedFields.Should().BeEquivalentTo(changed);
    }

    [Fact]
    public void UserUpdatedEvent_WithNullableNulls_AllowsNullValues()
    {
        // Act
        var evt = new UserUpdatedEvent("user-1", "u@x.com", null, null, null, Array.Empty<string>());

        // Assert
        evt.Username.Should().BeNull();
        evt.FirstName.Should().BeNull();
        evt.LastName.Should().BeNull();
        evt.ChangedFields.Should().BeEmpty();
    }

    [Fact]
    public void UserDeletedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var deletedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserDeletedEvent("user-1", "u@x.com", deletedAt, IsSoftDelete: true);

        // Assert
        evt.EventType.Should().Be("identity.user.deleted");
        evt.DeletedAt.Should().Be(deletedAt);
        evt.IsSoftDelete.Should().BeTrue();
    }

    [Fact]
    public void UserEmailVerifiedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var verifiedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserEmailVerifiedEvent("user-1", "u@x.com", verifiedAt);

        // Assert
        evt.EventType.Should().Be("identity.user.email_verified");
        evt.VerifiedAt.Should().Be(verifiedAt);
    }

    [Fact]
    public void UserEmailChangedEvent_Constructor_SetsAllProperties()
    {
        // Act
        var evt = new UserEmailChangedEvent("user-1", "old@x.com", "new@x.com", IsNewEmailVerified: false);

        // Assert
        evt.EventType.Should().Be("identity.user.email_changed");
        evt.OldEmail.Should().Be("old@x.com");
        evt.NewEmail.Should().Be("new@x.com");
        evt.IsNewEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void UserLockedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var lockedAt = DateTimeOffset.UtcNow;
        var until = lockedAt.AddDays(1);

        // Act
        var evt = new UserLockedEvent("user-1", "u@x.com", "too-many-failures", lockedAt, until);

        // Assert
        evt.EventType.Should().Be("identity.user.locked");
        evt.Reason.Should().Be("too-many-failures");
        evt.LockedAt.Should().Be(lockedAt);
        evt.LockedUntil.Should().Be(until);
    }

    [Fact]
    public void UserLockedEvent_WithoutLockedUntil_AllowsNull()
    {
        // Act
        var evt = new UserLockedEvent("user-1", "u@x.com", "permanent", DateTimeOffset.UtcNow, LockedUntil: null);

        // Assert
        evt.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void UserUnlockedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var unlocked = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserUnlockedEvent("user-1", "u@x.com", unlocked);

        // Assert
        evt.EventType.Should().Be("identity.user.unlocked");
        evt.UnlockedAt.Should().Be(unlocked);
    }

    [Fact]
    public void UserLoggedOutEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var logoutAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserLoggedOutEvent("user-1", "u@x.com", logoutAt, "session-123");

        // Assert
        evt.EventType.Should().Be("identity.user.logged_out");
        evt.LogoutAt.Should().Be(logoutAt);
        evt.SessionId.Should().Be("session-123");
    }

    [Fact]
    public void UserLoggedOutEvent_WithNullSessionId_AllowsNull()
    {
        // Act
        var evt = new UserLoggedOutEvent("user-1", "u@x.com", DateTimeOffset.UtcNow, SessionId: null);

        // Assert
        evt.SessionId.Should().BeNull();
    }

    [Fact]
    public void UserPasswordChangedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var changedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserPasswordChangedEvent("user-1", "u@x.com", changedAt, WasReset: true);

        // Assert
        evt.EventType.Should().Be("identity.user.password_changed");
        evt.ChangedAt.Should().Be(changedAt);
        evt.WasReset.Should().BeTrue();
    }

    [Fact]
    public void UserRoleRemovedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var removedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserRoleRemovedEvent("user-1", "u@x.com", "role-1", "Admin", removedAt, "actor-1");

        // Assert
        evt.EventType.Should().Be("identity.user.role_removed");
        evt.RoleId.Should().Be("role-1");
        evt.RoleName.Should().Be("Admin");
        evt.RemovedAt.Should().Be(removedAt);
        evt.RemovedBy.Should().Be("actor-1");
    }

    [Fact]
    public void UserMfaEnabledEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var enabledAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserMfaEnabledEvent("user-1", "u@x.com", "TOTP", enabledAt);

        // Assert
        evt.EventType.Should().Be("identity.user.mfa_enabled");
        evt.MfaType.Should().Be("TOTP");
        evt.EnabledAt.Should().Be(enabledAt);
    }

    [Fact]
    public void UserMfaDisabledEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var disabledAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new UserMfaDisabledEvent("user-1", "u@x.com", "WebAuthn", disabledAt);

        // Assert
        evt.EventType.Should().Be("identity.user.mfa_disabled");
        evt.MfaType.Should().Be("WebAuthn");
        evt.DisabledAt.Should().Be(disabledAt);
    }

    [Fact]
    public void OrganizationUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var changed = new List<string> { "Name", "Domain" };

        // Act
        var evt = new OrganizationUpdatedEvent("org-1", "Acme", "acme.com", changed);

        // Assert
        evt.EventType.Should().Be("identity.organization.updated");
        evt.OrganizationId.Should().Be("org-1");
        evt.Name.Should().Be("Acme");
        evt.Domain.Should().Be("acme.com");
        evt.ChangedFields.Should().BeEquivalentTo(changed);
    }

    [Fact]
    public void OrganizationMemberAddedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var added = DateTimeOffset.UtcNow;

        // Act
        var evt = new OrganizationMemberAddedEvent("org-1", "user-1", "u@x.com", "Member", added, "owner-1");

        // Assert
        evt.EventType.Should().Be("identity.organization.member_added");
        evt.OrganizationId.Should().Be("org-1");
        evt.Role.Should().Be("Member");
        evt.AddedAt.Should().Be(added);
        evt.AddedBy.Should().Be("owner-1");
    }

    [Fact]
    public void OrganizationMemberRemovedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var removed = DateTimeOffset.UtcNow;

        // Act
        var evt = new OrganizationMemberRemovedEvent("org-1", "user-1", "u@x.com", removed, "owner-1");

        // Assert
        evt.EventType.Should().Be("identity.organization.member_removed");
        evt.RemovedAt.Should().Be(removed);
        evt.RemovedBy.Should().Be("owner-1");
    }

    [Fact]
    public void OrganizationMemberRemovedEvent_WithNullRemovedBy_AllowsNull()
    {
        // Act
        var evt = new OrganizationMemberRemovedEvent("org-1", "user-1", "u@x.com", DateTimeOffset.UtcNow, RemovedBy: null);

        // Assert
        evt.RemovedBy.Should().BeNull();
    }
}
