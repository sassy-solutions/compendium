// -----------------------------------------------------------------------
// <copyright file="RowLevelSecurityExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Compendium.Core.Results;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Security;

/// <summary>
/// Extension methods for PostgreSQL Row-Level Security (RLS) support.
/// COMP-023: Multi-Tenancy Security Hardening
///
/// These extensions provide defense-in-depth security by setting database-level
/// tenant context for RLS policies. RLS provides protection against:
/// - Application bugs that bypass tenant filtering
/// - Direct database access (admin tools, reporting)
/// - SQL injection attacks
///
/// IMPORTANT: RLS is an OPTIONAL security enhancement. The Compendium framework
/// already enforces 100% tenant isolation at the application level.
/// </summary>
public static class RowLevelSecurityExtensions
{
    private static readonly Regex _tenantIdValidationRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Sets the tenant context for Row-Level Security on this connection.
    /// Call this immediately after opening a database connection.
    /// </summary>
    /// <param name="connection">The Npgsql connection.</param>
    /// <param name="tenantId">The tenant identifier to set. Must be alphanumeric, hyphen, or underscore only.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tenantId format is invalid.</exception>
    /// <example>
    /// <code>
    /// using var connection = new NpgsqlConnection(connectionString);
    /// await connection.OpenAsync();
    /// await connection.SetTenantContextAsync("tenant-abc-123");
    ///
    /// // Now all queries are automatically filtered by RLS to this tenant
    /// var events = await connection.QueryAsync("SELECT * FROM event_store WHERE stream_id = @Id", new { Id = "order-123" });
    /// </code>
    /// </example>
    public static async Task<Result> SetTenantContextAsync(
        this NpgsqlConnection connection,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Error.Validation(
                "RLS.InvalidTenantId",
                "Tenant ID cannot be null or whitespace");
        }

        // Validate tenant ID format to prevent SQL injection
        if (!_tenantIdValidationRegex.IsMatch(tenantId))
        {
            return Error.Validation(
                "RLS.InvalidTenantIdFormat",
                $"Tenant ID must contain only alphanumeric characters, hyphens, or underscores. Got: {tenantId}");
        }

