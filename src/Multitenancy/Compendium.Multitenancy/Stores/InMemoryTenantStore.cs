// -----------------------------------------------------------------------
// <copyright file="InMemoryTenantStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Multitenancy.Extensions;

namespace Compendium.Multitenancy.Stores;

/// <summary>
/// An in-memory implementation of the tenant store for development and testing.
/// </summary>
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<string, TenantInfo> _tenants = new();
    private readonly ConcurrentDictionary<string, string> _identifierToIdMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTenantStore"/> class.
    /// </summary>
    /// <param name="options">The options containing initial tenants.</param>
    public InMemoryTenantStore(InMemoryTenantStoreOptions? options = null)
    {
        if (options?.InitialTenants is not null)
        {
            foreach (var tenant in options.InitialTenants)
            {
                AddTenant(tenant);
            }
        }
    }

    /// <inheritdoc />
    public Task<Result<TenantInfo?>> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult(Result.Success<TenantInfo?>(null));
        }

        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(Result.Success(tenant));
    }

    /// <inheritdoc />
    public Task<Result<TenantInfo?>> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return Task.FromResult(Result.Success<TenantInfo?>(null));
        }

        // First try to find by ID
        if (_tenants.TryGetValue(identifier, out var tenant))
        {
            return Task.FromResult(Result.Success<TenantInfo?>(tenant));
        }

        // Then try to find by mapped identifier
        if (_identifierToIdMap.TryGetValue(identifier, out var tenantId))
        {
            _tenants.TryGetValue(tenantId, out tenant);
        }

        return Task.FromResult(Result.Success(tenant));
    }

    /// <inheritdoc />
    public Task<Result<IEnumerable<TenantInfo>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = _tenants.Values.ToList();
        return Task.FromResult(Result.Success<IEnumerable<TenantInfo>>(tenants));
    }

    /// <inheritdoc />
    public Task<Result> SaveAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (string.IsNullOrWhiteSpace(tenant.Id))
        {
            return Task.FromResult(Result.Failure(Error.Validation("Tenant.IdRequired", "Tenant ID is required")));
        }

        AddTenant(tenant);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult(Result.Failure(Error.Validation("Tenant.IdRequired", "Tenant ID is required")));
        }

        if (_tenants.TryRemove(tenantId, out var removed))
        {
            // Remove all identifier mappings for this tenant
            var keysToRemove = _identifierToIdMap
                .Where(kvp => kvp.Value == tenantId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _identifierToIdMap.TryRemove(key, out _);
            }

            return Task.FromResult(Result.Success());
        }

        return Task.FromResult(Result.Failure(Error.NotFound("Tenant.NotFound", $"Tenant '{tenantId}' not found")));
    }

    /// <summary>
    /// Adds an identifier mapping for a tenant.
    /// </summary>
    /// <param name="identifier">The alternate identifier (e.g., subdomain).</param>
    /// <param name="tenantId">The tenant ID to map to.</param>
    public void AddIdentifierMapping(string identifier, string tenantId)
    {
        _identifierToIdMap[identifier] = tenantId;
    }

    /// <summary>
    /// Gets the number of tenants in the store.
    /// </summary>
    public int Count => _tenants.Count;

    private void AddTenant(TenantInfo tenant)
    {
        _tenants[tenant.Id] = tenant;

        // Also map the name as an identifier if it's different from the ID
        if (!string.IsNullOrWhiteSpace(tenant.Name) &&
            !tenant.Name.Equals(tenant.Id, StringComparison.OrdinalIgnoreCase))
        {
            _identifierToIdMap[tenant.Name] = tenant.Id;
        }
    }
}
