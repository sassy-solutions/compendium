# Compendium.Adapters.Kubernetes.Sandbox

Kubernetes adapter for `Compendium.Abstractions.CodingAgents.Sandbox.IAgentSandbox`.

Provisions an ephemeral, non-root pod per agent run; drives it through
`pods/exec` for shell + file ops; deletes the pod on dispose (even when the
caller throws). The runtime substrate Claude Code / Codex / Gemini / OpenCode
runtimes plug into through POM-427's `ICodingAgentRuntime` port.

```csharp
services.AddKubernetesAgentSandbox(opt =>
{
    opt.Image = "ghcr.io/sassy-solutions/compendium/coding-agent-sandbox:1.0.0";
    opt.Namespace = "coding-agents";
    opt.ServiceAccountName = "coding-agent-sandbox";
});
```

Resolve `IKubernetesAgentSandboxFactory` from DI and call `Create()` to get a
fresh `IAgentSandbox` per run. Full operator guide:
[docs/operations/coding-agent-sandbox.md](../../../docs/operations/coding-agent-sandbox.md).
