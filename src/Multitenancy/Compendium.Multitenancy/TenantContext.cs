namespace Compendium.Multitenancy;

/// <summary>
/// Provides access to the current tenant context information.
/// Enables applications to operate in a multi-tenant environment by tracking the active tenant.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the ID of the current tenant.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the name of the current tenant.
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// Gets the complete information of the current tenant.
    /// </summary>
    TenantInfo? CurrentTenant { get; }

    /// <summary>
    /// Gets a value indicating whether a tenant is currently set.
    /// </summary>
    bool HasTenant { get; }
}

/// <summary>
/// Default implementation of tenant context using AsyncLocal for thread-safe tenant isolation.
/// Maintains tenant information per async context, ensuring proper isolation in concurrent scenarios.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly AsyncLocal<TenantInfo?> _currentTenant = new();

    /// <summary>
    /// Gets the ID of the current tenant.
    /// </summary>
    public string? TenantId => _currentTenant.Value?.Id;

    /// <summary>
    /// Gets the name of the current tenant.
    /// </summary>
    public string? TenantName => _currentTenant.Value?.Name;

    /// <summary>
    /// Gets the complete information of the current tenant.
    /// </summary>
    public TenantInfo? CurrentTenant => _currentTenant.Value;

    /// <summary>
    /// Gets a value indicating whether a tenant is currently set.
    /// </summary>
    public bool HasTenant => _currentTenant.Value is not null;

    /// <summary>
    /// Sets the current tenant information.
    /// </summary>
    /// <param name="tenant">The tenant information to set.</param>
    public void SetTenant(TenantInfo? tenant)
    {
        _currentTenant.Value = tenant;
    }
}

/// <summary>
/// Represents information about a tenant in a multi-tenant application.
/// Contains all necessary data to identify and configure tenant-specific behavior.
/// </summary>
public sealed record TenantInfo
{
    /// <summary>
    /// Gets or initializes the unique identifier of the tenant.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the display name of the tenant.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the tenant-specific database connection string.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Gets or initializes additional custom properties for the tenant.
    /// </summary>
    public Dictionary<string, object?> Properties { get; init; } = new();

    /// <summary>
    /// Gets or initializes a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets or initializes the timestamp when the tenant was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the tenant was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Provides a disposable scope for temporarily setting a tenant context.
/// When disposed, restores the previous tenant context, enabling nested tenant contexts.
/// </summary>
public sealed class TenantScope : IDisposable
{
    private readonly TenantContext _context;
    private readonly TenantInfo? _previousTenant;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScope"/> class.
    /// </summary>
    /// <param name="context">The tenant context to modify.</param>
    /// <param name="tenant">The tenant information to set for this scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public TenantScope(TenantContext context, TenantInfo? tenant)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previousTenant = context.CurrentTenant;
        _context.SetTenant(tenant);
    }

    /// <summary>
    /// Restores the previous tenant context when the scope is disposed.
    /// </summary>
    public void Dispose()
    {
        _context.SetTenant(_previousTenant);
    }
}
