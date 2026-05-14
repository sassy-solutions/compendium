# Coding-agent sandbox (Kubernetes)

`Compendium.Adapters.Kubernetes.Sandbox` implements
`Compendium.Abstractions.CodingAgents.Sandbox.IAgentSandbox` against a real
Kubernetes cluster. Each agent run gets its own ephemeral pod: created on
`StartAsync`, driven through `exec` for the duration of the run, and deleted
on dispose (even when the caller faults). It is the runtime substrate the
Claude Code / Codex / Gemini / OpenCode runtimes plug into via POM-427's
`ICodingAgentRuntime` port.

This doc covers operator-facing concerns: provisioning, security posture,
RBAC, NetworkPolicy, image, and observability.

---

## Architecture

```
[ ICodingAgentRuntime ] -- options --> [ IKubernetesAgentSandboxFactory ]
                                                 |
                                            Create()
                                                 v
[ IAgentSandbox ] -- StartAsync -----> creates Pod (RBAC: runtime SA)
                  -- ExecBash -------> kubectl exec into the pod
                  -- Read/Write/Edit-> exec base64 round-trip
                  -- DisposeAsync ---> deletes Pod (best-effort)
```

The sandbox does **not** require any in-cluster permissions for the agent
itself — `automountServiceAccountToken=false` is set on every pod. The
calling service (the runtime workload) holds the RBAC that lets it
create/exec/delete pods inside the sandbox namespace.

## Quick start

### 1. Cluster prerequisites

Apply the Helm chart (or the standalone manifests) shipped with the repo:

```bash
helm upgrade --install compendium-coding-agent-sandbox \
  ./deploy/sandbox/helm \
  --create-namespace \
  --set namespace=coding-agents \
  --set runtimeRbac.subjects[0].name=compendium-runtime \
  --set runtimeRbac.subjects[0].namespace=compendium-system
```

This provisions:

| Resource           | Purpose                                                                 |
|--------------------|-------------------------------------------------------------------------|
| `Namespace`        | `coding-agents`, labelled with PodSecurity `restricted`.                |
| `ServiceAccount`   | `coding-agent-sandbox`, no permissions, no auto-mounted token.          |
| `NetworkPolicy`    | Default-deny ingress; egress restricted to DNS + HTTPS:443.             |
| (optional) `CiliumNetworkPolicy` | FQDN allowlist for Anthropic / OpenAI / GitHub.            |
| `Role`/`RoleBinding` | Grants the runtime workload `pods` + `pods/exec` CRUD in this NS only. |

For ArgoCD-based clusters, see `deploy/sandbox/argocd-application.yaml`.

### 2. Build the sandbox image

```bash
docker build \
  -f deploy/sandbox/coding-agent.Dockerfile \
  -t ghcr.io/sassy-solutions/compendium/coding-agent-sandbox:1.0.0 \
  --build-arg INSTALL_DOTNET=false \
  deploy/sandbox
docker push ghcr.io/sassy-solutions/compendium/coding-agent-sandbox:1.0.0
```

The image is intentionally minimal: `bash`, `git`, `curl`, `node`, `python`,
plus an optional `.NET SDK` overlay (`--build-arg INSTALL_DOTNET=true`). It
runs as UID/GID `10001:10001` and matches the adapter defaults — override
`AGENT_UID`/`AGENT_GID` at build time if you have organisation-specific UID
allocations. Pin the image by digest in production.

### 3. Wire the adapter in the host

```csharp
using Compendium.Adapters.Kubernetes.Sandbox.DependencyInjection;

services.AddKubernetesAgentSandbox(opt =>
{
    opt.Image = "ghcr.io/sassy-solutions/compendium/coding-agent-sandbox:1.0.0";
    opt.Namespace = "coding-agents";
    opt.ServiceAccountName = "coding-agent-sandbox";
    opt.CpuLimit = "2";
    opt.MemoryLimit = "2Gi";
    opt.PodReadyTimeout = TimeSpan.FromMinutes(2);
    opt.DefaultCommandTimeout = TimeSpan.FromMinutes(10);
});
```

The Kubernetes client is registered as a singleton only if one is not already
present, so a multi-tenant namespace-provisioning module that already
provides `IKubernetes` keeps full control of authentication.

### 4. Use it from a runtime

```csharp
public sealed class ClaudeCodeRuntime : CliCodingAgentRuntime
{
    private readonly IKubernetesAgentSandboxFactory _sandboxFactory;
    public ClaudeCodeRuntime(IKubernetesAgentSandboxFactory factory) => _sandboxFactory = factory;

    public override string Engine => "claude-code";

    protected override IAgentSandbox CreateSandbox(CodingAgentRuntimeOptions options)
        => _sandboxFactory.Create();

    // ... BuildCommand / ParseStreamLine ...
}
```

