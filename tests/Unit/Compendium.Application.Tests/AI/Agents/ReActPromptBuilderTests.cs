// -----------------------------------------------------------------------
// <copyright file="ReActPromptBuilderTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;
using Compendium.Application.AI.Agents;

namespace Compendium.Application.Tests.AI.Agents;

/// <summary>
/// Unit tests for the <see cref="ReActPromptBuilder"/> class.
/// </summary>
public class ReActPromptBuilderTests
{
    [Fact]
    public void Build_WithoutBasePromptOrTools_ReturnsDefaultBaseOnly()
    {
        // Arrange / Act
        var prompt = ReActPromptBuilder.Build(null, Array.Empty<AgentTool>(), null);

        // Assert
        prompt.Should().Contain("You are a helpful AI assistant.");
        prompt.Should().NotContain("```action");
        prompt.Should().EndWith("\n");
    }

    [Fact]
    public void Build_WithEmptyBasePrompt_FallsBackToDefault()
    {
        // Arrange / Act
        var prompt = ReActPromptBuilder.Build("   ", Array.Empty<AgentTool>(), null);

        // Assert
        prompt.Should().Contain("You are a helpful AI assistant.");
    }

    [Fact]
    public void Build_WithBasePrompt_TrimsAndUsesIt()
    {
        // Arrange / Act
        var prompt = ReActPromptBuilder.Build("  Custom base.  ", Array.Empty<AgentTool>(), null);

        // Assert
        prompt.Should().Contain("Custom base.");
        prompt.Should().NotContain("helpful AI assistant");
    }

    [Fact]
    public void Build_WithTools_IncludesActionFenceAndToolListing()
    {
        // Arrange
        var tools = new[]
        {
            new AgentTool("search", "Search the web"),
            new AgentTool("calc", "Run a calculation", InputSchemaJson: "{\"type\":\"object\"}"),
        };

        // Act
        var prompt = ReActPromptBuilder.Build("Base", tools, null);

        // Assert
        prompt.Should().Contain(ReActPromptBuilder.ActionFenceOpen);
        prompt.Should().Contain(ReActPromptBuilder.ActionFenceClose);
        prompt.Should().Contain("`search`: Search the web");
        prompt.Should().Contain("`calc`: Run a calculation");
        prompt.Should().Contain("Input schema:");
    }

    [Fact]
    public void Build_WithToolWithoutSchema_OmitsSchemaLine()
    {
        // Arrange
        var tools = new[]
        {
            new AgentTool("search", "Search the web"),
        };

        // Act
        var prompt = ReActPromptBuilder.Build(null, tools, null);

        // Assert
        prompt.Should().NotContain("Input schema:");
    }

    [Fact]
    public void Build_WithAddendum_AppendsTrimmedAddendum()
    {
        // Arrange
        var addendum = "  Always speak French.  ";

        // Act
        var prompt = ReActPromptBuilder.Build("Base", Array.Empty<AgentTool>(), addendum);

        // Assert
        prompt.Should().Contain("Always speak French.");
    }

    [Fact]
    public void Build_WithEmptyAddendum_OmitsAddendumSection()
    {
        // Arrange / Act
        var prompt = ReActPromptBuilder.Build("Base", Array.Empty<AgentTool>(), "   ");

        // Assert
        // No actual addendum content.
        prompt.Should().Contain("Base");
    }

    [Fact]
    public void Build_AlwaysEndsWithNewline()
    {
        // Arrange / Act
        var prompt = ReActPromptBuilder.Build(null, Array.Empty<AgentTool>(), null);

        // Assert
        prompt.Should().EndWith("\n");
    }
}
