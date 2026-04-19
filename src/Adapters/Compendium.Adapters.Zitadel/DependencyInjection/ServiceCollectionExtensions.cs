// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity;
using Compendium.Adapters.Zitadel.Authentication;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Health;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Extensions.Http;

namespace Compendium.Adapters.Zitadel.DependencyInjection;

/// <summary>
/// Extension methods for registering Zitadel services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Zitadel identity provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure Zitadel options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddZitadel(
        this IServiceCollection services,
        Action<ZitadelOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return AddZitadelCore(services);
    }

    /// <summary>
    /// Adds Zitadel identity provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Zitadel options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddZitadel(
        this IServiceCollection services,
        ZitadelOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.Configure<ZitadelOptions>(opt =>
        {
            opt.Authority = options.Authority;
            opt.ClientId = options.ClientId;
            opt.ClientSecret = options.ClientSecret;
            opt.ServiceAccountKeyJson = options.ServiceAccountKeyJson;
            opt.ServiceAccountKeyPath = options.ServiceAccountKeyPath;
            opt.ProjectId = options.ProjectId;
            opt.DefaultOrganizationId = options.DefaultOrganizationId;
            opt.TimeoutSeconds = options.TimeoutSeconds;
            opt.MaxRetries = options.MaxRetries;
            opt.InternalBaseUrl = options.InternalBaseUrl;
            opt.SkipSslValidation = options.SkipSslValidation;
        });

        return AddZitadelCore(services);
    }

    private static IServiceCollection AddZitadelCore(IServiceCollection services)
    {
        // Register HTTP client with resilience policies
        services.AddHttpClient<ZitadelHttpClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ZitadelOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ZitadelOptions>>().Value;
                var handler = new HttpClientHandler();

                if (options.SkipSslValidation)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register services
        services.AddSingleton<ZitadelClaimsTransformation>();
        services.AddScoped<IIdentityUserService, ZitadelUserService>();
        services.AddScoped<ITokenValidator, ZitadelTokenValidator>();
        services.AddScoped<IOrganizationService, ZitadelOrganizationService>();
        services.AddScoped<IOrganizationIdentityProvisioner, ZitadelOrganizationIdentityProvisioner>();
        services.AddScoped<IProjectIdentityProvisioner, ZitadelProjectIdentityProvisioner>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Adds the Zitadel health check to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name (default: "zitadel").</param>
    /// <param name="failureStatus">
    /// The health status to report when the check fails (default: Unhealthy).
    /// </param>
    /// <param name="tags">Tags to apply to the health check.</param>
    /// <param name="timeout">Timeout for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddZitadelHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "zitadel",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHttpClient<ZitadelHealthCheck>()
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ZitadelOptions>>().Value;
                var handler = new HttpClientHandler();

                if (options.SkipSslValidation)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

        return builder.AddCheck<ZitadelHealthCheck>(
            name,
            failureStatus,
            tags ?? new[] { "ready", "identity" },
            timeout);
    }
}
