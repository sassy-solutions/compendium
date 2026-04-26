# 03 — AI with OpenRouter

A minimal chat completion using `Compendium.Adapters.OpenRouter`, which implements the provider-agnostic `IAIProvider` from `Compendium.Abstractions.AI`.

## What it shows

- `AddOpenRouter(o => o.ApiKey = ...)` registers `IAIProvider` against OpenRouter
- `IAIProvider.CompleteAsync(...)` for a single-shot response
- `Result<T>` for success / failure, including token counts and finish reason
- A pluggable offline fallback (`OfflineDemoProvider`) so the sample runs without internet or an API key

## Run it

### Live mode (real API call)

```bash
export OPENROUTER_API_KEY=sk-or-...
dotnet run -c Release
```

Get a free OpenRouter key at <https://openrouter.ai/keys>. The sample defaults to `anthropic/claude-3.5-haiku` (cheap and fast); change `DefaultModel` in `Program.cs` to try anything from <https://openrouter.ai/models>.

### Offline mode (no network)

Just don't set the env var:

```bash
unset OPENROUTER_API_KEY
dotnet run -c Release
```

You'll see `running in offline demo mode` and a hardcoded response. Useful for demos, CI, and laptops without network.

## Going further

- See `src/Abstractions/Compendium.Abstractions.AI/` for the provider contract — swap OpenRouter for an in-house `IAIProvider` without touching call sites.
- See `docs/adapters/openrouter.md` for streaming, model fallbacks, and cost tracking.
