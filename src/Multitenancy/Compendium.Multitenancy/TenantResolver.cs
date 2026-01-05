using Microsoft.Extensions.Logging;

namespace Compendium.Multitenancy;

/// <summary>
/// Defines a strategy for resolving tenant information from request context.
/// Implementations can resolve tenants from various sources like headers, domains, or paths.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves tenant information from the provided resolution context.
    /// </summary>
    /// <param name="context">The tenant resolution context containing request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the resolved tenant or null if not found.</returns>
    Task<Result<TenantInfo?>> ResolveTenantAsync(TenantResolutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains context information used for tenant resolution.
/// Aggregates various request properties that tenant resolvers can use to identify tenants.
/// </summary>
public sealed class TenantResolutionContext
{
    /// <summary>
    /// Gets or initializes the HTTP headers from the request.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// Gets or initializes the query parameters from the request.
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; init; } = new();

    /// <summary>
    /// Gets or initializes the host name from the request.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Gets or initializes the request path.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or initializes additional custom properties for resolution.
    /// </summary>
    public Dictionary<string, object?> Properties { get; init; } = new();
}

/// <summary>
/// A composite tenant resolver that tries multiple resolution strategies in sequence.
/// Returns the result from the first resolver that successfully identifies a tenant.
/// </summary>
public sealed class CompositeTenantResolver : ITenantResolver
{
    private readonly IEnumerable<ITenantResolver> _resolvers;
    private readonly ILogger<CompositeTenantResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTenantResolver"/> class.
    /// </summary>
    /// <param name="resolvers">The collection of tenant resolvers to try in order.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when resolvers or logger is null.</exception>
    public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers, ILogger<CompositeTenantResolver> logger)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves tenant information by trying each resolver in sequence until one succeeds.
    /// </summary>
    /// <param name="context">The tenant resolution context containing request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the resolved tenant or null if not found.</returns>
    public async Task<Result<TenantInfo?>> ResolveTenantAsync(TenantResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            return Result.Failure<TenantInfo?>(Error.Validation("TenantResolution.NullContext", "Tenant resolution context cannot be null"));
        }

        foreach (var resolver in _resolvers)
        {
            try
            {
                _logger.LogDebug("Attempting tenant resolution with {ResolverType}", resolver.GetType().Name);

                var result = await resolver.ResolveTenantAsync(context, cancellationToken);

                if (result.IsSuccess && result.Value is not null)
                {
                    _logger.LogDebug("Tenant resolved by {ResolverType}: {TenantId}", resolver.GetType().Name, result.Value.Id);
                    return result;
                }

                if (result.IsFailure)
                {
                    _logger.LogWarning("Tenant resolution failed with {ResolverType}: {Error}", resolver.GetType().Name, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in tenant resolver {ResolverType}", resolver.GetType().Name);
            }
        }

        _logger.LogDebug("No tenant resolved by any resolver");
        return Result.Success<TenantInfo?>(null);
    }
}

/// <summary>
/// A tenant resolver that identifies tenants based on HTTP headers.
/// Looks for tenant identifier in a specified header and resolves the tenant from storage.
/// </summary>
public sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly ITenantStore _tenantStore;
    private readonly HeaderTenantResolverOptions _options;
    private readonly ILogger<HeaderTenantResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderTenantResolver"/> class.
    /// </summary>
    /// <param name="tenantStore">The tenant store for retrieving tenant information.</param>
    /// <param name="options">The configuration options for header-based resolution.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public HeaderTenantResolver(ITenantStore tenantStore, HeaderTenantResolverOptions options, ILogger<HeaderTenantResolver> logger)
    {
        _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves tenant information from HTTP headers.
    /// </summary>
    /// <param name="context">The tenant resolution context containing request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the resolved tenant or null if not found.</returns>
    public async Task<Result<TenantInfo?>> ResolveTenantAsync(TenantResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Headers.TryGetValue(_options.HeaderName, out var tenantId) || string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogDebug("No tenant ID found in header {HeaderName}", _options.HeaderName);
            return Result.Success<TenantInfo?>(null);
        }

        _logger.LogDebug("Found tenant ID {TenantId} in header {HeaderName}", tenantId, _options.HeaderName);
        return await _tenantStore.GetByIdAsync(tenantId, cancellationToken);
    }
}

/// <summary>
/// Configuration options for header-based tenant resolution.
/// </summary>
public sealed class HeaderTenantResolverOptions
{
    /// <summary>
    /// Gets or initializes the name of the HTTP header containing the tenant identifier.
    /// Default is "X-Tenant-ID".
    /// </summary>
    public string HeaderName { get; init; } = "X-Tenant-ID";
}

/// <summary>
/// A tenant resolver that identifies tenants based on the request host/domain.
/// Can resolve tenants from subdomains or full domain names.
/// </summary>
public sealed class HostTenantResolver : ITenantResolver
{
    private readonly ITenantStore _tenantStore;
    private readonly HostTenantResolverOptions _options;
    private readonly ILogger<HostTenantResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostTenantResolver"/> class.
    /// </summary>
    /// <param name="tenantStore">The tenant store for retrieving tenant information.</param>
    /// <param name="options">The configuration options for host-based resolution.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public HostTenantResolver(ITenantStore tenantStore, HostTenantResolverOptions options, ILogger<HostTenantResolver> logger)
    {
        _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves tenant information from the request host/domain.
    /// </summary>
    /// <param name="context">The tenant resolution context containing request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the resolved tenant or null if not found.</returns>
    public async Task<Result<TenantInfo?>> ResolveTenantAsync(TenantResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.Host))
        {
            _logger.LogDebug("No host found in resolution context");
            return Result.Success<TenantInfo?>(null);
        }

        var host = context.Host.ToLowerInvariant();

        // Extract subdomain if configured
        if (_options.UseSubdomain)
        {
            var parts = host.Split('.');
            if (parts.Length >= 3) // subdomain.domain.tld
            {
                var subdomain = parts[0];
                _logger.LogDebug("Extracted subdomain {Subdomain} from host {Host}", subdomain, host);
                return await _tenantStore.GetByIdentifierAsync(subdomain, cancellationToken);
            }
        }

        // Use full host as identifier
        _logger.LogDebug("Using full host {Host} as tenant identifier", host);
        return await _tenantStore.GetByIdentifierAsync(host, cancellationToken);
    }
}

/// <summary>
/// Configuration options for host-based tenant resolution.
/// </summary>
public sealed class HostTenantResolverOptions
{
    /// <summary>
    /// Gets or initializes a value indicating whether to extract tenant identifier from subdomain.
    /// When true, extracts the first part of the hostname (e.g., "tenant" from "tenant.example.com").
    /// When false, uses the full hostname as tenant identifier.
    /// Default is true.
    /// </summary>
    public bool UseSubdomain { get; init; } = true;
}

/// <summary>
/// Defines the contract for storing and retrieving tenant information.
/// Provides persistent storage capabilities for multi-tenant applications.
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Retrieves a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the tenant or null if not found.</returns>
    Task<Result<TenantInfo?>> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tenant by an alternate identifier (e.g., subdomain, custom key).
    /// </summary>
    /// <param name="identifier">The alternate identifier to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the tenant or null if not found.</returns>
    Task<Result<TenantInfo?>> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tenants from the store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with all tenants.</returns>
    Task<Result<IEnumerable<TenantInfo>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a tenant to the store (create or update).
    /// </summary>
    /// <param name="tenant">The tenant information to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> SaveAsync(TenantInfo tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant from the store.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> DeleteAsync(string tenantId, CancellationToken cancellationToken = default);
}
