// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.OpenRouter.Configuration;
using Compendium.Adapters.OpenRouter.Http;
using Compendium.Adapters.OpenRouter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.OpenRouter.DependencyInjection;

/// <summary>
/// Extension methods for configuring OpenRouter services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenRouter AI provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenRouter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenRouterOptions>(
            configuration.GetSection(OpenRouterOptions.SectionName));

        return services.AddOpenRouterCore();
    }

    /// <summary>
    /// Adds OpenRouter AI provider services with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenRouter(
        this IServiceCollection services,
        Action<OpenRouterOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddOpenRouterCore();
    }

    private static IServiceCollection AddOpenRouterCore(this IServiceCollection services)
    {
        // Register HTTP client with resilience
        services.AddHttpClient<OpenRouterHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        // Register services
        services.AddSingleton<OpenRouterAIProvider>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<OpenRouterAIProvider>());

        return services;
    }
}
