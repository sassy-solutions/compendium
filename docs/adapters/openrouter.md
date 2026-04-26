# Compendium.Adapters.OpenRouter

[OpenRouter](https://openrouter.ai/) is a unified API that proxies many LLM providers (Anthropic, OpenAI, Google, Mistral, Meta, …) behind a single OpenAI-compatible interface. This adapter implements the AI-provider port from `Compendium.Abstractions.AI` against OpenRouter.

## Install

```bash
dotnet add package Compendium.Adapters.OpenRouter
```

You need an OpenRouter API key.

## Configuration

```json
{
  "OpenRouter": {
    "ApiKey": "sk-or-v1-...",
    "DefaultModel": "anthropic/claude-3.5-sonnet",
    "SiteUrl": "https://example.com",
    "SiteName": "Acme"
  }
}
```

Options (`OpenRouterOptions`):

| Option | Default | Description |
|---|---|---|
| `ApiKey` | _required_ | OpenRouter API key |
| `BaseUrl` | `https://openrouter.ai/api/v1` | API base URL |
| `DefaultModel` | `anthropic/claude-3.5-sonnet` | Default model when not specified per call |
| `DefaultTemperature` | `0.7` | Default temperature |
| `DefaultMaxTokens` | `4096` | Default max tokens |
| `TimeoutSeconds` | `120` | HTTP timeout (LLM calls can be slow) |
| `RetryAttempts` | `3` | Retries on transient failures |
| `SiteUrl` | `null` | Sent in `HTTP-Referer` for OpenRouter rankings |
| `SiteName` | `null` | Sent in `X-Title` for OpenRouter rankings |
| `EnableLogging` | `false` | Log full request/response (do not enable in prod) |
| `Models` | `{}` | Per-model overrides (max tokens, temperature, custom params) |

## Usage

```csharp
public sealed class SummarizeHandler(IAIProvider ai)
    : IQueryHandler<SummarizeQuery, string>
{
    public async Task<Result<string>> Handle(SummarizeQuery q, CancellationToken ct)
    {
        var result = await ai.CompleteAsync(new CompletionRequest
        {
            Model = "anthropic/claude-3.5-sonnet",
            Messages = [new("user", q.Text)],
            MaxTokens = 500,
        }, ct);

        return result.IsSuccess
            ? result.Value.Content
            : result.Error;
    }
}
```

The adapter supports streaming (`CompletionChunk`), embeddings (`EmbeddingRequest`/`EmbeddingResponse`), and per-model parameter overrides via `OpenRouterOptions.Models`.

## Gotchas

- **`EnableLogging` logs full prompts.** Useful for development, terrible for compliance: prompts often contain user PII and sensitive context. Keep this off outside dev.
- **Model strings are provider-prefixed.** Use `anthropic/claude-3.5-sonnet`, not `claude-3.5-sonnet`. OpenRouter routes by the prefix.
- **Pricing varies wildly per model.** Compendium does not enforce a budget — use OpenRouter's dashboard or your own metering on top. A runaway loop on `anthropic/claude-opus` is much more expensive than the same loop on `meta-llama/llama-3.1-8b`.
- **`DefaultMaxTokens: 4096` is conservative.** Larger context windows are available (200k for Claude 3.5 Sonnet, 1M+ for some models); raise `DefaultMaxTokens` if you need them.
- **Latency tail.** LLM responses occasionally time out at 60s+ even on fast models. The default `TimeoutSeconds: 120` is intentional; do not lower it without testing.

## See also

- [API Reference: Compendium.Adapters.OpenRouter.Configuration](../api/Compendium.Adapters.OpenRouter.Configuration.html)
- [OpenRouter docs](https://openrouter.ai/docs)
- [`samples/03-AI-WithOpenRouter`](https://github.com/sassy-solutions/compendium/tree/main/samples/03-AI-WithOpenRouter)
- [`Compendium.Abstractions.AI`](../api/Compendium.Abstractions.AI.html) — port contracts
