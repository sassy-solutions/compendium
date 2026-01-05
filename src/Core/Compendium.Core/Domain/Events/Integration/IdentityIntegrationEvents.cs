// -----------------------------------------------------------------------
// <copyright file="IdentityIntegrationEvents.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events.Integration;

/// <summary>
/// Integration event raised when a user is created in the identity system.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Username">The user's username.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="IsEmailVerified">Whether the user's email is verified.</param>
public sealed record UserCreatedEvent(
    string UserId,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    bool IsEmailVerified) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.created";
}

/// <summary>
/// Integration event raised when a user is updated in the identity system.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Username">The user's username.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="ChangedFields">The list of fields that were changed.</param>
public sealed record UserUpdatedEvent(
    string UserId,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> ChangedFields) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.updated";
}

/// <summary>
/// Integration event raised when a user is deleted from the identity system.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="DeletedAt">The timestamp when the user was deleted.</param>
/// <param name="IsSoftDelete">Whether this is a soft delete (deactivation) or hard delete.</param>
public sealed record UserDeletedEvent(
    string UserId,
    string Email,
    DateTimeOffset DeletedAt,
    bool IsSoftDelete) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.deleted";
}

/// <summary>
/// Integration event raised when a user's email is verified.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The verified email address.</param>
/// <param name="VerifiedAt">The timestamp when the email was verified.</param>
public sealed record UserEmailVerifiedEvent(
    string UserId,
    string Email,
    DateTimeOffset VerifiedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.email_verified";
}

/// <summary>
/// Integration event raised when a user's email is changed.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="OldEmail">The previous email address.</param>
/// <param name="NewEmail">The new email address.</param>
/// <param name="IsNewEmailVerified">Whether the new email is verified.</param>
public sealed record UserEmailChangedEvent(
    string UserId,
    string OldEmail,
    string NewEmail,
    bool IsNewEmailVerified) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.email_changed";
}

/// <summary>
/// Integration event raised when a user's account is locked.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Reason">The reason for locking the account.</param>
/// <param name="LockedAt">The timestamp when the account was locked.</param>
/// <param name="LockedUntil">The timestamp until which the account is locked, if temporary.</param>
public sealed record UserLockedEvent(
    string UserId,
    string Email,
    string Reason,
    DateTimeOffset LockedAt,
    DateTimeOffset? LockedUntil) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.locked";
}

/// <summary>
/// Integration event raised when a user's account is unlocked.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="UnlockedAt">The timestamp when the account was unlocked.</param>
public sealed record UserUnlockedEvent(
    string UserId,
    string Email,
    DateTimeOffset UnlockedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.unlocked";
}

/// <summary>
/// Integration event raised when a user logs in.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="LoginAt">The timestamp of the login.</param>
/// <param name="IpAddress">The IP address from which the login occurred.</param>
/// <param name="UserAgent">The user agent string of the client.</param>
/// <param name="AuthMethod">The authentication method used.</param>
public sealed record UserLoggedInEvent(
    string UserId,
    string Email,
    DateTimeOffset LoginAt,
    string? IpAddress,
    string? UserAgent,
    string AuthMethod) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.logged_in";
}

/// <summary>
/// Integration event raised when a user logs out.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="LogoutAt">The timestamp of the logout.</param>
/// <param name="SessionId">The session identifier that was terminated.</param>
public sealed record UserLoggedOutEvent(
    string UserId,
    string Email,
    DateTimeOffset LogoutAt,
    string? SessionId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.logged_out";
}

/// <summary>
/// Integration event raised when a user's password is changed.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="ChangedAt">The timestamp when the password was changed.</param>
/// <param name="WasReset">Whether the password was reset (vs. user-initiated change).</param>
public sealed record UserPasswordChangedEvent(
    string UserId,
    string Email,
    DateTimeOffset ChangedAt,
    bool WasReset) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.password_changed";
}

