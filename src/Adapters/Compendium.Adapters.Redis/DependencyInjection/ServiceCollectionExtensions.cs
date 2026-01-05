// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.Idempotency;
using Compendium.Adapters.Redis.Projections;
using Compendium.Application.Idempotency;
using Compendium.Infrastructure.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.DependencyInjection;

/// <summary>
/// Extension methods for registering Redis adapters in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis services to the service collection with connection multiplexer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        Action<RedisOptions>? configurationAction = null)
    {
        if (configurationAction != null)
        {
            services.Configure(configurationAction);
        }

        // Register Redis connection multiplexer as singleton
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;

            var configuration = ConfigurationOptions.Parse(options.ConnectionString);
            configuration.ConnectTimeout = options.ConnectTimeout;
            configuration.CommandMap = CommandMap.Default;

            return ConnectionMultiplexer.Connect(configuration);
        });

        return services;
    }

    /// <summary>
    /// Adds Redis services to the service collection with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddRedis(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds Redis idempotency store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisIdempotencyStore(
        this IServiceCollection services,
        Action<RedisOptions>? configurationAction = null)
    {
        // Add Redis services if not already added
        services.AddRedis(configurationAction);

        // Register the Redis idempotency store
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();

        // Register the default idempotency service
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        return services;
    }

    /// <summary>
    /// Adds Redis idempotency store to the service collection with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisIdempotencyStore(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddRedisIdempotencyStore(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds Redis projection checkpoint store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action.</param>
    /// <param name="defaultExpiration">Default expiration time for checkpoints (default: 7 days).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisProjectionCheckpointStore(
        this IServiceCollection services,
        Action<RedisOptions>? configurationAction = null,
        TimeSpan? defaultExpiration = null)
    {
        // Add Redis services if not already added
        services.AddRedis(configurationAction);

        // Register the Redis projection checkpoint store
        services.AddSingleton<IProjectionCheckpointStore>(serviceProvider =>
        {
            var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>();
            var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<RedisProjectionCheckpointStore>>();

            return new RedisProjectionCheckpointStore(
                connectionMultiplexer,
                options,
                logger,
                defaultExpiration);
        });

        return services;
    }

    /// <summary>
    /// Adds Redis projection checkpoint store to the service collection with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="defaultExpiration">Default expiration time for checkpoints (default: 7 days).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisProjectionCheckpointStore(
        this IServiceCollection services,
        string connectionString,
        TimeSpan? defaultExpiration = null)
    {
        return services.AddRedisProjectionCheckpointStore(
            options => { options.ConnectionString = connectionString; },
            defaultExpiration);
    }
}
