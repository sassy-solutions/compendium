# Compendium.Adapters.OpenRouter

> OpenRouter LLM gateway adapter: completions, streaming completions, and model discovery.

## Install

```bash
dotnet add package Compendium.Adapters.OpenRouter
```

## Configuration

`appsettings.json`:

```json
{
  "OpenRouter": {
    "ApiKey": "sk-or-v1-...",
    "BaseUrl": "https://openrouter.ai/api/v1",
    "DefaultModel": "anthropic/claude-3.5-sonnet",
    "DefaultTemperature": 0.7,
    "DefaultMaxTokens": 4096,
    "TimeoutSeconds": 120,
    "RetryAttempts": 3,
    "SiteUrl": "https://app.example.com",
    "SiteName": "Example",
    "EnableLogging": false,
    "Models": {
      "anthropic/claude-3.5-sonnet": {
        "MaxTokens": 8192,
        "Temperature": 0.2
      }
    }
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.OpenRouter;

// Bind directly from configuration (uses OpenRouterOptions.SectionName = "OpenRouter")
builder.Services.AddOpenRouter(builder.Configuration);

// Or configure inline
builder.Services.AddOpenRouter(options =>
{
    options.ApiKey = "sk-or-v1-...";
    options.DefaultModel = "anthropic/claude-3.5-sonnet";
});
```

### `OpenRouterOptions`

Bound from configuration section `OpenRouter` (constant `OpenRouterOptions.SectionName`).

| Property | Default | Description |
|---|---|---|
| `ApiKey` | `""` | OpenRouter API key. Required. |
| `BaseUrl` | `"https://openrouter.ai/api/v1"` | API base URL. |
| `DefaultModel` | `"anthropic/claude-3.5-sonnet"` | Default model when the request does not specify one. |
| `DefaultTemperature` | `0.7f` | Default sampling temperature. |
| `DefaultMaxTokens` | `4096` | Default maximum completion tokens. |
| `TimeoutSeconds` | `120` | HTTP timeout. LLM calls take longer than typical APIs. |
| `RetryAttempts` | `3` | Retry attempts for transient failures. |
| `SiteUrl` | `null` | Optional site URL forwarded to OpenRouter for app rankings. |
| `SiteName` | `null` | Optional site name forwarded to OpenRouter for app rankings. |
| `EnableLogging` | `false` | Log full request/response bodies. **PII risk — keep off in production.** |
| `Models` | `{}` | Per-model overrides (`MaxTokens`, `Temperature`, custom `Parameters`). |

`ModelConfig` (entries in `Models`):

| Property | Default | Description |
|---|---|---|
| `MaxTokens` | `null` | Override `DefaultMaxTokens` for this model. |
| `Temperature` | `null` | Override `DefaultTemperature` for this model. |
| `Parameters` | `null` | Custom request parameters merged into the body. |

## Usage

Resolve `IAIProvider` from DI (the adapter registers `OpenRouterAIProvider` as a singleton with `ProviderId = "openrouter"`):

```csharp
public class AssistantHandler
{
    private readonly IAIProvider _ai;

    public AssistantHandler(IAIProvider ai) => _ai = ai;

    public Task<Result<CompletionResponse>> Complete(string prompt, CancellationToken ct) =>
        _ai.CompleteAsync(
            new CompletionRequest(
                model: "anthropic/claude-3.5-sonnet",
                systemPrompt: "You are a helpful assistant.",
                messages: new[] { new ChatMessage("user", prompt) }),
            ct);

    public async IAsyncEnumerable<CompletionChunk> Stream(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var request = new CompletionRequest(
            model: "anthropic/claude-3.5-sonnet",
            messages: new[] { new ChatMessage("user", prompt) });

        await foreach (var chunk in _ai.StreamCompleteAsync(request, ct))
            yield return chunk;
    }
}
```

`ListModelsAsync` returns the catalog of available models with context window, max output, streaming/vision/tool support, and pricing per million tokens. `HealthCheckAsync` pings OpenRouter for liveness.

## Gotchas

- **No embeddings.** `EmbedAsync` returns "Embeddings are not directly supported via OpenRouter." Use a dedicated embedding provider (OpenAI, Voyage, etc.) for vector workloads.
- **Default timeout is 120 s.** LLM calls take long enough that the typical 30 s default is too aggressive; raise it if you target reasoning-heavy models.
- **Pricing is reported per million tokens.** OpenRouter returns a per-token figure; the adapter multiplies by 1,000,000 for `AIModel.Pricing` to match the standard SaaS pricing convention.
- **Vision capability is heuristic.** Detected from the model's `architecture.modality` field containing `"image"`. Trust the upstream metadata — this is not a hand-curated list.
- **`EnableLogging` logs prompt and response bodies.** Useful for development; do not turn on in production unless you have explicit data-handling approval.
- `SiteUrl` and `SiteName` are optional metadata sent to OpenRouter for app rankings. They have no effect on routing, just visibility on OpenRouter's leaderboards.

## See also

- [API Reference](../api/Compendium.Adapters.OpenRouter.html)
- [OpenRouter API documentation](https://openrouter.ai/docs)
- Sample app — coming via POM-182.
