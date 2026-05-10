// -----------------------------------------------------------------------
// <copyright file="PartialCoverageTopUpTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Abstractions.AI.Tests;

/// <summary>
/// Top-up tests for properties that the existing suite skipped, lifting partially covered
/// records (AgentRequest, AgentToolInvocation, CompletionRequest, CompletionResponse) above
/// the 90% line-coverage bar without touching production code.
/// </summary>
public sealed class AgentRequestTopUpTests
{
    [Fact]
    public void AgentRequest_PromptTemplateKey_RoundTripUnchanged()
    {
        // Arrange & Act
        var request = new AgentRequest
        {
            UserPrompt = "Hello",
            Model = "anthropic/claude-3.5-sonnet",
            PromptTemplateKey = "agents.support.v1",
        };

        // Assert
        request.PromptTemplateKey.Should().Be("agents.support.v1");
    }

    [Fact]
    public void AgentRequest_PromptVariables_RoundTripUnchanged()
    {
        // Arrange
        var variables = new Dictionary<string, object>
        {
            ["customer_name"] = "Alice",
            ["language"] = "fr",
            ["max_steps"] = 5,
        };

        // Act
        var request = new AgentRequest
        {
            UserPrompt = "Hello",
            Model = "openai/gpt-4o",
            PromptTemplateKey = "agents.support.v1",
            PromptVariables = variables,
        };

        // Assert
        request.PromptVariables.Should().BeSameAs(variables);
        request.PromptVariables!["customer_name"].Should().Be("Alice");
        request.PromptVariables["max_steps"].Should().Be(5);
    }

    [Fact]
    public void AgentRequest_PromptVariables_DefaultsToNull()
    {
        // Arrange & Act
        var request = new AgentRequest
        {
            UserPrompt = "Hello",
            Model = "m",
        };

        // Assert
        request.PromptTemplateKey.Should().BeNull();
        request.PromptVariables.Should().BeNull();
    }
}

public sealed class AgentToolInvocationTopUpTests
{
    [Fact]
    public void AgentToolInvocation_ArgumentsJson_RoundTripUnchanged()
    {
        // Arrange
        const string json = "{\"query\":\"weather in Paris\",\"limit\":3}";

        // Act
        var invocation = new AgentToolInvocation(
            ToolName: "search",
            ArgumentsJson: json,
            ResultText: "results...",
            IsError: false,
            Latency: TimeSpan.FromMilliseconds(120));

        // Assert
        invocation.ArgumentsJson.Should().Be(json);
        invocation.ToolName.Should().Be("search");
        invocation.ResultText.Should().Be("results...");
        invocation.IsError.Should().BeFalse();
        invocation.Latency.Should().Be(TimeSpan.FromMilliseconds(120));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"a\":1}")]
    [InlineData("{\"nested\":{\"k\":\"v\"}}")]
    [InlineData("[]")]
    public void AgentToolInvocation_ArgumentsJson_AcceptsArbitraryJson(string json)
    {
        // Arrange & Act
        var invocation = new AgentToolInvocation(
            ToolName: "t",
            ArgumentsJson: json,
            ResultText: "ok",
            IsError: false,
            Latency: TimeSpan.Zero);

        // Assert
        invocation.ArgumentsJson.Should().Be(json);
    }
}

public sealed class CompletionRequestTopUpTests
{
    [Fact]
    public void CompletionRequest_AdditionalParameters_RoundTripUnchanged()
    {
        // Arrange
        var additional = new Dictionary<string, object>
        {
            ["seed"] = 42,
            ["response_format"] = "json",
            ["logprobs"] = true,
        };

        // Act
        var request = new CompletionRequest
        {
            Model = "openai/gpt-4o",
            Messages = new[] { Message.User("hi") },
            AdditionalParameters = additional,
        };

        // Assert
        request.AdditionalParameters.Should().BeSameAs(additional);
        request.AdditionalParameters!["seed"].Should().Be(42);
        request.AdditionalParameters["response_format"].Should().Be("json");
        request.AdditionalParameters["logprobs"].Should().Be(true);
    }

    [Fact]
    public void CompletionRequest_AdditionalParameters_DefaultsToNull()
    {
        // Arrange & Act
        var request = new CompletionRequest
        {
            Model = "m",
            Messages = new[] { Message.User("hi") },
        };

        // Assert
        request.AdditionalParameters.Should().BeNull();
    }
}

public sealed class CompletionResponseTopUpTests
{
    [Fact]
    public void CompletionResponse_Metadata_RoundTripUnchanged()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["provider_id"] = "openrouter",
            ["model_resolved"] = "anthropic/claude-3-5-sonnet-20241022",
            ["cache_hit"] = false,
        };

        // Act
        var response = new CompletionResponse
        {
            Id = "cmpl-1",
            Model = "anthropic/claude-3.5-sonnet",
            Content = "ok",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 1, CompletionTokens = 1 },
            Metadata = metadata,
        };

        // Assert
        response.Metadata.Should().BeSameAs(metadata);
        response.Metadata!["provider_id"].Should().Be("openrouter");
        response.Metadata["cache_hit"].Should().Be(false);
    }

    [Fact]
    public void CompletionResponse_Metadata_DefaultsToNull()
    {
        // Arrange & Act
        var response = new CompletionResponse
        {
            Id = "cmpl-1",
            Model = "m",
            Content = "x",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 1, CompletionTokens = 1 },
        };

        // Assert
        response.Metadata.Should().BeNull();
    }
}
