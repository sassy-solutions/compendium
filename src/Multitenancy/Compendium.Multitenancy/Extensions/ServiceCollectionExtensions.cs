// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Multitenancy.Http;
using Compendium.Multitenancy.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Compendium.Multitenancy.Extensions;

/// <summary>
/// Extension methods for registering multitenancy services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core multitenancy services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumMultitenancy(this IServiceCollection services)
    {
        return services.AddCompendiumMultitenancy(_ => { });
    }

    /// <summary>
    /// Adds the core multitenancy services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for multitenancy options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumMultitenancy(
        this IServiceCollection services,
        Action<MultitenancyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MultitenancyOptions();
        configure(options);

        services.AddSingleton(options);

        // Register the tenant context accessor as a singleton
        // The TenantContext uses AsyncLocal internally, so a singleton is safe
        services.TryAddSingleton<TenantContextAccessor>();
        services.TryAddSingleton<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());
        services.TryAddSingleton<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContextAccessor>());
        services.TryAddSingleton<ITenantContext>(sp => sp.GetRequiredService<TenantContextAccessor>().TenantContext);

        // Register propagation options
        services.TryAddSingleton(options.Propagation);

        return services;
    }

    /// <summary>
    /// Adds header-based tenant resolution to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for header resolution options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHeaderTenantResolution(
        this IServiceCollection services,
        Action<HeaderTenantResolverOptions>? configure = null)
    {
        var options = new HeaderTenantResolverOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<ITenantResolver, HeaderTenantResolver>();

        return services;
    }

    /// <summary>
    /// Adds host/domain-based tenant resolution to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for host resolution options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHostTenantResolution(
        this IServiceCollection services,
        Action<HostTenantResolverOptions>? configure = null)
    {
        var options = new HostTenantResolverOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<ITenantResolver, HostTenantResolver>();

        return services;
    }

    /// <summary>
    /// Adds database-level tenant isolation strategy.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for database isolation options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseIsolation(
        this IServiceCollection services,
        Action<DatabaseIsolationOptions>? configure = null)
    {
        var options = new DatabaseIsolationOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<ITenantIsolationStrategy, DatabaseIsolationStrategy>();

        return services;
    }

    /// <summary>
    /// Adds schema-level tenant isolation strategy.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for schema isolation options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSchemaIsolation(
        this IServiceCollection services,
        Action<SchemaIsolationOptions>? configure = null)
    {
        var options = new SchemaIsolationOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<ITenantIsolationStrategy, SchemaIsolationStrategy>();

        return services;
    }

    /// <summary>
    /// Adds an in-memory tenant store for development/testing purposes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to pre-configure tenants.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryTenantStore(
        this IServiceCollection services,
        Action<InMemoryTenantStoreOptions>? configure = null)
    {
        var options = new InMemoryTenantStoreOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ITenantStore, InMemoryTenantStore>();

        return services;
    }
}

/// <summary>
/// Configuration options for multitenancy.
/// </summary>
public sealed class MultitenancyOptions
{
    /// <summary>
    /// Gets or sets the tenant propagation options.
    /// </summary>
    public TenantPropagationOptions Propagation { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether tenant context is required.
    /// When true, operations will fail if no tenant is set.
    /// </summary>
    public bool RequireTenant { get; set; }

    /// <summary>
    /// Gets or sets the default tenant ID to use when no tenant is resolved.
    /// </summary>
    public string? DefaultTenantId { get; set; }
}

/// <summary>
/// Options for the in-memory tenant store.
/// </summary>
public sealed class InMemoryTenantStoreOptions
{
    /// <summary>
    /// Gets or sets the initial tenants to add to the store.
    /// </summary>
    public List<TenantInfo> InitialTenants { get; set; } = new();
}
