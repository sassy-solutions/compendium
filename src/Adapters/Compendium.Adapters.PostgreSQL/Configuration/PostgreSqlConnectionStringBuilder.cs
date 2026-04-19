// -----------------------------------------------------------------------
// <copyright file="PostgreSqlConnectionStringBuilder.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Configuration;

/// <summary>
/// Helper class for building PostgreSQL connection strings with optimized pooling parameters.
/// Enhances base connection strings with Npgsql-specific pooling configuration.
/// </summary>
public static class PostgreSqlConnectionStringBuilder
{
    /// <summary>
    /// Builds an enhanced connection string with Npgsql pooling parameters from PostgreSqlOptions.
    /// If the base connection string already contains pooling parameters, they will be overridden.
    /// </summary>
    /// <param name="options">The PostgreSQL configuration options.</param>
    /// <returns>A connection string with optimized pooling parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when ConnectionString is null or empty.</exception>
    public static string BuildConnectionString(PostgreSqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new ArgumentException("ConnectionString cannot be null or empty", nameof(options));
        }

        // Parse the base connection string
        var builder = new NpgsqlConnectionStringBuilder(options.ConnectionString);

        // Apply Npgsql connection pooling parameters from options
        builder.Pooling = options.EnablePooling;
        builder.MinPoolSize = options.MinimumPoolSize;
        builder.MaxPoolSize = options.MaximumPoolSize;
        builder.ConnectionIdleLifetime = options.ConnectionIdleLifetime;
        builder.ConnectionLifetime = options.ConnectionLifetime;
        builder.Timeout = options.ConnectionTimeout;
        builder.CommandTimeout = options.CommandTimeout;

        // Apply TCP keepalive if enabled
        if (options.Keepalive > 0)
        {
            builder.KeepAlive = options.Keepalive;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Validates that the connection string and pooling configuration are valid.
    /// </summary>
    /// <param name="options">The PostgreSQL configuration options to validate.</param>
    /// <returns>A tuple indicating validation success and any error message.</returns>
    public static (bool IsValid, string? ErrorMessage) Validate(PostgreSqlOptions options)
    {
        if (options == null)
        {
            return (false, "PostgreSqlOptions cannot be null");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return (false, "ConnectionString is required");
        }

        if (options.MinimumPoolSize < 0)
        {
            return (false, "MinimumPoolSize cannot be negative");
        }

        if (options.MaximumPoolSize <= 0)
        {
            return (false, "MaximumPoolSize must be greater than 0");
        }

        if (options.MinimumPoolSize > options.MaximumPoolSize)
        {
            return (false, "MinimumPoolSize cannot be greater than MaximumPoolSize");
        }

        if (options.MaxPoolSize < options.MaximumPoolSize)
        {
            return (false, $"Application MaxPoolSize ({options.MaxPoolSize}) should be >= Npgsql MaximumPoolSize ({options.MaximumPoolSize})");
        }

        if (options.ConnectionIdleLifetime < 0)
        {
            return (false, "ConnectionIdleLifetime cannot be negative");
        }

        if (options.ConnectionLifetime < 0)
        {
            return (false, "ConnectionLifetime cannot be negative");
        }

        if (options.ConnectionTimeout <= 0)
        {
            return (false, "ConnectionTimeout must be greater than 0");
        }

        if (options.CommandTimeout <= 0)
        {
            return (false, "CommandTimeout must be greater than 0");
        }

        if (options.Keepalive < 0)
        {
            return (false, "Keepalive cannot be negative");
        }

        // Try to parse the connection string
        try
        {
            _ = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        }
        catch (Exception ex)
        {
            return (false, $"Invalid connection string: {ex.Message}");
        }

        return (true, null);
    }

    /// <summary>
    /// Gets the recommended production configuration for high-load scenarios.
    /// </summary>
    /// <returns>PostgreSqlOptions with optimized settings for 1000+ concurrent operations.</returns>
    public static PostgreSqlOptions GetRecommendedProductionOptions()
    {
        return new PostgreSqlOptions
        {
            MaxPoolSize = 200,
            CommandTimeout = 60,
            MinimumPoolSize = 50,
            MaximumPoolSize = 200,
            ConnectionIdleLifetime = 900,  // 15 minutes
            ConnectionLifetime = 3600,     // 1 hour
            ConnectionTimeout = 30,
            Keepalive = 30,
            EnablePooling = true,
            AutoCreateSchema = false,
            BatchSize = 1000
        };
    }

    /// <summary>
    /// Gets a conservative configuration for moderate-load scenarios.
    /// </summary>
    /// <returns>PostgreSqlOptions with balanced settings for moderate concurrency.</returns>
    public static PostgreSqlOptions GetConservativeOptions()
    {
        return new PostgreSqlOptions
        {
            MaxPoolSize = 100,
            CommandTimeout = 60,
            MinimumPoolSize = 25,
            MaximumPoolSize = 100,
            ConnectionIdleLifetime = 600,  // 10 minutes
            ConnectionLifetime = 1800,     // 30 minutes
            ConnectionTimeout = 30,
            Keepalive = 60,
            EnablePooling = true,
            AutoCreateSchema = false,
            BatchSize = 500
        };
    }
}
