// AI sample using the Compendium OpenRouter adapter.
//
// - Reads OPENROUTER_API_KEY from the environment.
// - Falls back to an offline stub if the key is missing, so `dotnet run` always
//   produces useful output (and CI can still build the sample).
//
// Run:
//   export OPENROUTER_API_KEY=sk-or-...
//   dotnet run

using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.OpenRouter.DependencyInjection;
using Compendium.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.WithOpenRouter;

public static class Program
{
    private const string DefaultModel = "anthropic/claude-3.5-haiku";

    public static async Task<int> Main()
    {
        Console.WriteLine("=== Compendium AI: OpenRouter chat completion ===\n");

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
            Console.WriteLine("  ⚠ OPENROUTER_API_KEY not set — running in offline demo mode.");
            Console.WriteLine("    Set the env var to make a real API call:");
            Console.WriteLine("      export OPENROUTER_API_KEY=sk-or-...\n");
            services.AddSingleton<IAIProvider, OfflineDemoProvider>();
        }

        await using var provider = services.BuildServiceProvider();
        var ai = provider.GetRequiredService<IAIProvider>();

        var request = new CompletionRequest
        {
            Model = DefaultModel,
            SystemPrompt = "You are a concise assistant. Reply in one sentence.",
            Messages = new[]
            {
                Message.User("In one sentence, what does the Compendium framework provide for .NET developers?"),
            },
            Temperature = 0.2f,
            MaxTokens = 200,
        };

        var result = await ai.CompleteAsync(request);
        if (result.IsFailure)
        {
            Console.Error.WriteLine($"✗ Completion failed: {result.Error.Code} - {result.Error.Message}");
            return 1;
        }

        var response = result.Value!;
        Console.WriteLine($"Provider:  {ai.ProviderId}");
        Console.WriteLine($"Model:     {response.Model}");
        Console.WriteLine($"Finish:    {response.FinishReason}");
        Console.WriteLine($"Tokens:    {response.Usage.PromptTokens} in / {response.Usage.CompletionTokens} out");
        Console.WriteLine();
        Console.WriteLine("Reply:");
        Console.WriteLine($"  {response.Content}");
        Console.WriteLine("\nDone.");
        return 0;
    }
}

/// <summary>
/// A tiny stub <see cref="IAIProvider"/> used when no API key is configured.
/// Lets the sample run end-to-end without network access — useful in CI,
/// at conferences, or on flights.
/// </summary>
internal sealed class OfflineDemoProvider : IAIProvider
{
    public string ProviderId => "offline-demo";

    public Task<Result<CompletionResponse>> CompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CompletionResponse
        {
            Id = Guid.NewGuid().ToString(),
            Model = request.Model,
            Content = "Compendium is a modular .NET framework for DDD, CQRS, event sourcing, and multi-tenancy with ready-to-use adapters. (offline demo response)",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 32, CompletionTokens = 28 },
        };
        return Task.FromResult(Result.Success(response));
    }

    public async IAsyncEnumerable<Result<CompletionChunk>> StreamCompleteAsync(
        CompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return Result.Success(new CompletionChunk
        {
            Id = Guid.NewGuid().ToString(),
            ContentDelta = "(offline demo)",
            IsFinal = true,
            FinishReason = FinishReason.Stop,
        });
    }

    public Task<Result<EmbeddingResponse>> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<EmbeddingResponse>(
            AIErrors.InvalidRequest("Embeddings are not supported in offline demo mode.")));

    public Task<Result<IReadOnlyList<AIModel>>> ListModelsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<AIModel>>(Array.Empty<AIModel>()));

    public Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