/// <summary>
/// Integration event raised when a role is assigned to a user.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="RoleId">The identifier of the assigned role.</param>
/// <param name="RoleName">The name of the assigned role.</param>
/// <param name="AssignedAt">The timestamp when the role was assigned.</param>
/// <param name="AssignedBy">The identifier of the user who assigned the role.</param>
public sealed record UserRoleAssignedEvent(
    string UserId,
    string Email,
    string RoleId,
    string RoleName,
    DateTimeOffset AssignedAt,
    string? AssignedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.role_assigned";
}

/// <summary>
/// Integration event raised when a role is removed from a user.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="RoleId">The identifier of the removed role.</param>
/// <param name="RoleName">The name of the removed role.</param>
/// <param name="RemovedAt">The timestamp when the role was removed.</param>
/// <param name="RemovedBy">The identifier of the user who removed the role.</param>
public sealed record UserRoleRemovedEvent(
    string UserId,
    string Email,
    string RoleId,
    string RoleName,
    DateTimeOffset RemovedAt,
    string? RemovedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.role_removed";
}

/// <summary>
/// Integration event raised when multi-factor authentication is enabled for a user.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="MfaType">The type of MFA enabled (e.g., TOTP, SMS, WebAuthn).</param>
/// <param name="EnabledAt">The timestamp when MFA was enabled.</param>
public sealed record UserMfaEnabledEvent(
    string UserId,
    string Email,
    string MfaType,
    DateTimeOffset EnabledAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.mfa_enabled";
}

/// <summary>
/// Integration event raised when multi-factor authentication is disabled for a user.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="MfaType">The type of MFA disabled.</param>
/// <param name="DisabledAt">The timestamp when MFA was disabled.</param>
public sealed record UserMfaDisabledEvent(
    string UserId,
    string Email,
    string MfaType,
    DateTimeOffset DisabledAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.user.mfa_disabled";
}

/// <summary>
/// Integration event raised when an organization is created.
/// </summary>
/// <param name="OrganizationId">The unique identifier of the organization.</param>
/// <param name="Name">The name of the organization.</param>
/// <param name="Domain">The domain of the organization.</param>
/// <param name="OwnerId">The identifier of the organization owner.</param>
public sealed record OrganizationCreatedEvent(
    string OrganizationId,
    string Name,
    string? Domain,
    string OwnerId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.organization.created";
}

/// <summary>
/// Integration event raised when an organization is updated.
/// </summary>
/// <param name="OrganizationId">The unique identifier of the organization.</param>
/// <param name="Name">The name of the organization.</param>
/// <param name="Domain">The domain of the organization.</param>
/// <param name="ChangedFields">The list of fields that were changed.</param>
public sealed record OrganizationUpdatedEvent(
    string OrganizationId,
    string Name,
    string? Domain,
    IReadOnlyList<string> ChangedFields) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.organization.updated";
}

/// <summary>
/// Integration event raised when a member is added to an organization.
/// </summary>
/// <param name="OrganizationId">The unique identifier of the organization.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The role assigned to the member.</param>
/// <param name="AddedAt">The timestamp when the member was added.</param>
/// <param name="AddedBy">The identifier of the user who added the member.</param>
public sealed record OrganizationMemberAddedEvent(
    string OrganizationId,
    string UserId,
    string Email,
    string Role,
    DateTimeOffset AddedAt,
    string? AddedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.organization.member_added";
}

/// <summary>
/// Integration event raised when a member is removed from an organization.
/// </summary>
/// <param name="OrganizationId">The unique identifier of the organization.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="RemovedAt">The timestamp when the member was removed.</param>
/// <param name="RemovedBy">The identifier of the user who removed the member.</param>
public sealed record OrganizationMemberRemovedEvent(
    string OrganizationId,
    string UserId,
    string Email,
    DateTimeOffset RemovedAt,
    string? RemovedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "identity.organization.member_removed";
}
