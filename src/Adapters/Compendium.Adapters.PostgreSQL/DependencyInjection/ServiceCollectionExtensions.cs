// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.PostgreSQL.Projections;
using Compendium.Core.EventSourcing;
using Compendium.Infrastructure.EventSourcing;
using Compendium.Infrastructure.Projections;
using Microsoft.Extensions.DependencyInjection;
using IProjectionStore = Compendium.Infrastructure.Projections.IProjectionStore;

namespace Compendium.Adapters.PostgreSQL.DependencyInjection;

/// <summary>
/// Extension methods for registering PostgreSQL adapters in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL event store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlEventStore(
        this IServiceCollection services,
        Action<PostgreSqlOptions>? configurationAction = null)
    {
        if (configurationAction != null)
        {
            services.Configure(configurationAction);
        }

        // Register event type registry (required by SecureEventDeserializer)
        services.AddSingleton<IEventTypeRegistry, EventTypeRegistry>();

        // Register event deserializer (required by PostgreSqlEventStore)
        services.AddSingleton<IEventDeserializer, SecureEventDeserializer>();

        // Register event store (both as interface and concrete type for streaming event store)
        services.AddSingleton<PostgreSqlEventStore>();
        services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<PostgreSqlEventStore>());

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL event store to the service collection with configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddPostgreSqlEventStore(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds PostgreSQL projection checkpoint store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProjectionCheckpointStore(
        this IServiceCollection services,
        Action<PostgreSqlOptions>? configurationAction = null)
    {
        if (configurationAction != null)
        {
            services.Configure(configurationAction);
        }

        services.AddSingleton<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL projection checkpoint store to the service collection with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProjectionCheckpointStore(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddPostgreSqlProjectionCheckpointStore(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds PostgreSQL projection store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProjectionStore(
        this IServiceCollection services,
        Action<PostgreSqlOptions>? configurationAction = null)
    {
        if (configurationAction != null)
        {
            services.Configure(configurationAction);
        }

        services.AddSingleton<IProjectionStore, PostgreSqlProjectionStore>();

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL projection store to the service collection with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProjectionStore(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddPostgreSqlProjectionStore(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds PostgreSQL streaming event store to the service collection.
    /// Required for live projection processing.
    /// IMPORTANT: This method should be called AFTER AddPostgreSqlEventStore.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration action (ignored if already configured).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlStreamingEventStore(
        this IServiceCollection services,
        Action<PostgreSqlOptions>? configurationAction = null)
    {
        // Note: Configuration is already handled by AddPostgreSqlEventStore
        // This method should be called after AddPostgreSqlEventStore which registers all necessary dependencies

        // Register streaming event store (depends on PostgreSqlEventStore from AddPostgreSqlEventStore)
        services.AddSingleton<IStreamingEventStore, PostgreSqlStreamingEventStore>();

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL streaming event store to the service collection with connection string.
    /// Required for live projection processing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlStreamingEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddPostgreSqlStreamingEventStore(options =>
        {
            options.ConnectionString = connectionString;
        });
    }
}
