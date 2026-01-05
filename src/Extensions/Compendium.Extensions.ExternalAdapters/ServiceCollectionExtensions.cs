// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.DependencyInjection;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.DependencyInjection;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.DependencyInjection;
using Compendium.Multitenancy.Extensions;

namespace Compendium.Extensions.ExternalAdapters;

/// <summary>
/// Extension methods for registering all external adapter services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all configured external adapter services to the service collection.
    /// This provides a single entry point for configuring Zitadel, Listmonk, LemonSqueezy, and multitenancy.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure external adapter options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddCompendiumExternalAdapters(options =>
    /// {
    ///     options.EnableZitadel = true;
    ///     options.Zitadel = new ZitadelOptions
    ///     {
    ///         Authority = "https://zitadel.example.com",
    ///         ClientId = "your-client-id",
    ///         // ...
    ///     };
    ///
    ///     options.EnableListmonk = true;
    ///     options.Listmonk = new ListmonkOptions
    ///     {
    ///         BaseUrl = "https://listmonk.example.com",
    ///         // ...
    ///     };
    ///
    ///     options.EnableLemonSqueezy = true;
    ///     options.LemonSqueezy = new LemonSqueezyOptions
    ///     {
    ///         ApiKey = "your-api-key",
    ///         // ...
    ///     };
    ///
    ///     options.EnableMultitenancy = true;
    ///     options.Multitenancy = new MultitenancyOptions
    ///     {
    ///         RequireTenant = true
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddCompendiumExternalAdapters(
        this IServiceCollection services,
        Action<ExternalAdaptersOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ExternalAdaptersOptions();
        configure(options);

        // Register multitenancy first as other adapters may depend on tenant context
        if (options.EnableMultitenancy && options.Multitenancy is not null)
        {
            services.AddCompendiumMultitenancy(multitenancyOptions =>
            {
                multitenancyOptions.Propagation = options.Multitenancy.Propagation;
                multitenancyOptions.RequireTenant = options.Multitenancy.RequireTenant;
                multitenancyOptions.DefaultTenantId = options.Multitenancy.DefaultTenantId;
            });
        }

        // Register Zitadel identity provider
        if (options.EnableZitadel && options.Zitadel is not null)
        {
            services.AddZitadel(options.Zitadel);
        }

        // Register Listmonk email/newsletter service
        if (options.EnableListmonk && options.Listmonk is not null)
        {
            services.AddListmonk(options.Listmonk);
        }

        // Register LemonSqueezy billing service
        if (options.EnableLemonSqueezy && options.LemonSqueezy is not null)
        {
            services.AddLemonSqueezy(lsOptions =>
            {
                lsOptions.ApiKey = options.LemonSqueezy.ApiKey;
                lsOptions.StoreId = options.LemonSqueezy.StoreId;
                lsOptions.WebhookSigningSecret = options.LemonSqueezy.WebhookSigningSecret;
                lsOptions.BaseUrl = options.LemonSqueezy.BaseUrl;
                lsOptions.TimeoutSeconds = options.LemonSqueezy.TimeoutSeconds;
                lsOptions.MaxRetries = options.LemonSqueezy.MaxRetries;
                lsOptions.TestMode = options.LemonSqueezy.TestMode;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds all external adapter services from IConfiguration.
    /// Configuration is read from the "Compendium:ExternalAdapters" section by default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionPath">The configuration section path (default: "Compendium:ExternalAdapters").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // In appsettings.json:
    /// // {
    /// //   "Compendium": {
    /// //     "ExternalAdapters": {
    /// //       "EnableZitadel": true,
    /// //       "Zitadel": { ... },
    /// //       "EnableListmonk": true,
    /// //       "Listmonk": { ... },
    /// //       "EnableLemonSqueezy": true,
    /// //       "LemonSqueezy": { ... },
    /// //       "EnableMultitenancy": true,
    /// //       "Multitenancy": { ... }
    /// //     }
    /// //   }
    /// // }
    ///
    /// services.AddCompendiumExternalAdapters(configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddCompendiumExternalAdapters(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = ExternalAdaptersConfigurationSections.Root)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath);
        var options = new ExternalAdaptersOptions();
        section.Bind(options);

        // Bind nested sections
        var zitadelSection = section.GetSection(ExternalAdaptersConfigurationSections.Zitadel);
        if (zitadelSection.Exists() && options.EnableZitadel)
        {
            options.Zitadel = new ZitadelOptions();
            zitadelSection.Bind(options.Zitadel);
        }

        var listmonkSection = section.GetSection(ExternalAdaptersConfigurationSections.Listmonk);
        if (listmonkSection.Exists() && options.EnableListmonk)
        {
            options.Listmonk = new ListmonkOptions();
            listmonkSection.Bind(options.Listmonk);
        }

        var lemonSqueezySection = section.GetSection(ExternalAdaptersConfigurationSections.LemonSqueezy);
        if (lemonSqueezySection.Exists() && options.EnableLemonSqueezy)
        {
            options.LemonSqueezy = new LemonSqueezyOptions();
            lemonSqueezySection.Bind(options.LemonSqueezy);
        }

        var multitenancySection = section.GetSection(ExternalAdaptersConfigurationSections.Multitenancy);
        if (multitenancySection.Exists() && options.EnableMultitenancy)
        {
            options.Multitenancy = new MultitenancyOptions();
            multitenancySection.Bind(options.Multitenancy);
        }

        return services.AddCompendiumExternalAdapters(_ =>
        {
            _.EnableZitadel = options.EnableZitadel;
            _.Zitadel = options.Zitadel;
            _.EnableListmonk = options.EnableListmonk;
            _.Listmonk = options.Listmonk;
            _.EnableLemonSqueezy = options.EnableLemonSqueezy;
            _.LemonSqueezy = options.LemonSqueezy;
            _.EnableMultitenancy = options.EnableMultitenancy;
            _.Multitenancy = options.Multitenancy;
        });
    }

    /// <summary>
    /// Adds only Zitadel identity provider from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing Zitadel options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumZitadel(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ZitadelOptions();
        configuration.Bind(options);

        return services.AddZitadel(options);
    }

    /// <summary>
    /// Adds only Listmonk email service from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing Listmonk options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumListmonk(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ListmonkOptions();
        configuration.Bind(options);

        return services.AddListmonk(options);
    }

    /// <summary>
    /// Adds only LemonSqueezy billing service from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing LemonSqueezy options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumLemonSqueezy(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddLemonSqueezy(configuration);
    }
}
