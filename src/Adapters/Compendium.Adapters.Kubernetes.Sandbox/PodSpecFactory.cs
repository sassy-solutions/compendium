// -----------------------------------------------------------------------
// <copyright file="PodSpecFactory.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Compendium.Abstractions.CodingAgents.Sandbox;
using k8s.Models;

namespace Compendium.Adapters.Kubernetes.Sandbox;

/// <summary>
/// Pure pod-spec builder, extracted from <see cref="KubernetesAgentSandbox"/>
/// so it can be unit tested without a live cluster. The builder reconciles
/// adapter defaults (<see cref="KubernetesSandboxOptions"/>) with the per-run
/// neutral options (<see cref="SandboxOptions"/>) according to these rules:
/// <list type="bullet">
///   <item>per-run image, namespace, working directory and tenant override the adapter defaults;</item>
///   <item>per-run environment is appended to the container (sandbox env is treated as non-secret);</item>
///   <item>resource limits, security context, RBAC and volumes always come from adapter options.</item>
/// </list>
/// </summary>
internal static class PodSpecFactory
{
    internal const string WorkspaceVolumeName = "workspace";
    internal const string TenantLabel = "compendium.io/tenant";
    internal const string ComponentLabel = "compendium.io/component";
    internal const string ManagedByLabel = "app.kubernetes.io/managed-by";

    public static V1Pod Build(
        string podName,
        KubernetesSandboxOptions adapterOptions,
        SandboxOptions runOptions)
    {
        ArgumentNullException.ThrowIfNull(podName);
        ArgumentNullException.ThrowIfNull(adapterOptions);
        ArgumentNullException.ThrowIfNull(runOptions);

        var workingDir = string.IsNullOrWhiteSpace(runOptions.WorkingDirectory)
            ? adapterOptions.WorkingDirectory
            : runOptions.WorkingDirectory!;
        var image = string.IsNullOrWhiteSpace(runOptions.Image)
            ? adapterOptions.Image
            : runOptions.Image!;

        var labels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ManagedByLabel] = "compendium",
            [ComponentLabel] = "coding-agent-sandbox",
        };
        if (!string.IsNullOrEmpty(runOptions.TenantId))
        {
            labels[TenantLabel] = runOptions.TenantId;
        }

        if (adapterOptions.PodLabels is { } extraLabels)
        {
            foreach (var kv in extraLabels)
            {
                labels[kv.Key] = kv.Value;
            }
        }

        var annotations = new Dictionary<string, string>(StringComparer.Ordinal);
        if (adapterOptions.PodAnnotations is { } extraAnn)
        {
            foreach (var kv in extraAnn)
            {
                annotations[kv.Key] = kv.Value;
            }
        }

        var envVars = new List<V1EnvVar>();
        if (runOptions.Environment is { } env)
        {
            foreach (var kv in env)
            {
                envVars.Add(new V1EnvVar(kv.Key, kv.Value));
            }
        }

        var workspaceVolume = BuildWorkspaceVolume(adapterOptions);

        var container = new V1Container
        {
            Name = adapterOptions.ContainerName,
            Image = image,
            ImagePullPolicy = "IfNotPresent",
            WorkingDir = workingDir,
            Command = new List<string> { "/bin/sh", "-c" },
            // Keep PID 1 alive so the pod stays Running while we drive it through exec.
            // Workdir is ensured at start so subsequent exec calls always have it.
            Args = new List<string>
            {
                $"mkdir -p {ShellQuote(workingDir)} && exec sleep infinity",
            },
            Env = envVars.Count == 0 ? null : envVars,
            Resources = new V1ResourceRequirements
            {
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    ["cpu"] = new(adapterOptions.CpuRequest),
                    ["memory"] = new(adapterOptions.MemoryRequest),
                },
                Limits = new Dictionary<string, ResourceQuantity>
                {
                    ["cpu"] = new(adapterOptions.CpuLimit),
                    ["memory"] = new(adapterOptions.MemoryLimit),
                },
            },
            SecurityContext = new V1SecurityContext
            {
                AllowPrivilegeEscalation = false,
                Privileged = false,
                ReadOnlyRootFilesystem = adapterOptions.ReadOnlyRootFilesystem,
                RunAsNonRoot = true,
                RunAsUser = adapterOptions.RunAsUser,
                RunAsGroup = adapterOptions.RunAsGroup,
                Capabilities = new V1Capabilities { Drop = new List<string> { "ALL" } },
            },
            VolumeMounts = new List<V1VolumeMount>
            {
                new() { Name = WorkspaceVolumeName, MountPath = workingDir },
            },
        };

        var podSpec = new V1PodSpec
        {
            ServiceAccountName = adapterOptions.ServiceAccountName,
            AutomountServiceAccountToken = false,
            RestartPolicy = "Never",
            EnableServiceLinks = false,
            TerminationGracePeriodSeconds = adapterOptions.DeleteGracePeriodSeconds,
            Containers = new List<V1Container> { container },
            Volumes = new List<V1Volume> { workspaceVolume },
            SecurityContext = new V1PodSecurityContext
            {
                RunAsNonRoot = true,
                RunAsUser = adapterOptions.RunAsUser,
                RunAsGroup = adapterOptions.RunAsGroup,
                FsGroup = adapterOptions.RunAsGroup,
                SeccompProfile = new V1SeccompProfile { Type = "RuntimeDefault" },
            },
        };

        return new V1Pod
        {
            ApiVersion = "v1",
            Kind = "Pod",
            Metadata = new V1ObjectMeta
            {
                Name = podName,
                NamespaceProperty = !string.IsNullOrWhiteSpace(runOptions.Namespace)
                    ? runOptions.Namespace
                    : adapterOptions.Namespace,
                Labels = labels,
                Annotations = annotations.Count == 0 ? null : annotations,
            },
            Spec = podSpec,
        };
    }

    private static string ShellQuote(string value)
        => "'" + value.Replace("'", "'\\''") + "'";

    private static V1Volume BuildWorkspaceVolume(KubernetesSandboxOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WorkspaceStorageRequest))
        {
            return new V1Volume
            {
                Name = WorkspaceVolumeName,
                EmptyDir = new V1EmptyDirVolumeSource(),
            };
        }

        return new V1Volume
        {
            Name = WorkspaceVolumeName,
            Ephemeral = new V1EphemeralVolumeSource
            {
                VolumeClaimTemplate = new V1PersistentVolumeClaimTemplate
                {
                    Spec = new V1PersistentVolumeClaimSpec
                    {
                        AccessModes = new List<string> { "ReadWriteOnce" },
                        Resources = new V1VolumeResourceRequirements
                        {
                            Requests = new Dictionary<string, ResourceQuantity>
                            {
                                ["storage"] = new(options.WorkspaceStorageRequest),
                            },
                        },
                        StorageClassName = options.WorkspaceStorageClassName,
                    },
                },
            },
        };
    }
}
