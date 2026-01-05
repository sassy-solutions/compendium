// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.Http;
using Compendium.Adapters.Listmonk.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Compendium.Adapters.Listmonk.DependencyInjection;

/// <summary>
/// Extension methods for registering Listmonk services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Listmonk email service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure Listmonk options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddListmonk(
        this IServiceCollection services,
        Action<ListmonkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return AddListmonkCore(services);
    }

    /// <summary>
    /// Adds Listmonk email service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Listmonk options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddListmonk(
        this IServiceCollection services,
        ListmonkOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.Configure<ListmonkOptions>(opt =>
        {
            opt.BaseUrl = options.BaseUrl;
            opt.Username = options.Username;
            opt.Password = options.Password;
            opt.DefaultFromEmail = options.DefaultFromEmail;
            opt.DefaultFromName = options.DefaultFromName;
            opt.DefaultListId = options.DefaultListId;
            opt.TimeoutSeconds = options.TimeoutSeconds;
            opt.MaxRetries = options.MaxRetries;
            opt.SkipSslValidation = options.SkipSslValidation;
        });

        return AddListmonkCore(services);
    }

    private static IServiceCollection AddListmonkCore(IServiceCollection services)
    {
        // Register HTTP client with resilience policies
        services.AddHttpClient<ListmonkHttpClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ListmonkOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ListmonkOptions>>().Value;
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
        services.AddScoped<IEmailService, ListmonkEmailService>();
        services.AddScoped<INewsletterService, ListmonkNewsletterService>();

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
