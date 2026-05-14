// -----------------------------------------------------------------------
// <copyright file="KubernetesSandboxFactory.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.CodingAgents.Sandbox;
using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Kubernetes.Sandbox;

/// <summary>
/// Default implementation of <see cref="IKubernetesAgentSandboxFactory"/> that
/// reuses a single <see cref="IKubernetes"/> client across every sandbox it
/// produces (the client is thread-safe and connection-pooled).
/// </summary>
public sealed class KubernetesSandboxFactory : IKubernetesAgentSandboxFactory
{
    private readonly IKubernetes _client;
    private readonly KubernetesSandboxOptions _options;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesSandboxFactory"/> class.
    /// </summary>
    /// <param name="client">The shared Kubernetes API client.</param>
    /// <param name="options">The adapter options.</param>
    /// <param name="loggerFactory">A logger factory for per-sandbox loggers.</param>
    public KubernetesSandboxFactory(
        IKubernetes client,
        IOptions<KubernetesSandboxOptions> options,
        ILoggerFactory loggerFactory)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IAgentSandbox Create()
    {
        return new KubernetesAgentSandbox(
            _client,
            _options,
            _loggerFactory.CreateLogger<KubernetesAgentSandbox>(),
            ownsClient: false);
    }
}
