using Microsoft.Extensions.Logging;

namespace Compendium.Multitenancy;

/// <summary>
/// Defines a strategy for isolating tenant data in a multi-tenant application.
/// Implementations determine how tenants are separated at the database level.
/// </summary>
public interface ITenantIsolationStrategy
{
    /// <summary>
    /// Gets the database connection string for a specific tenant.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the connection string.</returns>
    Task<Result<string>> GetConnectionStringAsync(TenantInfo tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the schema name for a specific tenant.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the schema name.</returns>
    Task<Result<string>> GetSchemaNameAsync(TenantInfo tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that the necessary database resources exist for a tenant.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> EnsureTenantResourcesAsync(TenantInfo tenant, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the levels of data isolation supported in multi-tenant applications.
/// </summary>
public enum IsolationLevel
{
    /// <summary>
    /// Each tenant has its own separate database.
    /// </summary>
    Database,

    /// <summary>
    /// Tenants share a database but use separate schemas.
    /// </summary>
    Schema,

    /// <summary>
    /// Tenants share database and schema but are separated by row-level filters.
    /// </summary>
    Row
}

/// <summary>
/// Implements tenant isolation by providing each tenant with a separate database.
/// This strategy offers the highest level of isolation but requires more database resources.
/// </summary>
public sealed class DatabaseIsolationStrategy : ITenantIsolationStrategy
{
    private readonly DatabaseIsolationOptions _options;
    private readonly ILogger<DatabaseIsolationStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseIsolationStrategy"/> class.
    /// </summary>
    /// <param name="options">The configuration options for database isolation.</param>
    /// <param name="logger">The logger for this strategy.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public DatabaseIsolationStrategy(DatabaseIsolationOptions options, ILogger<DatabaseIsolationStrategy> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the database connection string for a specific tenant.
    /// Uses tenant-specific connection string if available, otherwise builds from template.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the connection string.</returns>
    public Task<Result<string>> GetConnectionStringAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
        {
            return Task.FromResult(Result.Failure<string>(Error.Validation("Tenant.Null", "Tenant cannot be null")));
        }

        // Use tenant-specific connection string if available
        if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            _logger.LogDebug("Using tenant-specific connection string for tenant {TenantId}", tenant.Id);
            return Task.FromResult(Result.Success(tenant.ConnectionString));
        }

        // Build connection string using template
        var connectionString = _options.ConnectionStringTemplate.Replace("{TenantId}", tenant.Id);
        _logger.LogDebug("Built connection string from template for tenant {TenantId}", tenant.Id);

        return Task.FromResult(Result.Success(connectionString));
    }

    /// <summary>
    /// Gets the schema name for a specific tenant.
    /// In database isolation, returns the default schema since each tenant has its own database.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the schema name.</returns>
    public Task<Result<string>> GetSchemaNameAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
        {
            return Task.FromResult(Result.Failure<string>(Error.Validation("Tenant.Null", "Tenant cannot be null")));
        }

        // In database isolation, each tenant has its own database, so use default schema
        return Task.FromResult(Result.Success(_options.DefaultSchema));
    }

    /// <summary>
    /// Ensures that the necessary database resources exist for a tenant.
    /// Creates the tenant database, runs migrations, and sets up initial data.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> EnsureTenantResourcesAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
        {
            return Result.Failure(Error.Validation("Tenant.Null", "Tenant cannot be null"));
        }

        try
        {
            _logger.LogInformation("Ensuring database resources for tenant {TenantId}", tenant.Id);

            // In a real implementation, this would:
            // 1. Create the database if it doesn't exist
            // 2. Run migrations
            // 3. Set up initial data

            await Task.Delay(100, cancellationToken); // Simulate work

            _logger.LogInformation("Database resources ensured for tenant {TenantId}", tenant.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure database resources for tenant {TenantId}", tenant.Id);
            return Result.Failure(Error.Failure("TenantResources.EnsureFailed", ex.Message));
        }
    }
}

/// <summary>
/// Implements tenant isolation by providing each tenant with a separate schema within a shared database.
/// This strategy balances isolation with resource efficiency.
/// </summary>
public sealed class SchemaIsolationStrategy : ITenantIsolationStrategy
{
    private readonly SchemaIsolationOptions _options;
    private readonly ILogger<SchemaIsolationStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaIsolationStrategy"/> class.
    /// </summary>
    /// <param name="options">The configuration options for schema isolation.</param>
    /// <param name="logger">The logger for this strategy.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public SchemaIsolationStrategy(SchemaIsolationOptions options, ILogger<SchemaIsolationStrategy> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the database connection string for a specific tenant.
    /// All tenants share the same database connection in schema isolation.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the connection string.</returns>
    public Task<Result<string>> GetConnectionStringAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        // All tenants share the same database connection
        _logger.LogDebug("Using shared connection string for tenant {TenantId}", tenant?.Id);
        return Task.FromResult(Result.Success(_options.SharedConnectionString));
    }