        try
        {
            using var cmd = new NpgsqlCommand("SELECT set_tenant_context(@tenantId)", connection);
            cmd.Parameters.AddWithValue("tenantId", tenantId);

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (PostgresException ex) when (ex.SqlState == "42883") // Function does not exist
        {
            return Error.Failure(
                "RLS.FunctionNotFound",
                "Row-Level Security function 'set_tenant_context' not found. Did you run migration 002-row-level-security.sql?");
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "RLS.SetContextFailed",
                $"Failed to set tenant context: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current tenant context from the database session.
    /// </summary>
    /// <param name="connection">The Npgsql connection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current tenant ID, or null if not set.</returns>
    /// <example>
    /// <code>
    /// using var connection = new NpgsqlConnection(connectionString);
    /// await connection.OpenAsync();
    ///
    /// var currentTenant = await connection.GetTenantContextAsync();
    /// Console.WriteLine($"Current tenant: {currentTenant}");
    /// </code>
    /// </example>
    public static async Task<Result<string?>> GetTenantContextAsync(
        this NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        try
        {
            using var cmd = new NpgsqlCommand("SELECT get_tenant_context()", connection);
            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success<string?>(result as string);
        }
        catch (PostgresException ex) when (ex.SqlState == "42883") // Function does not exist
        {
            return Error.Failure(
                "RLS.FunctionNotFound",
                "Row-Level Security function 'get_tenant_context' not found. Did you run migration 002-row-level-security.sql?");
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "RLS.GetContextFailed",
                $"Failed to get tenant context: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the tenant context for this database session.
    /// Use for administrative operations that need cross-tenant access.
    /// </summary>
    /// <param name="connection">The Npgsql connection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// WARNING: Only use this for trusted administrative operations.
    /// After clearing context, the connection can access ALL tenants' data.
    /// </remarks>
    /// <example>
    /// <code>
    /// using var connection = new NpgsqlConnection(connectionString);
    /// await connection.OpenAsync();
    /// await connection.SetTenantContextAsync("tenant-abc");
    ///
    /// // Later, for admin operation:
    /// await connection.ClearTenantContextAsync();
    /// // Now can access all tenants
    /// </code>
    /// </example>
    public static async Task<Result> ClearTenantContextAsync(
        this NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        try
        {
            using var cmd = new NpgsqlCommand("SELECT clear_tenant_context()", connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (PostgresException ex) when (ex.SqlState == "42883") // Function does not exist
        {
            return Error.Failure(
                "RLS.FunctionNotFound",
                "Row-Level Security function 'clear_tenant_context' not found. Did you run migration 002-row-level-security.sql?");
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "RLS.ClearContextFailed",
                $"Failed to clear tenant context: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a tenant ID format for security.
    /// Prevents SQL injection and path traversal attacks.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate.</param>
    /// <returns>True if the tenant ID is valid, false otherwise.</returns>
    /// <remarks>
    /// Valid tenant IDs must:
    /// - Not be null or whitespace
    /// - Contain only: a-z, A-Z, 0-9, hyphen (-), underscore (_)
    /// - Be between 1 and 255 characters
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!RowLevelSecurityExtensions.IsValidTenantId(userInputTenantId))
    /// {
    ///     throw new ArgumentException("Invalid tenant ID format");
    /// }
    /// </code>
    /// </example>
    public static bool IsValidTenantId(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        if (tenantId.Length > 255)
        {
            return false;
        }

        return _tenantIdValidationRegex.IsMatch(tenantId);
    }

    /// <summary>
    /// Creates a safe tenant filtering WHERE clause for SQL queries.
    /// Use this when building custom queries that need tenant isolation.
    /// </summary>
    /// <param name="tenantId">The tenant ID to filter by.</param>
    /// <param name="columnName">The column name containing tenant_id (default: "tenant_id").</param>
    /// <returns>A SQL WHERE clause fragment with parameterized tenant filtering.</returns>
    /// <exception cref="ArgumentException">Thrown when tenantId format is invalid.</exception>
    /// <remarks>
    /// This method returns a safe SQL fragment that can be included in WHERE clauses.
    /// It handles both single-tenant and multi-tenant (NULL tenant_id) scenarios.
    /// </remarks>
    /// <example>
    /// <code>
    /// var tenantFilter = RowLevelSecurityExtensions.CreateTenantFilter(tenantId);
    /// var sql = $@"
    ///     SELECT * FROM custom_table
    ///     WHERE aggregate_id = @AggregateId
    ///     {tenantFilter}";
    ///
    /// // Produces: AND (@TenantId IS NULL OR tenant_id = @TenantId)
    /// </code>
    /// </example>
    public static string CreateTenantFilter(string? tenantId, string columnName = "tenant_id")
    {
        if (!string.IsNullOrWhiteSpace(tenantId) && !IsValidTenantId(tenantId))
        {
            throw new ArgumentException(
                $"Invalid tenant ID format: {tenantId}. Must contain only alphanumeric characters, hyphens, or underscores.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(columnName) || !IsValidColumnName(columnName))
        {
            throw new ArgumentException(
                $"Invalid column name: {columnName}. Must be a valid SQL identifier.",
                nameof(columnName));
        }

        return $"AND (@TenantId IS NULL OR {columnName} = @TenantId)";
    }

    /// <summary>
    /// Validates a column name to prevent SQL injection.
    /// </summary>
    private static bool IsValidColumnName(string columnName)
    {
        // Allow: alphanumeric, underscore, and dot (for table.column syntax)
        return Regex.IsMatch(columnName, @"^[a-zA-Z0-9_\.]+$");
    }
}
