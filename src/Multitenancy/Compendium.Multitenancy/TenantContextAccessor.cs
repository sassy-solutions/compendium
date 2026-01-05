// -----------------------------------------------------------------------
// <copyright file="TenantContextAccessor.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Multitenancy;

/// <summary>
/// Provides access to the current tenant context in a thread-safe manner.
/// This is the primary interface for reading tenant information from any layer.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets the current tenant context.
    /// </summary>
    ITenantContext TenantContext { get; }
}

/// <summary>
/// Provides access to the tenant context and allows modification.
/// This interface is used by middleware and tenant resolution components.
/// </summary>
public interface ITenantContextSetter : ITenantContextAccessor
{
    /// <summary>
    /// Sets the current tenant.
    /// </summary>
    /// <param name="tenant">The tenant information to set.</param>
    void SetTenant(TenantInfo? tenant);

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    void ClearTenant();
}

/// <summary>
/// Default implementation of the tenant context accessor.
/// Uses AsyncLocal to maintain tenant context per async context.
/// </summary>
public sealed class TenantContextAccessor : ITenantContextSetter
{
    private readonly TenantContext _tenantContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantContextAccessor"/> class.
    /// </summary>
    public TenantContextAccessor()
    {
        _tenantContext = new TenantContext();
    }

    /// <summary>
    /// Gets the current tenant context.
    /// </summary>
    public ITenantContext TenantContext => _tenantContext;

    /// <summary>
    /// Sets the current tenant.
    /// </summary>
    /// <param name="tenant">The tenant information to set.</param>
    public void SetTenant(TenantInfo? tenant)
    {
        _tenantContext.SetTenant(tenant);
    }

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    public void ClearTenant()
    {
        _tenantContext.SetTenant(null);
    }
}
