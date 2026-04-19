// -----------------------------------------------------------------------
// <copyright file="PostgreSqlHealthCheck.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Npgsql;

namespace Compendium.Adapters.AspNetCore.Health;

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// Verifies database connection and performs a simple query.
/// </summary>
public sealed class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlHealthCheck> _logger;

    public PostgreSqlHealthCheck(string connectionString, ILogger<PostgreSqlHealthCheck> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is 1)
            {
                return HealthCheckResult.Healthy("PostgreSQL is healthy");
            }

            return HealthCheckResult.Degraded("PostgreSQL query returned unexpected result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL health check failed");
            return HealthCheckResult.Unhealthy("PostgreSQL is unhealthy", ex);
        }
    }
}
