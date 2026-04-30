// AI Agent sample using Compendium's StandardAgent (ReAct loop) on top of the
// OpenRouter adapter.
//
// - Reads OPENROUTER_API_KEY from the environment.
// - Falls back to a scripted offline provider when the key is absent so the sample
//   always runs end-to-end (useful in CI / on flights / for design review).
// - Wires a tiny in-memory tool registry exposing `now` and `echo` so you can see
//   the loop dispatch a tool, feed the result back, and produce a final answer.
//
// Run:
//   export OPENROUTER_API_KEY=sk-or-...
//   dotnet run --project samples/04-AI-Agent

using System.Globalization;
using System.Text.Json;
using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Agents;
using Compendium.Abstractions.AI.Agents.Models;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.OpenRouter.DependencyInjection;
using Compendium.Application.AI.Agents;
using Compendium.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Agent;

public static class Program
{
    private const string DefaultModel = "anthropic/claude-3.5-haiku";

    public static async Task<int> Main()
    {
        Console.WriteLine("=== Compendium AI Agent: ReAct loop with two demo tools ===\n");

        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine($"  ✓ OPENROUTER_API_KEY found — calling {DefaultModel} live.\n");
            services.AddOpenRouter(o =>
            {
                o.ApiKey = apiKey;
                o.DefaultModel = DefaultModel;
            });
        }
        else
        {
            Console.WriteLine("  ⚠ OPENROUTER_API_KEY not set — running with a scripted provider.");
            Console.WriteLine("    Set the env var to make real API calls:");
            Console.WriteLine("      export OPENROUTER_API_KEY=sk-or-...\n");
            services.AddSingleton<IAIProvider, ScriptedDemoProvider>();
        }

        services.AddSingleton<IAgentToolRegistry, InMemoryToolRegistry>();
        services.AddSingleton<IAgent, StandardAgent>();

        await using var sp = services.BuildServiceProvider();
        var agent = sp.GetRequiredService<IAgent>();

        var tools = new[]
        {
            new AgentTool("now", "Returns the current UTC timestamp in ISO-8601 format. No arguments."),
            new AgentTool("echo", "Echoes the given text back. Args: { \"text\": <string> }"),
        };

        var request = new AgentRequest
        {
            UserPrompt = "What time is it right now? After you find out, also echo the phrase 'compendium' through the echo tool.",
            Model = DefaultModel,
            Tools = tools,
            Options = new AgentLoopOptions
            {
                MaxTurns = 5,
                OnTurnCompleted = t => Console.WriteLine($"  → turn {t.Index} ({t.ToolInvocations.Count} tool call(s), {t.Latency.TotalMilliseconds:F0}ms)"),
            },
            Temperature = 0.0f,
        };

        var result = await agent.RunAsync(request);
        if (result.IsFailure)
        {
            Console.Error.WriteLine($"✗ Agent run failed: {result.Error.Code} - {result.Error.Message}");
            return 1;
        }

        var run = result.Value;
        Console.WriteLine();
        Console.WriteLine($"Termination: {run.TerminationReason}");
        Console.WriteLine($"Turns:       {run.Turns.Count}");
        Console.WriteLine($"Tokens:      {run.TotalUsage.PromptTokens} in / {run.TotalUsage.CompletionTokens} out");
        Console.WriteLine();
        Console.WriteLine("Final answer:");
        Console.WriteLine($"  {run.FinalOutput}");
        Console.WriteLine("\nAudit trail:");
        foreach (var turn in run.Turns)
        {
            Console.WriteLine($"  [{turn.Index}] {turn.ToolInvocations.Count} tool call(s)");
            foreach (var inv in turn.ToolInvocations)
            {
                var marker = inv.IsError ? "✗" : "✓";
                Console.WriteLine($"      {marker} {inv.ToolName}({inv.ArgumentsJson}) → {inv.ResultText}");
            }
        }

        Console.WriteLine("\nDone.");
        return 0;
    }
}

/// <summary>
/// Two demo tools exposed in-process. <c>now</c> takes no args; <c>echo</c> takes
/// <c>{"text":"…"}</c>.
/// </summary>
internal sealed class InMemoryToolRegistry : IAgentToolRegistry
{
    public IReadOnlyList<AgentTool> Discover() => Array.Empty<AgentTool>();

    public Task<Result<AgentToolResult>> InvokeAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        switch (toolName)
        {
            case "now":
                return Task.FromResult(Result.Success(new AgentToolResult(
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))));

            case "echo":
                try
                {
                    using var doc = JsonDocument.Parse(argumentsJson);
                    var text = doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                    return Task.FromResult(Result.Success(new AgentToolResult($"echoed: {text}")));
                }
                catch (JsonException ex)
                {
                    return Task.FromResult(Result.Success(new AgentToolResult(
                        $"echo: malformed args — {ex.Message}", IsError: true)));
                }

            default:
                return Task.FromResult(Result.Failure<AgentToolResult>(
                    Error.NotFound("Tools.Unknown", $"Tool '{toolName}' is not registered")));
        }
    }
}

/// <summary>
/// Scripted offline provider for the demo. Returns a fixed sequence of responses
/// that drives the agent through one tool call (now), then a final answer.
/// </summary>
internal sealed class ScriptedDemoProvider : IAIProvider
{
    private readonly Queue<string> _responses;

    public ScriptedDemoProvider()
    {
        _responses = new Queue<string>(new[]
        {
            "I'll fetch the current time first.\n\n```action\n{\"tool\": \"now\", \"args\": {}}\n```",
            "Now I'll echo 'compendium'.\n\n```action\n{\"tool\": \"echo\", \"args\": {\"text\": \"compendium\"}}\n```",
            "Done — the time was reported by the `now` tool, and `echo` returned 'echoed: compendium'.",
        });
    }

    public string ProviderId => "scripted-demo";

    public Task<Result<CompletionResponse>> CompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        var content = _responses.Count > 0 ? _responses.Dequeue() : "(scripted run exhausted)";
        return Task.FromResult(Result.Success(new CompletionResponse
        {
            Id = Guid.NewGuid().ToString(),
            Model = request.Model,
            Content = content,
            FinishReason = content.Contains("```action") ? FinishReason.ToolCall : FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 50, CompletionTokens = 20 },
        }));
    }

    public IAsyncEnumerable<Result<CompletionChunk>> StreamCompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Scripted demo does not support streaming.");

    public Task<Result<EmbeddingResponse>> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<EmbeddingResponse>(Error.Failure("Demo.EmbeddingsNotSupported", "Scripted demo does not support embeddings.")));

    public Task<Result<IReadOnlyList<AIModel>>> ListModelsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<AIModel>>(Array.Empty<AIModel>()));

    public Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
