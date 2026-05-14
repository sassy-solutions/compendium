// -----------------------------------------------------------------------
// <copyright file="KubernetesAgentSandboxNamingTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Kubernetes.Sandbox.Tests;

public class KubernetesAgentSandboxNamingTests
{
    [Theory]
    [InlineData("coding-agent")]
    [InlineData("CodingAgent")]
    [InlineData("agent_!@#")]
    public void BuildPodName_produces_RFC1123_compatible_names(string prefix)
    {
        var name = KubernetesAgentSandbox.BuildPodName(prefix);

        name.Length.Should().BeLessThanOrEqualTo(63);
        name.Should().MatchRegex("^[a-z0-9-]+$");
        char.IsLetterOrDigit(name[0]).Should().BeTrue();
        char.IsLetterOrDigit(name[^1]).Should().BeTrue();
    }

    [Fact]
    public void BuildPodName_falls_back_when_prefix_has_no_valid_chars()
    {
        var name = KubernetesAgentSandbox.BuildPodName("___");

        name.Should().StartWith("coding-agent-");
    }

    [Fact]
    public void BuildPodName_truncates_oversized_prefix()
    {
        var prefix = new string('a', 80);

        var name = KubernetesAgentSandbox.BuildPodName(prefix);

        name.Length.Should().Be(63);
    }
}
