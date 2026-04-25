// -----------------------------------------------------------------------
// <copyright file="SagaServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Application.Sagas.Choreography;
using Compendium.Application.Sagas.ProcessManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Compendium.Application.Sagas.DependencyInjection;

/// <summary>
/// DI registration helpers for both saga flavors.
/// </summary>
public static class SagaServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IProcessManagerOrchestrator"/> and a default in-memory
    /// repository. Adapters (e.g. PostgreSQL) typically replace the repository
    /// registration after this call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddProcessManagers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IProcessManagerRepository, InMemoryProcessManagerRepository>();
        services.TryAddScoped<IProcessManagerOrchestrator, ProcessManagerOrchestrator>();
        return services;
    }

    /// <summary>
    /// Registers the choreography router, an in-memory publisher, and scans the supplied
    /// assemblies for <see cref="IHandle{TEvent}"/> implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handlerAssemblies">
    /// Assemblies to scan for choreography handlers. If empty, only the entry assembly
    /// is scanned.
    /// </param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddEventChoreography(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IChoreographyRouter, ChoreographyRouter>();

        // Default publisher: in-memory, with deferred router lookup so events fan out in-process.
        services.TryAddSingleton<IIntegrationEventPublisher>(sp =>
            new InMemoryIntegrationEventPublisher(() => sp.GetService<IChoreographyRouter>()));

        var assemblies = handlerAssemblies is { Length: > 0 }
            ? handlerAssemblies
            : new[] { Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() };

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IHandle<>))
                {
                    services.AddTransient(iface, type);
                }
            }
        }
    }
}
