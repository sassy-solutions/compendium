// -----------------------------------------------------------------------
// <copyright file="DependencyInjectionTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Kubernetes.Sandbox.DependencyInjection;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.Kubernetes.Sandbox.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddKubernetesAgentSandbox_registers_factory_and_options()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Pre-register a fake IKubernetes so DI doesn't try to read kubeconfig.
        services.AddSingleton(Substitute.For<IKubernetes>());

        services.AddKubernetesAgentSandbox(o =>
        {
            o.Namespace = "tenant-ns";
            o.Image = "ghcr.io/x/y:1";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<KubernetesSandboxOptions>>().Value;
        options.Namespace.Should().Be("tenant-ns");
        options.Image.Should().Be("ghcr.io/x/y:1");

        var factory = sp.GetRequiredService<IKubernetesAgentSandboxFactory>();
        factory.Should().BeOfType<KubernetesSandboxFactory>();
    }

    [Fact]
    public void AddKubernetesAgentSandbox_reuses_existing_IKubernetes_registration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var preExisting = Substitute.For<IKubernetes>();
        services.AddSingleton(preExisting);

        services.AddKubernetesAgentSandbox();

        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IKubernetes>().Should().BeSameAs(preExisting);
    }

    [Fact]
    public void Factory_Create_returns_a_fresh_sandbox_per_call()
    {
        var client = Substitute.For<IKubernetes>();
        var factory = new KubernetesSandboxFactory(
            client,
            Options.Create(new KubernetesSandboxOptions()),
            new LoggerFactory());

        var s1 = factory.Create();
        var s2 = factory.Create();

        s1.Should().NotBeSameAs(s2);
        s1.Kind.Should().Be(SandboxKind.KubernetesPod);
    }
}
