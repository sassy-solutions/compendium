# 04 — AI Agent (ReAct loop with tools)

Uses `Compendium.Application.AI.Agents.StandardAgent` — a ReAct-style agent loop on top of `IAIProvider` — to drive a multi-turn conversation that calls in-process tools and produces a final answer.

## What it shows

- `IAgent` + `StandardAgent` for tool-using LLM workflows without any provider-specific tool-calling format
- `IAgentToolRegistry` as the dispatch surface for tool invocations
- `AgentRequest` / `AgentResult` / `AgentTurn` — the full audit trail of a run, ready to persist for observability
- `AgentLoopOptions.OnTurnCompleted` for streaming/telemetry
- A pluggable scripted offline provider so the sample runs end-to-end without internet or an API key

## Run it

### Live mode (real OpenRouter calls)

```bash
export OPENROUTER_API_KEY=sk-or-...
dotnet run -c Release
```

### Offline mode (no API key)

```bash
dotnet run -c Release
```

The agent receives a prompt asking it to (1) fetch the current UTC time via the `now` tool, then (2) echo the literal `compendium` via the `echo` tool, then (3) summarise. In offline mode a scripted provider walks through those three turns deterministically.

## How it works

The agent renders a system prompt that teaches the model how to emit an `\`\`\`action` JSON block to call a tool. Each turn:

1. Send messages + system prompt to `IAIProvider.CompleteAsync(...)`
2. Parse any `\`\`\`action` block out of the response
3. If found, dispatch through `IAgentToolRegistry.InvokeAsync(...)` and feed the result back as the next user message
4. If not found, the response is the final answer — return it

Bounded by `AgentLoopOptions.MaxTurns`, `MaxTotalTokens`, and `Timeout`.
