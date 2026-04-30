// -----------------------------------------------------------------------
// <copyright file="StandardAgent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Agents;
using Compendium.Abstractions.AI.Agents.Models;
using Compendium.Abstractions.AI.Models;
using Compendium.Core.Results;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.AI.Agents;

/// <summary>
/// Default <see cref="IAgent"/> implementation: ReAct-style loop on top of
/// <see cref="IAIProvider"/>. The model receives a system prompt that explains how
/// to call tools (an <c>```action</c> JSON block); the agent parses each response,
/// dispatches matching invocations through <see cref="IAgentToolRegistry"/>, feeds
/// the results back, and loops up to <see cref="AgentLoopOptions.MaxTurns"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is provider-agnostic: it never touches the provider's native
/// tool-calling format. It works with any chat model that respects the system
/// prompt's grammar — tested against OpenRouter / Anthropic / OpenAI families.
/// </para>
/// <para>
/// A custom system prompt can be supplied either through <see cref="IPromptRegistry"/>
/// (via <see cref="AgentRequest.PromptTemplateKey"/>) or directly through
/// <see cref="AgentRequest.SystemPromptAddendum"/>.
/// </para>
/// </remarks>
public sealed class StandardAgent : IAgent
{
    private static readonly ActivitySource ActivitySource = new("Compendium.AI.Agents", "1.0");

    private readonly IAIProvider _provider;
    private readonly IAgentToolRegistry _toolRegistry;
    private readonly IPromptRegistry? _promptRegistry;
    private readonly ILogger<StandardAgent>? _logger;

