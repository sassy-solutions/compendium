// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.ClaudeCode.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.ClaudeCode.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddClaudeCodeRuntime_registers_runtime_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddClaudeCodeRuntime();

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<ICodingAgentRuntime>();

        runtime.Should().BeOfType<ClaudeCodeRuntime>();
        runtime.Engine.Should().Be("claude-code");
        provider.GetRequiredService<ICodingAgentRuntime>().Should().BeSameAs(runtime);
    }
}
