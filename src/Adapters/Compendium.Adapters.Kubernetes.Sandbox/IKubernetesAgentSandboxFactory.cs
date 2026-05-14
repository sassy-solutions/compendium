// -----------------------------------------------------------------------
// <copyright file="IKubernetesAgentSandboxFactory.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.CodingAgents.Sandbox;

namespace Compendium.Adapters.Kubernetes.Sandbox;

/// <summary>
/// Produces fresh <see cref="KubernetesAgentSandbox"/> instances on demand. Each
/// call returns an un-started sandbox; the caller is responsible for invoking
/// <see cref="IAgentSandbox.StartAsync"/> and <see cref="IAsyncDisposable.DisposeAsync"/>.
/// </summary>
public interface IKubernetesAgentSandboxFactory
{
    /// <summary>
    /// Creates a new sandbox bound to the supplied adapter options. Per-run
    /// values (image override, namespace override, etc.) are passed to
    /// <see cref="IAgentSandbox.StartAsync"/> via <see cref="SandboxOptions"/>.
    /// </summary>
    /// <returns>A new sandbox instance.</returns>
    IAgentSandbox Create();
}
