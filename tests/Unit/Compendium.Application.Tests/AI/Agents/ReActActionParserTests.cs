// -----------------------------------------------------------------------
// <copyright file="ReActActionParserTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Application.AI.Agents;

namespace Compendium.Application.Tests.AI.Agents;

/// <summary>
/// Unit tests for the <see cref="ReActActionParser"/> class.
/// </summary>
public class ReActActionParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\t\n")]
    public void TryParse_WhenContentEmpty_ReturnsFalseWithNoErrorOrAction(string? content)
    {
        // Arrange / Act
        var ok = ReActActionParser.TryParse(content!, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        action.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParse_WhenNoActionFenceFound_ReturnsFalseWithNullErrorAndAction()
    {
        // Arrange
        var content = "Just a normal answer with no fenced action block.";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        action.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParse_WhenWordActionableMatchedNotActionTag_ReturnsFalse()
    {
        // Arrange — `actionable` should NOT match `action`.
        var content = "Here is some ```actionable\n{ \"tool\": \"x\" }\n``` text";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        action.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParse_WhenBlockNotClosed_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n{ \"tool\": \"x\" }\n";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        action.Should().BeNull();
        error.Should().Contain("not closed");
    }

    [Fact]
    public void TryParse_WhenJsonMalformed_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n{ this is not json }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        action.Should().BeNull();
        error.Should().Contain("malformed");
    }

    [Fact]
    public void TryParse_WhenJsonNotObject_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n[1, 2, 3]\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        error.Should().Contain("JSON object");
    }

    [Fact]
    public void TryParse_WhenToolMissing_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n{ \"args\": {} }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        error.Should().Contain("string `tool`");
    }

    [Fact]
    public void TryParse_WhenToolNotString_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n{ \"tool\": 1 }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        error.Should().Contain("string `tool`");
    }

    [Fact]
    public void TryParse_WhenToolEmpty_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n{ \"tool\": \"   \" }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        error.Should().Contain("non-empty");
    }

    [Fact]
    public void TryParse_WhenArgsNotObject_ReturnsFalseWithError()
    {
        // Arrange
        var content = "```action\n{ \"tool\": \"t\", \"args\": [1,2] }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeFalse();
        error.Should().Contain("JSON object");
    }

    [Fact]
    public void TryParse_BlockForm_ReturnsParsedAction()
    {
        // Arrange
        var content = "Reasoning text...\n\n```action\n{ \"tool\": \"search\", \"args\": { \"q\": \"hello\" } }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeTrue();
        error.Should().BeNull();
        action!.ToolName.Should().Be("search");
        action.ArgumentsJson.Should().Contain("hello");
    }

    [Fact]
    public void TryParse_InlineForm_ReturnsParsedAction()
    {
        // Arrange — model puts JSON on same line as opening fence.
        var content = "```action { \"tool\": \"t\" } ```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeTrue();
        action!.ToolName.Should().Be("t");
        action.ArgumentsJson.Should().Be("{}");
    }

    [Fact]
    public void TryParse_WithoutArgsProperty_DefaultsToEmptyObject()
    {
        // Arrange
        var content = "```action\n{ \"tool\": \"t\" }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeTrue();
        action!.ArgumentsJson.Should().Be("{}");
    }

    [Fact]
    public void TryParse_WithNullArgs_DefaultsToEmptyObject()
    {
        // Arrange
        var content = "```action\n{ \"tool\": \"t\", \"args\": null }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeTrue();
        action!.ArgumentsJson.Should().Be("{}");
    }

    [Fact]
    public void TryParse_PreservesArgsRawJson()
    {
        // Arrange
        var content = "```action\n{ \"tool\": \"t\", \"args\": { \"a\": 1, \"b\": \"two\" } }\n```";

        // Act
        var ok = ReActActionParser.TryParse(content, out var action, out var error);

        // Assert
        ok.Should().BeTrue();
        action!.ArgumentsJson.Should().Contain("\"a\"");
        action.ArgumentsJson.Should().Contain("\"b\"");
    }
}
