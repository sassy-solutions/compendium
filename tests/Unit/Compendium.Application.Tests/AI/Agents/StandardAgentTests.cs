// -----------------------------------------------------------------------
// <copyright file="StandardAgentTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Agents;
using Compendium.Abstractions.AI.Agents.Models;
using Compendium.Abstractions.AI.Models;
using Compendium.Application.AI.Agents;

namespace Compendium.Application.Tests.AI.Agents;

/// <summary>
/// Unit tests for the <see cref="StandardAgent"/> class.
/// </summary>
public class StandardAgentTests
{
    [Fact]
    public void Constructor_WhenProviderIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new StandardAgent(null!, Substitute.For<IAgentToolRegistry>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
    }

    [Fact]
    public void Constructor_WhenToolRegistryIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new StandardAgent(Substitute.For<IAIProvider>(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("toolRegistry");
    }

    [Fact]
    public async Task RunAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var agent = CreateAgent(out _, out _);

        // Act
        var act = async () => await agent.RunAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_WhenUserPromptInvalid_ReturnsValidationFailure(string? prompt)
    {
        // Arrange
        var agent = CreateAgent(out _, out _);
        var request = new AgentRequest { UserPrompt = prompt!, Model = "m" };

        // Act
        var result = await agent.RunAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Agent.UserPromptRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\t")]
    public async Task RunAsync_WhenModelInvalid_ReturnsValidationFailure(string? model)
    {
        // Arrange
        var agent = CreateAgent(out _, out _);
        var request = new AgentRequest { UserPrompt = "hi", Model = model! };

        // Act
        var result = await agent.RunAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Agent.ModelRequired");
    }

    [Fact]
    public async Task RunAsync_WhenMaxTurnsZero_ReturnsValidationFailure()
    {
        // Arrange
        var agent = CreateAgent(out _, out _);
        var request = new AgentRequest
        {
            UserPrompt = "hi",
            Model = "m",
            Options = new AgentLoopOptions { MaxTurns = 0 },
        };

        // Act
        var result = await agent.RunAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Agent.MaxTurnsInvalid");
    }

    [Fact]
    public async Task RunAsync_WhenTimeoutNonPositive_ReturnsValidationFailure()
    {
        // Arrange
        var agent = CreateAgent(out _, out _);
        var request = new AgentRequest
        {
            UserPrompt = "hi",
            Model = "m",
            Options = new AgentLoopOptions { Timeout = TimeSpan.Zero },
        };

        // Act
        var result = await agent.RunAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Agent.TimeoutInvalid");
    }

    [Fact]
    public async Task RunAsync_WhenProviderFails_ReturnsProviderError()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out _);
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<CompletionResponse>(Error.Failure("Provider.Down", "down"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Provider.Down");
    }

    [Fact]
    public async Task RunAsync_WhenModelReturnsFinalAnswer_TerminatesAsCompleted()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out _);
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse("Hello!"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FinalOutput.Should().Be("Hello!");
        result.Value.TerminationReason.Should().Be(AgentTerminationReason.Completed);
        result.Value.Turns.Should().HaveCount(1);
    }

    [Fact]
    public async Task RunAsync_WhenModelInvokesKnownTool_FeedsResultBack()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        var tool = new AgentTool("calc", "calculator");

        var calls = 0;
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromResult(Result.Success(BuildResponse(
                        "Reasoning... ```action\n{ \"tool\": \"calc\", \"args\": { \"x\": 2 } }\n```")))
                    : Task.FromResult(Result.Success(BuildResponse("Final answer")));
            });

        registry.Discover().Returns(new[] { tool });
        registry.InvokeAsync("calc", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new AgentToolResult("42"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TerminationReason.Should().Be(AgentTerminationReason.Completed);
        result.Value.Turns.Should().HaveCount(2);
        result.Value.Turns[0].ToolInvocations.Should().ContainSingle()
            .Which.ToolName.Should().Be("calc");
    }

    [Fact]
    public async Task RunAsync_WhenModelInvokesUnknownTool_FeedsErrorBackAndContinues()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        registry.Discover().Returns(new[] { new AgentTool("known", "k") });

        var calls = 0;
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromResult(Result.Success(BuildResponse(
                        "```action\n{ \"tool\": \"unknown\" }\n```")))
                    : Task.FromResult(Result.Success(BuildResponse("Final")));
            });

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Turns[0].ToolInvocations.Should().ContainSingle()
            .Which.IsError.Should().BeTrue();
        // Registry should NOT be invoked for unknown tool.
        await registry.DidNotReceive().InvokeAsync("unknown", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenNoToolsAvailableAndModelInvokesUnknownTool_ReportsNoToolsAvailable()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        registry.Discover().Returns(Array.Empty<AgentTool>());

        var calls = 0;
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromResult(Result.Success(BuildResponse(
                        "```action\n{ \"tool\": \"unknown\" }\n```")))
                    : Task.FromResult(Result.Success(BuildResponse("Final")));
            });

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Turns[0].ToolInvocations.Should().ContainSingle()
            .Which.ResultText.Should().Contain("No tools are available");
    }

    [Fact]
    public async Task RunAsync_WhenRegistryInvokeFails_AbortsRunWithFailure()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        registry.Discover().Returns(new[] { new AgentTool("calc", "c") });

        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse(
                "```action\n{ \"tool\": \"calc\" }\n```"))));

        registry.InvokeAsync("calc", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<AgentToolResult>(Error.Failure("Reg.Down", "fail"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Reg.Down");
    }

    [Fact]
    public async Task RunAsync_WhenActionParseError_FeedsErrorBackAndContinues()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out _);

        var calls = 0;
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromResult(Result.Success(BuildResponse(
                        "```action\n{ broken json\n```")))
                    : Task.FromResult(Result.Success(BuildResponse("Final")));
            });

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Turns[0].ToolInvocations.Should().ContainSingle()
            .Which.ToolName.Should().Be("<parse-error>");
    }

    [Fact]
    public async Task RunAsync_WhenMaxTurnsReached_TerminatesAsMaxTurns()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        registry.Discover().Returns(new[] { new AgentTool("loop", "l") });

        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse(
                "```action\n{ \"tool\": \"loop\" }\n```"))));

        registry.InvokeAsync("loop", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new AgentToolResult("ok"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                Options = new AgentLoopOptions { MaxTurns = 2 },
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TerminationReason.Should().Be(AgentTerminationReason.MaxTurnsReached);
        result.Value.Turns.Should().HaveCount(2);
    }

    [Fact]
    public async Task RunAsync_WhenTokenBudgetExhausted_TerminatesAsTokenBudget()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        registry.Discover().Returns(new[] { new AgentTool("loop", "l") });

        // Each turn costs 100 tokens; cap is 50 → exhausted after 1 turn.
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse(
                "```action\n{ \"tool\": \"loop\" }\n```",
                promptTokens: 50,
                completionTokens: 50))));

        registry.InvokeAsync("loop", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new AgentToolResult("ok"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                Options = new AgentLoopOptions { MaxTurns = 5, MaxTotalTokens = 50 },
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TerminationReason.Should().Be(AgentTerminationReason.TokenBudgetExhausted);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_TerminatesAsCancelled()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out _);
        using var cts = new CancellationTokenSource();

        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<CompletionResponse>>>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        // Act
        var result = await agent.RunAsync(
            new AgentRequest { UserPrompt = "hi", Model = "m" },
            cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TerminationReason.Should().Be(AgentTerminationReason.Cancelled);
    }

    [Fact]
    public async Task RunAsync_OnTurnCompletedCallback_IsInvokedPerTurn()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out _);
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse("Final"))));

        var observed = new List<int>();

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                Options = new AgentLoopOptions { OnTurnCompleted = t => observed.Add(t.Index) },
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        observed.Should().Equal(1);
    }

    [Fact]
    public async Task RunAsync_OnTurnCompletedCallbackThrows_StillCompletesRun()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out _);
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse("Final"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                Options = new AgentLoopOptions { OnTurnCompleted = _ => throw new InvalidOperationException("oops") },
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_RequestToolsTakesPrecedenceOverRegistry()
    {
        // Arrange
        var agent = CreateAgent(out var provider, out var registry);
        registry.Discover().Returns(new[] { new AgentTool("registry-only", "r") });

        var calls = 0;
        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromResult(Result.Success(BuildResponse(
                        "```action\n{ \"tool\": \"request-only\" }\n```")))
                    : Task.FromResult(Result.Success(BuildResponse("done")));
            });

        registry.InvokeAsync("request-only", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new AgentToolResult("v"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                Tools = new[] { new AgentTool("request-only", "r") },
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Turns[0].ToolInvocations.Should().ContainSingle()
            .Which.ToolName.Should().Be("request-only");
    }

    [Fact]
    public async Task RunAsync_WithPromptRegistryHit_UsesRenderedPrompt()
    {
        // Arrange
        var provider = Substitute.For<IAIProvider>();
        var registry = Substitute.For<IAgentToolRegistry>();
        var promptRegistry = Substitute.For<IPromptRegistry>();
        var agent = new StandardAgent(provider, registry, promptRegistry);

        promptRegistry
            .RenderPromptAsync("k", Arg.Any<IReadOnlyDictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success("Rendered base.")));

        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse("Final"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                PromptTemplateKey = "k",
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await provider.Received(1).CompleteAsync(
            Arg.Is<CompletionRequest>(r => r.SystemPrompt!.Contains("Rendered base.")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithPromptRegistryFailure_FallsBackToDefaultBasePrompt()
    {
        // Arrange
        var provider = Substitute.For<IAIProvider>();
        var registry = Substitute.For<IAgentToolRegistry>();
        var promptRegistry = Substitute.For<IPromptRegistry>();
        var agent = new StandardAgent(provider, registry, promptRegistry);

        promptRegistry
            .RenderPromptAsync("k", Arg.Any<IReadOnlyDictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<string>(Error.NotFound("Prompt.NotFound", "x"))));

        provider.CompleteAsync(Arg.Any<CompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(BuildResponse("Final"))));

        // Act
        var result = await agent.RunAsync(
            new AgentRequest
            {
                UserPrompt = "hi",
                Model = "m",
                PromptTemplateKey = "k",
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    private static StandardAgent CreateAgent(
        out IAIProvider provider,
        out IAgentToolRegistry registry)
    {
        provider = Substitute.For<IAIProvider>();
        registry = Substitute.For<IAgentToolRegistry>();
        return new StandardAgent(provider, registry);
    }

    private static CompletionResponse BuildResponse(string content, int promptTokens = 1, int completionTokens = 1)
    {
        return new CompletionResponse
        {
            Id = Guid.NewGuid().ToString(),
            Model = "m",
            Content = content,
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = promptTokens, CompletionTokens = completionTokens },
        };
    }
}
