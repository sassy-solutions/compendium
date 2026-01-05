// -----------------------------------------------------------------------
// <copyright file="HealthCheckExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackExchange.Redis;

namespace Compendium.Adapters.AspNetCore.Health;

/// <summary>
/// Extension methods for configuring health checks in ASP.NET Core applications.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds Compendium framework health checks to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="postgresConnectionString">PostgreSQL connection string (optional).</param>
    /// <param name="redisConnectionMultiplexer">Redis connection multiplexer (optional).</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddCompendiumHealthChecks(
        this IServiceCollection services,
        string? postgresConnectionString = null,
        IConnectionMultiplexer? redisConnectionMultiplexer = null)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add PostgreSQL health check if connection string provided
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            healthChecksBuilder.AddCheck<PostgreSqlHealthCheck>(
                "postgresql",
                tags: new[] { "ready", "db" });

            services.AddSingleton(sp =>
                new PostgreSqlHealthCheck(
                    postgresConnectionString,
                    sp.GetRequiredService<ILogger<PostgreSqlHealthCheck>>()));
        }

        // Add Redis health check if connection multiplexer provided
        if (redisConnectionMultiplexer != null)
        {
            healthChecksBuilder.AddCheck<RedisHealthCheck>(
                "redis",
                tags: new[] { "ready", "cache" });

            services.AddSingleton(sp =>
                new RedisHealthCheck(
                    redisConnectionMultiplexer,
                    sp.GetRequiredService<ILogger<RedisHealthCheck>>()));
        }

        return healthChecksBuilder;
    }

    /// <summary>
    /// Maps health check endpoints with JSON response formatting.
    /// Configures /health (liveness) and /health/ready (readiness) endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapCompendiumHealthChecks(this IEndpointRouteBuilder app)
    {
        // Liveness endpoint - checks if the app is running
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false, // Don't check any registered health checks, just return if app is alive
            ResponseWriter = WriteHealthCheckResponse
        });

        // Readiness endpoint - checks if the app is ready to serve traffic
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        return app;
    }

    /// <summary>
    /// Writes a detailed JSON response for health check results.
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
