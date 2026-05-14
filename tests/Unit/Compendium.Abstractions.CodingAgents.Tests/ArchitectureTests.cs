// -----------------------------------------------------------------------
// <copyright file="ArchitectureTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace Compendium.Abstractions.CodingAgents.Tests;

/// <summary>
/// Architecture guards: the abstractions package must not depend on Nexus.*
/// or any concrete adapter (Compendium.Adapters.*). These are pure ports.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Target =
        typeof(CliCodingAgentRuntime).Assembly;

    [Fact]
    public void Assembly_has_no_reference_to_Nexus_assemblies()
    {
        var refs = Target.GetReferencedAssemblies();

        refs.Where(a => a.Name is not null && a.Name.StartsWith("Nexus.", StringComparison.Ordinal))
            .Should().BeEmpty("abstractions must not depend on Nexus.* assemblies");
    }

    [Fact]
    public void Assembly_has_no_reference_to_Compendium_Adapters_assemblies()
    {
        var refs = Target.GetReferencedAssemblies();

        refs.Where(a => a.Name is not null && a.Name.StartsWith("Compendium.Adapters.", StringComparison.Ordinal))
            .Should().BeEmpty("abstractions must not depend on any Compendium.Adapters.* assembly");
    }

    [Fact]
    public void Assembly_only_depends_on_Compendium_Core_within_Compendium_family()
    {
        var compendiumRefs = Target.GetReferencedAssemblies()
            .Where(a => a.Name is not null && a.Name.StartsWith("Compendium.", StringComparison.Ordinal))
            .Select(a => a.Name!)
            .ToArray();

        compendiumRefs.Should().OnlyContain(name => name == "Compendium.Core");
    }
}
