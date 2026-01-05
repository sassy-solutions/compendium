// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Http;
using Compendium.Adapters.LemonSqueezy.Services;
using Compendium.Adapters.LemonSqueezy.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Compendium.Adapters.LemonSqueezy.DependencyInjection;

/// <summary>
/// Extension methods for registering LemonSqueezy adapter services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LemonSqueezy billing adapter services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for LemonSqueezy options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLemonSqueezy(
        this IServiceCollection services,
        Action<LemonSqueezyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Configure options
        services.Configure(configure);

        // Get options for HTTP client configuration
        var options = new LemonSqueezyOptions();
        configure(options);

        // Register HTTP client with resilience policies
        services.AddHttpClient<LemonSqueezyHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(options.MaxRetries))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register services
        services.AddScoped<IBillingService, LemonSqueezyBillingService>();
        services.AddScoped<ISubscriptionService, LemonSqueezySubscriptionService>();
        services.AddScoped<ILicenseService, LemonSqueezyLicenseService>();
        services.AddScoped<IPaymentWebhookHandler, LemonSqueezyWebhookHandler>();

        return services;
    }

    /// <summary>
    /// Adds LemonSqueezy billing adapter services to the service collection with configuration from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section containing LemonSqueezy options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLemonSqueezy(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        // Bind configuration
        services.Configure<LemonSqueezyOptions>(configurationSection);

        // Get options for HTTP client configuration
        var options = new LemonSqueezyOptions();
        configurationSection.Bind(options);

        // Register HTTP client with resilience policies
        services.AddHttpClient<LemonSqueezyHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(options.MaxRetries))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register services
        services.AddScoped<IBillingService, LemonSqueezyBillingService>();
        services.AddScoped<ISubscriptionService, LemonSqueezySubscriptionService>();
        services.AddScoped<ILicenseService, LemonSqueezyLicenseService>();
        services.AddScoped<IPaymentWebhookHandler, LemonSqueezyWebhookHandler>();

        return services;
    }

    /// <summary>
    /// Gets the retry policy for transient HTTP errors.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Logging is handled by the calling code
                });
    }

    /// <summary>
    /// Gets the circuit breaker policy for HTTP requests.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
