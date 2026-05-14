// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Kubernetes.Sandbox.DependencyInjection;

/// <summary>
/// DI helpers for <see cref="KubernetesAgentSandbox"/>. Consumers register the
/// adapter once via <see cref="AddKubernetesAgentSandbox"/>; downstream coding-agent
/// runtimes resolve <see cref="IKubernetesAgentSandboxFactory"/> to mint a fresh
/// sandbox per run.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Kubernetes sandbox adapter with the supplied options. A
    /// singleton <see cref="IKubernetes"/> client is registered only if one is
    /// not already present, so callers who provision their own client (e.g.
    /// from a multi-tenant namespace provisioning module) can reuse it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional callback to configure adapter options.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddKubernetesAgentSandbox(
        this IServiceCollection services,
        Action<KubernetesSandboxOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<KubernetesSandboxOptions>();
        }

        services.TryAddSingleton<IKubernetes>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<KubernetesSandboxOptions>>().Value;
            var config = BuildClientConfiguration(opts);
            return new k8s.Kubernetes(config);
        });

        services.TryAddSingleton<IKubernetesAgentSandboxFactory, KubernetesSandboxFactory>();

        return services;
    }

    private static KubernetesClientConfiguration BuildClientConfiguration(KubernetesSandboxOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.KubeConfigPath))
        {
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(options.KubeConfigPath);
        }

        if (KubernetesClientConfiguration.IsInCluster())
        {
            return KubernetesClientConfiguration.InClusterConfig();
        }

        return KubernetesClientConfiguration.BuildDefaultConfig();
    }
}
