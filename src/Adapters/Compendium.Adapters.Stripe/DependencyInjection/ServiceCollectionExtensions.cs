// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Stripe.Configuration;
using Compendium.Adapters.Stripe.Services;
using Compendium.Adapters.Stripe.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Stripe.DependencyInjection;

/// <summary>
/// Extension methods for registering the Stripe billing adapter services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Stripe billing adapter to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for <see cref="StripeOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStripeAdapter(
        this IServiceCollection services,
        Action<StripeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        var options = new StripeOptions();
        configure(options);

        ApplyGlobalStripeConfiguration(options);
        RegisterAdapterServices(services);

        return services;
    }

    /// <summary>
    /// Adds the Stripe billing adapter to the service collection using configuration
    /// bound from the supplied section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section containing Stripe options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStripeAdapter(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.Configure<StripeOptions>(configurationSection);

        var options = new StripeOptions();
        configurationSection.Bind(options);

        ApplyGlobalStripeConfiguration(options);
        RegisterAdapterServices(services);

        return services;
    }

    private static void ApplyGlobalStripeConfiguration(StripeOptions options)
    {
        if (!string.IsNullOrEmpty(options.SecretKey))
        {
            StripeConfiguration.ApiKey = options.SecretKey;
        }

        // ApiVersion in Stripe.net is a compile-time constant set by the SDK package
        // version. Pinning a specific API version is done via a custom StripeClient,
        // which is out of scope here. The StripeOptions.ApiVersion property is kept
        // for consumers to inspect; the effective version is governed by the SDK.
    }

    private static void RegisterAdapterServices(IServiceCollection services)
    {
        services.AddScoped<IBillingService, StripeBillingService>();
        services.AddScoped<ISubscriptionService, StripeSubscriptionService>();
        services.AddScoped<IPaymentWebhookHandler, StripeWebhookHandler>();
    }
}