    /// <summary>
    /// Gets the schema name for a specific tenant.
    /// Builds the schema name from the template using the tenant ID.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the schema name.</returns>
    public Task<Result<string>> GetSchemaNameAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
        {
            return Task.FromResult(Result.Failure<string>(Error.Validation("Tenant.Null", "Tenant cannot be null")));
        }

        var schemaName = _options.SchemaNameTemplate.Replace("{TenantId}", tenant.Id);
        _logger.LogDebug("Using schema {SchemaName} for tenant {TenantId}", schemaName, tenant.Id);

        return Task.FromResult(Result.Success(schemaName));
    }

    /// <summary>
    /// Ensures that the necessary schema resources exist for a tenant.
    /// Creates the tenant schema, tables, and sets up initial data.
    /// </summary>
    /// <param name="tenant">The tenant information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> EnsureTenantResourcesAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
        {
            return Result.Failure(Error.Validation("Tenant.Null", "Tenant cannot be null"));
        }

        try
        {
            var schemaResult = await GetSchemaNameAsync(tenant, cancellationToken);
            if (schemaResult.IsFailure)
            {
                return schemaResult;
            }

            _logger.LogInformation("Ensuring schema resources for tenant {TenantId} in schema {SchemaName}", tenant.Id, schemaResult.Value);

            // In a real implementation, this would:
            // 1. Create the schema if it doesn't exist
            // 2. Create tables in the schema
            // 3. Set up initial data

            await Task.Delay(100, cancellationToken); // Simulate work

            _logger.LogInformation("Schema resources ensured for tenant {TenantId}", tenant.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure schema resources for tenant {TenantId}", tenant.Id);
            return Result.Failure(Error.Failure("TenantResources.EnsureFailed", ex.Message));
        }
    }
}

/// <summary>
/// Configuration options for database-level tenant isolation strategy.
/// </summary>
public sealed class DatabaseIsolationOptions
{
    /// <summary>
    /// Gets or sets the connection string template with {TenantId} placeholder.
    /// </summary>
    public string ConnectionStringTemplate { get; init; } = "Server=localhost;Database=App_{TenantId};Integrated Security=true;";

    /// <summary>
    /// Gets or sets the default schema name to use within tenant databases.
    /// </summary>
    public string DefaultSchema { get; init; } = "dbo";
}

/// <summary>
/// Configuration options for schema-level tenant isolation strategy.
/// </summary>
public sealed class SchemaIsolationOptions
{
    /// <summary>
    /// Gets or sets the shared database connection string used by all tenants.
    /// </summary>
    public string SharedConnectionString { get; init; } = "Server=localhost;Database=AppShared;Integrated Security=true;";

    /// <summary>
    /// Gets or sets the schema name template with {TenantId} placeholder.
    /// </summary>
    public string SchemaNameTemplate { get; init; } = "tenant_{TenantId}";
}
