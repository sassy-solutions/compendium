// -----------------------------------------------------------------------
// <copyright file="TenancyIntegrationEvents.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events.Integration;

/// <summary>
/// Integration event raised when a tenant is created.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="Identifier">The unique identifier/slug for the tenant.</param>
/// <param name="OwnerId">The identifier of the tenant owner.</param>
/// <param name="Plan">The subscription plan of the tenant.</param>
/// <param name="IsActive">Whether the tenant is active.</param>
public sealed record TenantCreatedEvent(
    string TenantId,
    string Name,
    string Identifier,
    string? OwnerId,
    string? Plan,
    bool IsActive) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.created";
}

/// <summary>
/// Integration event raised when a tenant is updated.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="Identifier">The unique identifier/slug for the tenant.</param>
/// <param name="ChangedFields">The list of fields that were changed.</param>
public sealed record TenantUpdatedEvent(
    string TenantId,
    string Name,
    string Identifier,
    IReadOnlyList<string> ChangedFields) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.updated";
}

/// <summary>
/// Integration event raised when a tenant is suspended.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="Reason">The reason for suspension.</param>
/// <param name="SuspendedAt">The timestamp when the tenant was suspended.</param>
/// <param name="SuspendedUntil">The timestamp until which the tenant is suspended, if temporary.</param>
public sealed record TenantSuspendedEvent(
    string TenantId,
    string Name,
    string Reason,
    DateTimeOffset SuspendedAt,
    DateTimeOffset? SuspendedUntil) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.suspended";
}

/// <summary>
/// Integration event raised when a tenant is reactivated.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="ReactivatedAt">The timestamp when the tenant was reactivated.</param>
public sealed record TenantReactivatedEvent(
    string TenantId,
    string Name,
    DateTimeOffset ReactivatedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.reactivated";
}

/// <summary>
/// Integration event raised when a tenant is deleted.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="DeletedAt">The timestamp when the tenant was deleted.</param>
/// <param name="IsSoftDelete">Whether this is a soft delete or hard delete.</param>
public sealed record TenantDeletedEvent(
    string TenantId,
    string Name,
    DateTimeOffset DeletedAt,
    bool IsSoftDelete) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.deleted";
}

/// <summary>
/// Integration event raised when a tenant's plan is changed.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="OldPlan">The previous plan.</param>
/// <param name="NewPlan">The new plan.</param>
/// <param name="ChangeType">The type of change (upgrade, downgrade, renewal).</param>
/// <param name="ChangedAt">The timestamp when the plan was changed.</param>
public sealed record TenantPlanChangedEvent(
    string TenantId,
    string Name,
    string? OldPlan,
    string NewPlan,
    string ChangeType,
    DateTimeOffset ChangedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.plan_changed";
}

/// <summary>
/// Integration event raised when a user is added to a tenant.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The role assigned to the user within the tenant.</param>
/// <param name="AddedAt">The timestamp when the user was added.</param>
/// <param name="AddedBy">The identifier of the user who added this user.</param>
public sealed record TenantUserAddedEvent(
    string TenantId,
    string UserId,
    string Email,
    string Role,
    DateTimeOffset AddedAt,
    string? AddedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.user_added";
}

/// <summary>
/// Integration event raised when a user is removed from a tenant.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="RemovedAt">The timestamp when the user was removed.</param>
/// <param name="RemovedBy">The identifier of the user who removed this user.</param>
public sealed record TenantUserRemovedEvent(
    string TenantId,
    string UserId,
    string Email,
    DateTimeOffset RemovedAt,
    string? RemovedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.user_removed";
}

/// <summary>
/// Integration event raised when a user's role is changed within a tenant.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="OldRole">The previous role.</param>
/// <param name="NewRole">The new role.</param>
/// <param name="ChangedAt">The timestamp when the role was changed.</param>
/// <param name="ChangedBy">The identifier of the user who changed the role.</param>
public sealed record TenantUserRoleChangedEvent(
    string TenantId,
    string UserId,
    string Email,
    string OldRole,
    string NewRole,
    DateTimeOffset ChangedAt,
    string? ChangedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.user_role_changed";
}

/// <summary>
/// Integration event raised when tenant settings are updated.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="SettingsCategory">The category of settings that was updated.</param>
/// <param name="ChangedSettings">The settings that were changed (key-value pairs).</param>
/// <param name="UpdatedAt">The timestamp when the settings were updated.</param>
/// <param name="UpdatedBy">The identifier of the user who updated the settings.</param>
public sealed record TenantSettingsUpdatedEvent(
    string TenantId,
    string Name,
    string SettingsCategory,
    IReadOnlyDictionary<string, string?> ChangedSettings,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.settings_updated";
}

/// <summary>
/// Integration event raised when a tenant exceeds a quota or limit.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
/// <param name="QuotaType">The type of quota exceeded (e.g., storage, users, API calls).</param>
/// <param name="CurrentUsage">The current usage value.</param>
/// <param name="Limit">The quota limit.</param>
/// <param name="ExceededAt">The timestamp when the quota was exceeded.</param>
public sealed record TenantQuotaExceededEvent(
    string TenantId,
    string Name,
    string QuotaType,
    long CurrentUsage,
    long Limit,
    DateTimeOffset ExceededAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "tenancy.tenant.quota_exceeded";
}
