// -----------------------------------------------------------------------
// <copyright file="KubernetesSandboxOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Kubernetes.Sandbox;

/// <summary>
/// Adapter-level defaults for <see cref="KubernetesAgentSandbox"/>. These are
/// the values used when the per-run <see cref="Compendium.Abstractions.CodingAgents.Sandbox.SandboxOptions"/>
/// does not override them, and the values that govern adapter behavior that is
/// not represented on the neutral port (RBAC service account, resource limits,
/// network policy, PVC).
/// </summary>
/// <remarks>
/// All quantities are CPU/memory strings in the standard Kubernetes resource
/// format (<c>500m</c>, <c>1Gi</c>, ...). Validation is deferred to the API
/// server.
/// </remarks>
public sealed class KubernetesSandboxOptions
{
    /// <summary>
    /// Configuration section name used with <c>IConfiguration.GetSection</c>.
    /// </summary>
    public const string SectionName = "Compendium:CodingAgents:KubernetesSandbox";

    /// <summary>
    /// Gets or sets the OCI image to run inside the sandbox pod. Should be a
    /// pinned digest in production.
    /// </summary>
    public string Image { get; set; } = "ghcr.io/sassy-solutions/compendium/coding-agent-sandbox:latest";

    /// <summary>
    /// Gets or sets the default namespace pods are provisioned in when the per-run
    /// <see cref="Compendium.Abstractions.CodingAgents.Sandbox.SandboxOptions.Namespace"/>
    /// is null.
    /// </summary>
    public string Namespace { get; set; } = "coding-agents";

    /// <summary>
    /// Gets or sets the service account name pods run as. Should have RBAC scoped
    /// to only what the sandbox requires (typically no cluster permissions at all).
    /// </summary>
    public string ServiceAccountName { get; set; } = "coding-agent-sandbox";

    /// <summary>
    /// Gets or sets a prefix used for generated pod names. Pod names are derived as
    /// <c>{PodNamePrefix}-{shortGuid}</c>.
    /// </summary>
    public string PodNamePrefix { get; set; } = "coding-agent";

    /// <summary>
    /// Gets or sets the container name inside the pod. Used by <c>exec</c> calls.
    /// </summary>
    public string ContainerName { get; set; } = "agent";

    /// <summary>
    /// Gets or sets the working directory inside the container the agent is rooted at.
    /// Overridable by <see cref="Compendium.Abstractions.CodingAgents.Sandbox.SandboxOptions.WorkingDirectory"/>.
    /// </summary>
    public string WorkingDirectory { get; set; } = "/workspace";

    /// <summary>
    /// Gets or sets the CPU request for the sandbox container.
    /// </summary>
    public string CpuRequest { get; set; } = "100m";

    /// <summary>
    /// Gets or sets the CPU limit for the sandbox container.
    /// </summary>
    public string CpuLimit { get; set; } = "1";

    /// <summary>
    /// Gets or sets the memory request for the sandbox container.
    /// </summary>
    public string MemoryRequest { get; set; } = "256Mi";

    /// <summary>
    /// Gets or sets the memory limit for the sandbox container.
    /// </summary>
    public string MemoryLimit { get; set; } = "1Gi";

    /// <summary>
    /// Gets or sets the storage request for the ephemeral workspace volume. When
    /// null, an in-memory <c>emptyDir</c> backs the workspace; when set, an
    /// ephemeral generic PVC is used so the workspace survives container
    /// restarts within the pod's lifetime.
    /// </summary>
    public string? WorkspaceStorageRequest { get; set; }

    /// <summary>
    /// Gets or sets the storage class name backing the ephemeral PVC, when
    /// <see cref="WorkspaceStorageRequest"/> is set. Null means "cluster default".
    /// </summary>
    public string? WorkspaceStorageClassName { get; set; }

    /// <summary>
    /// Gets or sets the maximum wall-clock duration any single <c>exec</c> may
    /// run for before the adapter terminates the channel.
    /// </summary>
    public TimeSpan DefaultCommandTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the maximum time the adapter waits for the pod to reach
    /// <c>Running</c> after creation, before failing <c>StartAsync</c>.
    /// </summary>
    public TimeSpan PodReadyTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the grace period passed to the API server when deleting the
    /// pod on dispose. Zero forces immediate deletion.
    /// </summary>
    public int DeleteGracePeriodSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the UID the container runs as (non-root). Defaults to
    /// <c>10001</c> to match the bundled <c>coding-agent-sandbox</c> image.
    /// </summary>
    public long RunAsUser { get; set; } = 10001;

    /// <summary>
    /// Gets or sets the GID the container runs as.
    /// </summary>
    public long RunAsGroup { get; set; } = 10001;

    /// <summary>
    /// Gets or sets a value indicating whether the root filesystem inside the
    /// container is read-only. Defaults to <see langword="true"/>; the writable
    /// workspace is mounted at <see cref="WorkingDirectory"/>.
    /// </summary>
    public bool ReadOnlyRootFilesystem { get; set; } = true;

    /// <summary>
    /// Gets or sets additional labels applied to every pod created by this adapter.
    /// </summary>
    public IReadOnlyDictionary<string, string>? PodLabels { get; set; }

    /// <summary>
    /// Gets or sets additional annotations applied to every pod created by this adapter.
    /// </summary>
    public IReadOnlyDictionary<string, string>? PodAnnotations { get; set; }

    /// <summary>
    /// Gets or sets the kubeconfig path the client falls back to when the
    /// process is not running inside a Kubernetes cluster (e.g. local dev). Null
    /// means "use in-cluster config, then default discovery".
    /// </summary>
    public string? KubeConfigPath { get; set; }
}
