// -----------------------------------------------------------------------
// <copyright file="SandboxKind.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Sandbox;

/// <summary>
/// Identifies how an <see cref="IAgentSandbox"/> isolates the coding agent's
/// filesystem and process tree.
/// </summary>
public enum SandboxKind
{
    /// <summary>
    /// No sandbox — the agent runs directly on the host. Use only for tests
    /// or trusted local development.
    /// </summary>
    None = 0,

    /// <summary>
    /// The agent runs as a child process on the host, typically rooted at
    /// a dedicated working directory. Provides file-system scoping but no
    /// kernel-level isolation.
    /// </summary>
    LocalProcess = 1,

    /// <summary>
    /// The agent runs inside a Kubernetes pod. The runtime is responsible
    /// for provisioning, exec'ing into, and tearing down the pod.
    /// </summary>
    KubernetesPod = 2,
}
