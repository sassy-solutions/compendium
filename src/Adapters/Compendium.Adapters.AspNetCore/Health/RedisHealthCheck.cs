// -----------------------------------------------------------------------
// <copyright file="RedisHealthCheck.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Compendium.Adapters.AspNetCore.Health;

/// <summary>
/// Health check for Redis connectivity.
/// Verifies Redis connection and performs a PING command.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var pingResult = await db.PingAsync();

            if (pingResult.TotalMilliseconds < 1000) // < 1 second
            {
                return HealthCheckResult.Healthy(
                    $"Redis is healthy (ping: {pingResult.TotalMilliseconds:F2}ms)");
            }

            return HealthCheckResult.Degraded(
                $"Redis is slow (ping: {pingResult.TotalMilliseconds:F2}ms)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
        }
    }
}
