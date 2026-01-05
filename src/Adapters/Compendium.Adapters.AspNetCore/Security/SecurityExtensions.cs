// -----------------------------------------------------------------------
// <copyright file="SecurityExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Compendium.Adapters.AspNetCore.Security;

/// <summary>
/// Extension methods for configuring security features in ASP.NET Core applications.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// The default CORS policy name for Compendium applications.
    /// </summary>
    public const string DefaultCorsPolicyName = "CompendiumCorsPolicy";

    /// <summary>
    /// Adds security headers middleware with the specified options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumSecurityHeaders(
        this IServiceCollection services,
        Action<SecurityHeadersOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<SecurityHeadersOptions>(options =>
        {
            // Apply default configuration
            var defaults = SecurityHeadersOptions.ForApi();
            options.EnableHsts = defaults.EnableHsts;
            options.HstsMaxAgeSeconds = defaults.HstsMaxAgeSeconds;
            options.HstsIncludeSubDomains = defaults.HstsIncludeSubDomains;
            options.EnableNoSniff = defaults.EnableNoSniff;
            options.EnableFrameOptions = defaults.EnableFrameOptions;
            options.FrameOptionsValue = defaults.FrameOptionsValue;
            options.EnableContentSecurityPolicy = defaults.EnableContentSecurityPolicy;
            options.ContentSecurityPolicy = defaults.ContentSecurityPolicy;
            options.RemoveServerHeader = defaults.RemoveServerHeader;
            options.RemoveXPoweredByHeader = defaults.RemoveXPoweredByHeader;
            options.EnablePermittedCrossDomainPolicies = defaults.EnablePermittedCrossDomainPolicies;
            options.PermittedCrossDomainPoliciesValue = defaults.PermittedCrossDomainPoliciesValue;
            options.EnableReferrerPolicy = defaults.EnableReferrerPolicy;
            options.ReferrerPolicyValue = defaults.ReferrerPolicyValue;
            options.EnablePermissionsPolicy = defaults.EnablePermissionsPolicy;
            options.PermissionsPolicyValue = defaults.PermissionsPolicyValue;

            // Apply custom configuration
            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// Adds strict CORS policy for Compendium applications.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration delegate for CORS policy.</param>
    /// <param name="policyName">Optional custom policy name. Defaults to "CompendiumCorsPolicy".</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Default policy:
    /// - No origins allowed (must be explicitly configured)
    /// - Only specified HTTP methods allowed
    /// - Only specified headers allowed
    /// - Credentials not allowed by default
    /// </remarks>
    public static IServiceCollection AddCompendiumCors(
        this IServiceCollection services,
        Action<CorsPolicyBuilder> configure,
        string policyName = DefaultCorsPolicyName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                // Apply custom configuration
                configure(builder);
            });
        });

        return services;
    }

    /// <summary>
    /// Adds a strict CORS policy for API scenarios with specific allowed origins.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="allowedOrigins">The allowed origins.</param>
    /// <param name="policyName">Optional custom policy name.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumStrictCors(
        this IServiceCollection services,
        string[] allowedOrigins,
        string policyName = DefaultCorsPolicyName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(allowedOrigins);

        if (allowedOrigins.Length == 0)
        {
            throw new ArgumentException("At least one allowed origin must be specified.", nameof(allowedOrigins));
        }

        return services.AddCompendiumCors(builder =>
        {
            builder
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .AllowCredentials();
        }, policyName);
    }

    /// <summary>
    /// Uses the security headers middleware in the application pipeline.
    /// Should be registered early in the pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCompendiumSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Uses HSTS (HTTP Strict Transport Security) middleware.
    /// Should only be used in production environments.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="maxAgeInSeconds">The max age in seconds. Default: 31536000 (1 year).</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// This is a convenience method that wraps ASP.NET Core's built-in HSTS middleware.
    /// For more control, configure HSTS via SecurityHeadersOptions instead.
    /// </remarks>
    public static IApplicationBuilder UseCompendiumHsts(
        this IApplicationBuilder app,
        int maxAgeInSeconds = 31536000)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseHsts();
    }

    /// <summary>
    /// Uses the Compendium CORS policy.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="policyName">Optional custom policy name. Defaults to "CompendiumCorsPolicy".</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCompendiumCors(
        this IApplicationBuilder app,
        string policyName = DefaultCorsPolicyName)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseCors(policyName);
    }
}
