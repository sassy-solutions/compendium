// -----------------------------------------------------------------------
// <copyright file="PodSpecFactoryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using k8s.Models;

namespace Compendium.Adapters.Kubernetes.Sandbox.Tests;

public class PodSpecFactoryTests
{
    private static KubernetesSandboxOptions DefaultAdapterOptions() => new()
    {
        Image = "ghcr.io/example/agent:1.0.0",
        Namespace = "coding-agents",
        ServiceAccountName = "coding-agent-sandbox",
        WorkingDirectory = "/workspace",
        RunAsUser = 10001,
        RunAsGroup = 10001,
    };

    [Fact]
    public void Build_uses_per_run_overrides_when_provided()
    {
        var adapter = DefaultAdapterOptions();
        var run = new SandboxOptions
        {
            Kind = SandboxKind.KubernetesPod,
            Image = "ghcr.io/example/agent:override",
            Namespace = "tenant-acme",
            WorkingDirectory = "/work",
            TenantId = "acme",
            Environment = new Dictionary<string, string> { ["FOO"] = "bar" },
        };

        var pod = PodSpecFactory.Build("agent-abc", adapter, run);

        pod.Metadata!.Name.Should().Be("agent-abc");
        pod.Metadata.NamespaceProperty.Should().Be("tenant-acme");
        pod.Metadata.Labels.Should().Contain(new KeyValuePair<string, string>("compendium.io/tenant", "acme"));
        pod.Metadata.Labels.Should().Contain(new KeyValuePair<string, string>("app.kubernetes.io/managed-by", "compendium"));

        var container = pod.Spec.Containers.Single();
        container.Image.Should().Be("ghcr.io/example/agent:override");
        container.WorkingDir.Should().Be("/work");
        container.Env.Should().ContainSingle(e => e.Name == "FOO" && e.Value == "bar");
    }

    [Fact]
    public void Build_falls_back_to_adapter_defaults_when_run_options_are_empty()
    {
        var adapter = DefaultAdapterOptions();
        var run = new SandboxOptions { Kind = SandboxKind.KubernetesPod };

        var pod = PodSpecFactory.Build("agent-default", adapter, run);

        pod.Metadata!.NamespaceProperty.Should().Be("coding-agents");
        pod.Spec.Containers.Single().Image.Should().Be(adapter.Image);
        pod.Spec.Containers.Single().WorkingDir.Should().Be("/workspace");
        pod.Spec.ServiceAccountName.Should().Be("coding-agent-sandbox");
    }

    [Fact]
    public void Build_enforces_non_root_security_context()
    {
        var pod = PodSpecFactory.Build("agent-sec", DefaultAdapterOptions(), new SandboxOptions());

        var container = pod.Spec.Containers.Single();
        var sec = container.SecurityContext;
        sec.Should().NotBeNull();
        sec!.RunAsNonRoot.Should().BeTrue();
        sec.AllowPrivilegeEscalation.Should().BeFalse();
        sec.Privileged.Should().BeFalse();
        sec.ReadOnlyRootFilesystem.Should().BeTrue();
        sec.Capabilities!.Drop.Should().Contain("ALL");

        var podSec = pod.Spec.SecurityContext;
        podSec.Should().NotBeNull();
        podSec!.RunAsNonRoot.Should().BeTrue();
        podSec.RunAsUser.Should().Be(10001);
        podSec.SeccompProfile!.Type.Should().Be("RuntimeDefault");
    }

    [Fact]
    public void Build_disables_service_account_token_automount()
    {
        var pod = PodSpecFactory.Build("agent-x", DefaultAdapterOptions(), new SandboxOptions());

        pod.Spec.AutomountServiceAccountToken.Should().BeFalse();
        pod.Spec.RestartPolicy.Should().Be("Never");
        pod.Spec.EnableServiceLinks.Should().BeFalse();
    }

    [Fact]
    public void Build_uses_emptyDir_volume_by_default()
    {
        var pod = PodSpecFactory.Build("agent-vol", DefaultAdapterOptions(), new SandboxOptions());

        var volume = pod.Spec.Volumes.Single();
        volume.Name.Should().Be(PodSpecFactory.WorkspaceVolumeName);
        volume.EmptyDir.Should().NotBeNull();
        volume.Ephemeral.Should().BeNull();
    }

    [Fact]
    public void Build_uses_ephemeral_pvc_volume_when_storage_request_set()
    {
        var adapter = DefaultAdapterOptions();
        adapter.WorkspaceStorageRequest = "1Gi";
        adapter.WorkspaceStorageClassName = "fast";

        var pod = PodSpecFactory.Build("agent-pvc", adapter, new SandboxOptions());

        var volume = pod.Spec.Volumes.Single();
        volume.Ephemeral.Should().NotBeNull();
        volume.EmptyDir.Should().BeNull();
        var spec = volume.Ephemeral!.VolumeClaimTemplate.Spec;
        spec.StorageClassName.Should().Be("fast");
        spec.Resources.Requests["storage"].ToString().Should().Be("1Gi");
    }

    [Fact]
    public void Build_applies_resource_limits_from_adapter_options()
    {
        var adapter = DefaultAdapterOptions();
        adapter.CpuLimit = "2";
        adapter.MemoryLimit = "2Gi";
        adapter.CpuRequest = "250m";
        adapter.MemoryRequest = "512Mi";

        var pod = PodSpecFactory.Build("agent-res", adapter, new SandboxOptions());

        var resources = pod.Spec.Containers.Single().Resources;
        resources.Limits["cpu"].ToString().Should().Be("2");
        resources.Limits["memory"].ToString().Should().Be("2Gi");
        resources.Requests["cpu"].ToString().Should().Be("250m");
        resources.Requests["memory"].ToString().Should().Be("512Mi");
    }

    [Fact]
    public void Build_quotes_workingDir_safely_in_init_command()
    {
        var adapter = DefaultAdapterOptions();
        adapter.WorkingDirectory = "/work'space";

        var pod = PodSpecFactory.Build("agent-quote", adapter, new SandboxOptions());

        var args = pod.Spec.Containers.Single().Args.Single();
        args.Should().Contain("'/work'\\''space'");
        args.Should().Contain("exec sleep infinity");
    }

    [Fact]
    public void Build_merges_pod_labels_and_annotations()
    {
        var adapter = DefaultAdapterOptions();
        adapter.PodLabels = new Dictionary<string, string> { ["team"] = "platform" };
        adapter.PodAnnotations = new Dictionary<string, string> { ["audit"] = "yes" };

        var pod = PodSpecFactory.Build("agent-meta", adapter, new SandboxOptions());

        pod.Metadata!.Labels.Should().Contain(new KeyValuePair<string, string>("team", "platform"));
        pod.Metadata.Annotations.Should().Contain(new KeyValuePair<string, string>("audit", "yes"));
    }
}
