// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Compendium.Infrastructure.Resilience;

/// <summary>
/// Extension methods for registering resilience services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Polly resilience pipelines for PostgreSQL and Redis to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurePostgreSql">Optional configuration for PostgreSQL resilience options.</param>
    /// <param name="configureRedis">Optional configuration for Redis resilience options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPollyResilience(
        this IServiceCollection services,
        Action<PollyResilienceOptions>? configurePostgreSql = null,
        Action<PollyResilienceOptions>? configureRedis = null)
    {
        // Register the factory as a singleton
        services.AddSingleton<PollyResiliencePipelineFactory>();

        // Register telemetry listener
        services.AddSingleton<ResilienceTelemetryListener>();

        // Register PostgreSQL resilience pipeline
        services.AddSingleton<ResiliencePipeline>(sp =>
        {
            var factory = sp.GetRequiredService<PollyResiliencePipelineFactory>();
            var options = PollyResilienceOptions.PostgreSqlDefaults();
            configurePostgreSql?.Invoke(options);
            return factory.CreatePostgreSqlPipeline(options);
        });

        // Register Redis resilience pipeline as a named service using a different approach
        // Note: Consumers can get both pipelines through PollyResiliencePipelineFactory

        return services;
    }

    /// <summary>
    /// Adds Polly resilience pipelines with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="postgresqlOptions">PostgreSQL resilience options.</param>
    /// <param name="redisOptions">Redis resilience options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPollyResilience(
        this IServiceCollection services,
        PollyResilienceOptions postgresqlOptions,
        PollyResilienceOptions redisOptions)
    {
        services.AddSingleton<PollyResiliencePipelineFactory>();
        services.AddSingleton<ResilienceTelemetryListener>();

        // Store options for later use
        services.AddSingleton(postgresqlOptions);
        services.AddSingleton(redisOptions);

        return services;
    }
}
