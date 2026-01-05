// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Authentication;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Services;
using Microsoft.Extensions.DependencyInjection;
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
}