## Security posture

| Surface                         | Posture                                                            |
|---------------------------------|--------------------------------------------------------------------|
| Container user                  | `runAsNonRoot=true`, UID/GID 10001 (configurable).                 |
| Privilege escalation            | `allowPrivilegeEscalation=false`, `privileged=false`.              |
| Capabilities                    | `drop: [ALL]`.                                                     |
| Root filesystem                 | `readOnlyRootFilesystem=true`; only `/workspace` is writable.      |
| Seccomp                         | `RuntimeDefault`.                                                  |
| Service-account token mount     | Disabled (`automountServiceAccountToken=false`).                   |
| Network egress                  | Default deny + HTTPS allowlist; FQDN allowlist on Cilium.          |
| Network ingress                 | Default deny.                                                      |
| Resource limits                 | CPU + memory limits always set; safe defaults `1 CPU / 1Gi`.       |
| Run timeout                     | Per-command timeout, defaults to 10 min, configurable.             |
| Lifecycle                       | `DisposeAsync` deletes the pod even if the caller throws.          |

The pod **cannot** call the Kubernetes API by design. Anything sensitive
(API keys for Anthropic / OpenAI / GitHub) is injected as plain environment
variables through `SandboxOptions.Environment` by the runtime. Source these
from your secret store (e.g. ExternalSecrets) and pass them via the runtime's
auth dictionary, not via a mounted secret.

## Tenancy

Pass `SandboxOptions.TenantId` on every run; the adapter stamps the pod with
`compendium.io/tenant=<id>`. Combine with per-tenant namespaces (override
`SandboxOptions.Namespace`) when stronger isolation is required — Helm's
`namespace` value applies to a single namespace; provision one chart release
per tenant for hard isolation.

## Observability

- All pods carry `compendium.io/component=coding-agent-sandbox` and
  `app.kubernetes.io/managed-by=compendium`. Scrape per these labels.
- The adapter logs at `Warning` when the best-effort pod delete fails on
  dispose (keep an alert on a non-zero rate).
- For longer-term audit, enable Kubernetes audit logs on `pods/exec`
  inside the sandbox namespace.

## Failure modes & errors

The adapter surfaces failures through the standard `Result` pattern with
error codes prefixed `sandbox.kubernetes.*`:

| Code                            | Meaning                                                                 |
|---------------------------------|-------------------------------------------------------------------------|
| `pod_create_failed`             | API server rejected the pod (quota, admission webhook, image policy).   |
| `pod_read_failed`               | API server unreachable mid-readiness poll.                              |
| `pod_terminal`                  | Pod went `Failed`/`Succeeded` before becoming `Running`.                |
| `pod_ready_timeout`             | Pod never reached `Running` within `PodReadyTimeout`.                   |
| `exec_failed`                   | `pods/exec` upgrade or stream failed.                                   |
| `toolchain_missing`             | The container image lacks `base64` (image hardening regression).        |
| `file_not_found`                | Read/edit target doesn't exist.                                         |
| `read_failed` / `write_failed`  | Generic file IO error inside the pod.                                   |
| `decode_failed`                 | Returned bytes weren't valid base64 (shell corruption).                 |
| `edit_no_match` / `edit_not_unique` | Edit guarantees broken — substring wasn't found, or matched >1.     |
| `already_started` / `not_started` / `disposed` | Lifecycle misuse.                                         |

When a `Result<SandboxResult>` is `Success` but `ExitCode != 0`, the
command itself failed — the sandbox dispatched it correctly. Inspect
`Stderr` / `ExitCode` on the result.

## Tests

- **Unit tests** (`tests/Unit/Compendium.Adapters.Kubernetes.Sandbox.Tests`)
  exercise `PodSpecFactory`, naming, DI registration, and architecture guards
  — no cluster required.
- **Integration tests**
  (`tests/Integration/Compendium.Adapters.Kubernetes.Sandbox.IntegrationTests`)
  spin up a single-node k3s via Testcontainers and exercise the full
  exec/read/write/edit cycle. They auto-skip when Docker isn't available, so
  the suite is safe to run on contributors' laptops.

```bash
dotnet test tests/Unit/Compendium.Adapters.Kubernetes.Sandbox.Tests/
dotnet test tests/Integration/Compendium.Adapters.Kubernetes.Sandbox.IntegrationTests/
```

## Related ADRs / tickets

- POM-427 — `IAgentSandbox` / `IAgentRuntimeRegistry` ports (merged).
- POM-431 — this adapter.
- POM-B1 — `IAgent` / `StandardAgent` ReAct primitives that consume this
  sandbox transitively through `ICodingAgentRuntime`.
