// -----------------------------------------------------------------------
// <copyright file="HttpClientBuilderExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Multitenancy.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Multitenancy.Extensions;

/// <summary>
/// Extension methods for adding tenant propagation to HTTP clients.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds tenant context propagation to the HTTP client.
    /// This will add tenant headers to all outgoing requests.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddTenantPropagation(this IHttpClientBuilder builder)
    {
        return builder.AddTenantPropagation(_ => { });
    }

    /// <summary>
    /// Adds tenant context propagation to the HTTP client with configuration.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">The configuration action for propagation options.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddTenantPropagation(
        this IHttpClientBuilder builder,
        Action<TenantPropagationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TenantPropagationOptions();
        configure(options);

        builder.Services.AddSingleton(options);

        builder.AddHttpMessageHandler(sp =>
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            var opts = sp.GetRequiredService<TenantPropagationOptions>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TenantPropagatingDelegatingHandler>>();
            return new TenantPropagatingDelegatingHandler(accessor, opts, logger);
        });

        return builder;
    }
}
