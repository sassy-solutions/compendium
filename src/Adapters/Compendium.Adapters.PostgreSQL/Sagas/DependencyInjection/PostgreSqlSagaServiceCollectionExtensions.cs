// -----------------------------------------------------------------------
// <copyright file="PostgreSqlSagaServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Adapters.PostgreSQL.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Compendium.Adapters.PostgreSQL.Sagas.DependencyInjection;

/// <summary>
/// DI helpers to wire the PostgreSQL <see cref="IProcessManagerRepository"/> implementation.
/// </summary>
public static class PostgreSqlSagaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PostgreSQL process-manager repository, replacing any previous
    /// registration of <see cref="IProcessManagerRepository"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationAction">Optional configuration delegate; if omitted, options are expected to be configured already.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProcessManagerRepository(
        this IServiceCollection services,
        Action<PostgreSqlOptions>? configurationAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configurationAction != null)
        {
            services.Configure(configurationAction);
        }

        // Use Replace so this overrides the in-memory default that AddProcessManagers() registers.
        services.RemoveAll<IProcessManagerRepository>();
        services.AddSingleton<IProcessManagerRepository, PostgresProcessManagerRepository>();
        return services;
    }
}
