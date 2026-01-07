// -----------------------------------------------------------------------
// <copyright file="TenantValidationExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Multitenancy;
using Compendium.Multitenancy.Stores;
using Microsoft.AspNetCore.Builder;

namespace Compendium.Adapters.AspNetCore.Security;

/// <summary>
/// Extension methods for configuring tenant validation.
/// </summary>
public static class TenantValidationExtensions
{
    /// <summary>
    /// Adds tenant validation services to the service collection.
    /// Includes tenant resolvers, consistency validator, and tenant context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureMiddleware">Optional middleware configuration.</param>
    /// <param name="configureConsistency">Optional consistency validation configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantValidation(
        this IServiceCollection services,
        Action<TenantValidationMiddlewareOptions>? configureMiddleware = null,
        Action<TenantConsistencyOptions>? configureConsistency = null)
    {
        // Configure options
        var middlewareOptions = new TenantValidationMiddlewareOptions();
        configureMiddleware?.Invoke(middlewareOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(middlewareOptions));

        var consistencyOptions = new TenantConsistencyOptions();
        configureConsistency?.Invoke(consistencyOptions);
        services.AddSingleton(consistencyOptions);

        // Register tenant context (scoped for per-request isolation)
        services.AddScoped<TenantContext>();

        // Register consistency validator
        services.AddSingleton<ITenantConsistencyValidator, TenantConsistencyValidator>();

        return services;
    }

    /// <summary>
    /// Adds tenant validation with an in-memory tenant store.
    /// Useful for development and testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="tenants">Initial tenants to seed.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantValidationWithInMemoryStore(
        this IServiceCollection services,
        params TenantInfo[] tenants)
    {
        services.AddTenantValidation();

        // Create and seed the in-memory store
        var store = new InMemoryTenantStore();
        foreach (var tenant in tenants)
        {
            store.SaveAsync(tenant).GetAwaiter().GetResult();
        }

        services.AddSingleton<ITenantStore>(store);

        return services;
    }

    /// <summary>
    /// Uses the tenant validation middleware in the application pipeline.
    /// Should be registered after authentication but before authorization.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// Typical pipeline order:
    /// 1. UseAuthentication()
    /// 2. UseTenantValidation() // This middleware
    /// 3. UseAuthorization()
    /// 4. MapControllers()
    /// </remarks>
    public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<TenantValidationMiddleware>();
    }
}
