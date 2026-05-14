// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.ClaudeCode.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.ClaudeCode.DependencyInjection;

/// <summary>
/// DI extensions for registering the Claude Code coding-agent adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ClaudeCodeRuntime"/> as a singleton
    /// <see cref="ICodingAgentRuntime"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClaudeCodeRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ICodingAgentRuntime, ClaudeCodeRuntime>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="ClaudeCodeRuntime"/> with a custom sandbox factory
    /// (e.g. a Kubernetes-pod sandbox injected by the host).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sandboxFactory">Factory invoked once per run.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClaudeCodeRuntime(
        this IServiceCollection services,
        Func<IServiceProvider, CodingAgentRuntimeOptions, IAgentSandbox> sandboxFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sandboxFactory);
        services.AddSingleton<ICodingAgentRuntime>(sp =>
            new ClaudeCodeRuntime(options => sandboxFactory(sp, options)));
        return services;
    }
}