    /// <summary>
    /// Initializes a new <see cref="StandardAgent"/>.
    /// </summary>
    /// <param name="provider">The LLM provider that backs each turn.</param>
    /// <param name="toolRegistry">Where tool invocations are dispatched.</param>
    /// <param name="promptRegistry">Optional source for resolving a base system prompt by key.</param>
    /// <param name="logger">Optional logger.</param>
    public StandardAgent(
        IAIProvider provider,
        IAgentToolRegistry toolRegistry,
        IPromptRegistry? promptRegistry = null,
        ILogger<StandardAgent>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _promptRegistry = promptRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AgentResult>> RunAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            return Result.Failure<AgentResult>(Error.Validation("Agent.UserPromptRequired", "UserPrompt is required."));
        if (string.IsNullOrWhiteSpace(request.Model))
            return Result.Failure<AgentResult>(Error.Validation("Agent.ModelRequired", "Model is required."));

        var options = request.Options ?? new AgentLoopOptions();
        if (options.MaxTurns <= 0)
            return Result.Failure<AgentResult>(Error.Validation("Agent.MaxTurnsInvalid", "MaxTurns must be > 0."));
        if (options.Timeout != Timeout.InfiniteTimeSpan && options.Timeout <= TimeSpan.Zero)
            return Result.Failure<AgentResult>(Error.Validation("Agent.TimeoutInvalid", "Timeout must be > 0 or Timeout.InfiniteTimeSpan."));

        // Tool sources: the request takes precedence (per-call narrowing) and the registry
        // acts as a fallback (caller-of-record). Merging by name keeps either source as the
        // source of truth without duplicating entries.
        var requestTools = request.Tools ?? Array.Empty<AgentTool>();
        var registryTools = _toolRegistry.Discover() ?? Array.Empty<AgentTool>();
        var tools = requestTools.Count > 0
            ? requestTools
            : registryTools;
        var basePrompt = await ResolveBasePromptAsync(request, cancellationToken).ConfigureAwait(false);
        var systemPrompt = ReActPromptBuilder.Build(basePrompt, tools, request.SystemPromptAddendum);

        var messages = new List<Message>
        {
            Message.User(request.UserPrompt),
        };

        var turns = new List<AgentTurn>(capacity: options.MaxTurns);
        var totalPromptTokens = 0;
        var totalCompletionTokens = 0;
        decimal? totalCost = null;

        using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        loopCts.CancelAfter(options.Timeout);

        for (var turnIndex = 1; turnIndex <= options.MaxTurns; turnIndex++)
        {
            using var turnActivity = ActivitySource.StartActivity("agent.turn");
            turnActivity?.SetTag("agent.turn.index", turnIndex);
            turnActivity?.SetTag("agent.model", request.Model);

            if (loopCts.IsCancellationRequested)
            {
                return BuildPartialResult(turns, totalPromptTokens, totalCompletionTokens, totalCost,
                    cancellationToken.IsCancellationRequested
                        ? AgentTerminationReason.Cancelled
                        : AgentTerminationReason.Timeout);
            }

            var turnStart = DateTime.UtcNow;
            var swTurn = Stopwatch.StartNew();

            var completionRequest = new CompletionRequest
            {
                Model = request.Model,
                Messages = messages.ToArray(),
                SystemPrompt = systemPrompt,
                Temperature = request.Temperature,
                TenantId = request.TenantId,
                UserId = request.UserId,
            };

            Result<CompletionResponse> completion;
            try
            {
                completion = await _provider.CompleteAsync(completionRequest, loopCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (loopCts.IsCancellationRequested)
            {
                return BuildPartialResult(turns, totalPromptTokens, totalCompletionTokens, totalCost,
                    cancellationToken.IsCancellationRequested
                        ? AgentTerminationReason.Cancelled
                        : AgentTerminationReason.Timeout);
            }

            if (completion.IsFailure)
            {
                return Result.Failure<AgentResult>(completion.Error);
            }

            var response = completion.Value;
            totalPromptTokens += response.Usage.PromptTokens;
            totalCompletionTokens += response.Usage.CompletionTokens;
            if (response.Usage.EstimatedCostUsd.HasValue)
            {
                totalCost = (totalCost ?? 0m) + response.Usage.EstimatedCostUsd.Value;
            }

            // Try to parse an action block out of the assistant content.
            var hasAction = ReActActionParser.TryParse(response.Content, out var action, out var parseError);
            var toolInvocations = new List<AgentToolInvocation>();

            if (hasAction && action is not null)
            {
                if (tools.All(t => !string.Equals(t.Name, action.ToolName, StringComparison.Ordinal)))
                {
                    // Unknown tool — feed the error back to the model so it can recover.
                    var msg = tools.Count == 0
                        ? $"Unknown tool '{action.ToolName}'. No tools are available for this run."
                        : $"Unknown tool '{action.ToolName}'. Choose one of: {string.Join(", ", tools.Select(t => t.Name))}";
                    toolInvocations.Add(new AgentToolInvocation(action.ToolName, action.ArgumentsJson, msg, IsError: true, swTurn.Elapsed));
                    messages.Add(Message.Assistant(response.Content));
                    messages.Add(Message.User(BuildToolFeedback(action.ToolName, msg, isError: true)));
                }
                else
                {
                    var swTool = Stopwatch.StartNew();
                    var invocation = await _toolRegistry.InvokeAsync(action.ToolName, action.ArgumentsJson, loopCts.Token).ConfigureAwait(false);
                    swTool.Stop();

                    if (invocation.IsFailure)
                    {
                        // Registry-level failure (unknown tool, parser bug, …) aborts the loop.
                        return Result.Failure<AgentResult>(invocation.Error);
                    }

                    toolInvocations.Add(new AgentToolInvocation(
                        action.ToolName, action.ArgumentsJson, invocation.Value.Content,
                        invocation.Value.IsError, swTool.Elapsed));
                    messages.Add(Message.Assistant(response.Content));
                    messages.Add(Message.User(BuildToolFeedback(action.ToolName, invocation.Value.Content, invocation.Value.IsError)));
                }
            }
            else if (parseError is not null)
            {
                // Malformed action block — let the model retry by feeding the parser error back.
                _logger?.LogDebug("Agent action parse error on turn {Turn}: {Error}", turnIndex, parseError);
                toolInvocations.Add(new AgentToolInvocation(
                    ToolName: "<parse-error>", ArgumentsJson: "{}",
                    ResultText: parseError, IsError: true, Latency: TimeSpan.Zero));
                messages.Add(Message.Assistant(response.Content));
                messages.Add(Message.User($"Your previous response contained a malformed action block: {parseError}\nFix the JSON or omit the block to provide a final answer."));
            }

            swTurn.Stop();
            var turn = new AgentTurn
            {
                Index = turnIndex,
                AssistantContent = response.Content,
                ToolInvocations = toolInvocations,
                Usage = response.Usage,
                Latency = swTurn.Elapsed,
                StartedAt = turnStart,
            };
            turns.Add(turn);

            try
            {
                options.OnTurnCompleted?.Invoke(turn);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                                       and not StackOverflowException
                                       and not AccessViolationException)
            {
                // Critical exceptions are NOT swallowed — let the runtime tear down the
                // process. Everything else is per-callback noise we can keep going past.
                _logger?.LogWarning(ex, "OnTurnCompleted callback threw on turn {Turn}; ignoring", turnIndex);
            }

            // Termination conditions
            if (!hasAction && parseError is null)
            {
                // Final answer — no further tool work.
                return Result.Success(new AgentResult
                {
                    FinalOutput = response.Content,
                    Turns = turns,
                    TerminationReason = AgentTerminationReason.Completed,
                    TotalUsage = new UsageStats
                    {
                        PromptTokens = totalPromptTokens,
                        CompletionTokens = totalCompletionTokens,
                        EstimatedCostUsd = totalCost,
                    },
                });
            }

            if (options.MaxTotalTokens is { } cap && (totalPromptTokens + totalCompletionTokens) >= cap)
            {
                return BuildPartialResult(turns, totalPromptTokens, totalCompletionTokens, totalCost,
                    AgentTerminationReason.TokenBudgetExhausted);
            }
        }

        // Out of turns.
        return BuildPartialResult(turns, totalPromptTokens, totalCompletionTokens, totalCost,
            AgentTerminationReason.MaxTurnsReached);
    }

    private async Task<string?> ResolveBasePromptAsync(AgentRequest request, CancellationToken ct)
    {
        if (_promptRegistry is null || string.IsNullOrWhiteSpace(request.PromptTemplateKey))
        {
            return null;
        }

        var resolved = await _promptRegistry.RenderPromptAsync(
            request.PromptTemplateKey,
            request.PromptVariables ?? new Dictionary<string, object>(),
            ct).ConfigureAwait(false);

        if (resolved.IsSuccess)
        {
            return resolved.Value;
        }

        // Fall back to the default base prompt rather than failing the run, so a
        // misconfigured key doesn't take the agent down — but log+tag so the failure
        // is observable in production.
        _logger?.LogWarning(
            "Failed to render prompt template '{PromptTemplateKey}' ({Code}: {Message}); falling back to default base prompt.",
            request.PromptTemplateKey, resolved.Error.Code, resolved.Error.Message);
        Activity.Current?.SetTag("ai.agent.prompt_template.key", request.PromptTemplateKey);
        Activity.Current?.SetTag("ai.agent.prompt_template.render_failed", true);
        return null;
    }

    private static string BuildToolFeedback(string toolName, string content, bool isError)
    {
        var prefix = isError ? "TOOL ERROR" : "TOOL RESULT";
        return $"{prefix} (`{toolName}`):\n{content}";
    }

    private static Result<AgentResult> BuildPartialResult(
        IReadOnlyList<AgentTurn> turns, int promptTokens, int completionTokens, decimal? cost,
        AgentTerminationReason reason)
    {
        var lastAssistant = turns.LastOrDefault()?.AssistantContent ?? string.Empty;
        return Result.Success(new AgentResult
        {
            FinalOutput = lastAssistant,
            Turns = turns,
            TerminationReason = reason,
            TotalUsage = new UsageStats
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                EstimatedCostUsd = cost,
            },
        });
    }
}
