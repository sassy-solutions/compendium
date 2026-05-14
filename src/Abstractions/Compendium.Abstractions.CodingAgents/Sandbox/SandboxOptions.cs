// -----------------------------------------------------------------------
// <copyright file="SandboxOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Sandbox;

/// <summary>
/// Configures how an <see cref="IAgentSandbox"/> is provisioned for a single
/// agent run. Adapter implementations interpret the kind-specific fields
/// (<see cref="Image"/>, <see cref="Namespace"/>, etc.) — fields that do not
/// apply to a given <see cref="SandboxKind"/> are ignored.
/// </summary>
public sealed record SandboxOptions
{
    /// <summary>
    /// Gets the sandbox kind to provision. Defaults to <see cref="SandboxKind.None"/>.
    /// </summary>
    public SandboxKind Kind { get; init; } = SandboxKind.None;

    /// <summary>
    /// Gets the working directory the agent should be rooted at inside the
    /// sandbox. For <see cref="SandboxKind.LocalProcess"/> this is a host path;
    /// for <see cref="SandboxKind.KubernetesPod"/> it is a path inside the pod.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the container/pod image to use. Only meaningful for
    /// <see cref="SandboxKind.KubernetesPod"/>.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// Gets the Kubernetes namespace the pod is provisioned in. Only meaningful
    /// for <see cref="SandboxKind.KubernetesPod"/>.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Gets environment variables seeded into the sandbox before the agent starts.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>
    /// Gets the maximum wall-clock time a single sandbox command may run.
    /// Implementations should enforce this via SIGTERM/SIGKILL.
    /// </summary>
    public TimeSpan? CommandTimeout { get; init; }

    /// <summary>
    /// Gets a tenant identifier the sandbox should be scoped to (used for
    /// quota, network policy, and audit). Optional.
    /// </summary>
    public string? TenantId { get; init; }
}
