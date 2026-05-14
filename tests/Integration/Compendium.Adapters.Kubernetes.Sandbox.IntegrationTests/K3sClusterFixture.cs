// -----------------------------------------------------------------------
// <copyright file="K3sClusterFixture.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using k8s;
using Testcontainers.K3s;
using Xunit;

namespace Compendium.Adapters.Kubernetes.Sandbox.IntegrationTests;

/// <summary>
/// Spins up a single-node k3s cluster via Testcontainers and yields an
/// <see cref="IKubernetes"/> client pointed at it. Shared across the assembly's
/// integration test classes so the (slow) cluster start cost amortizes.
/// </summary>
public sealed class K3sClusterFixture : IAsyncLifetime
{
    private K3sContainer? _container;

    public IKubernetes Client { get; private set; } = null!;

    public string Kubeconfig { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new K3sBuilder()
                .WithImage("rancher/k3s:v1.31.1-k3s1")
                .WithCommand("--disable", "traefik,servicelb,metrics-server")
                .Build();

            await _container.StartAsync().ConfigureAwait(false);
            Kubeconfig = await _container.GetKubeconfigAsync().ConfigureAwait(false);

            var config = KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Kubeconfig)))
                .GetAwaiter().GetResult();
            Client = new k8s.Kubernetes(config);
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            // Docker not available, image pull failed, etc. — integration tests skip.
            UnavailableReason = ex.Message;
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (Client is IDisposable d)
        {
            d.Dispose();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

[CollectionDefinition(Name)]
public sealed class K3sClusterCollection : ICollectionFixture<K3sClusterFixture>
{
    public const string Name = "k3s-cluster";
}
