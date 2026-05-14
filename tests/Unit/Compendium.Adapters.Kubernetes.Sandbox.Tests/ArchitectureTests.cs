// -----------------------------------------------------------------------
// <copyright file="ArchitectureTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace Compendium.Adapters.Kubernetes.Sandbox.Tests;

/// <summary>
/// Architecture guards: the Kubernetes adapter must only depend on Compendium.Core
/// and Compendium.Abstractions.CodingAgents within the framework family. No
/// Nexus.* references; no fork of the IAgentSandbox port.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Target = typeof(KubernetesAgentSandbox).Assembly;

    [Fact]
    public void Assembly_has_no_reference_to_Nexus_assemblies()
    {
        Target.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("Nexus.", StringComparison.Ordinal) == true)
            .Should().BeEmpty();
    }

    [Fact]
    public void Assembly_only_depends_on_Compendium_Core_or_Compendium_Abstractions_CodingAgents()
    {
        var compendiumRefs = Target.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("Compendium.", StringComparison.Ordinal) == true)
            .Select(a => a.Name!)
            .ToArray();

        compendiumRefs.Should().OnlyContain(name =>
            name == "Compendium.Core" || name == "Compendium.Abstractions.CodingAgents");
    }

    [Fact]
    public void Assembly_does_not_define_a_competing_IAgentSandbox_abstraction()
    {
        // The adapter must reuse Compendium.Abstractions.CodingAgents.Sandbox.IAgentSandbox.
        Target.GetTypes()
            .Where(t => t.IsInterface && t.Name == "IAgentSandbox")
            .Should().BeEmpty();
    }
}
