# 0003. Zero-dependency Core

* Status: Accepted
* Date: 2026-04-25
* Deciders: @sassy-solutions/maintainers

## Context

`Compendium.Core` is referenced (directly or transitively) by every other Compendium package and by every consumer application. Anything we add to `Core`'s dependency graph propagates to:

- Every CQRS handler in every consumer.
- Every test project that touches a domain primitive.
- Every adapter, regardless of which third-party SDK it actually wraps.

Three risks follow from that fan-out:

1. **Version conflicts.** A `Microsoft.Extensions.*` minor bump in Core can collide with a consumer pinned to a different version, especially in older `netstandard2.0` consumers.
2. **Surface coupling.** Pulling Newtonsoft.Json into Core means every consumer ships Newtonsoft, even those standardised on `System.Text.Json`.
3. **Supply-chain blast radius.** A compromised package in Core's graph reaches every Compendium user. Reducing the graph reduces the attack surface.

A framework's core domain primitives — aggregates, value objects, `Result<T>`, `Error` — don't *need* anything beyond the .NET BCL. Allowing dependencies "because it's convenient" is a one-way door.

## Decision

`Compendium.Core` has **zero NuGet package references**. Only types from the .NET BCL (the runtime that ships with the target TFM) are allowed.

Concretely, the rule covers:

- No `Microsoft.Extensions.*` (logging, DI, options, configuration). Core types take primitives, not `ILogger<T>`.
- No JSON libraries (`Newtonsoft.Json`, `System.Text.Json` is BCL — but Core does no JSON).
- No FP libraries (`LanguageExt`, `OneOf`, …) — we ship our own `Result<T>` / `Error` (see [ADR 0001](0001-result-pattern.md)).
- No reflection-heavy mappers, no source generators consumed by Core itself.
- The `Compendium.Core.csproj` is the source of truth: it has an `<!-- No external dependencies for Core -->` marker and contains no `<PackageReference>` entries other than `InternalsVisibleTo` plumbing.

Logging, telemetry, and DI plumbing live in `Compendium.Application`, `Compendium.Infrastructure`, or the adapters — never in `Core`.

## Consequences

### Positive
- `Compendium.Core` works on every TFM we target with zero version-conflict risk.
- The supply-chain attack surface for the most-imported package is the .NET BCL itself.
- Pure Core forces domain code to be expressed in domain terms — if you reach for a logger inside an aggregate, you've made a design mistake.
- Cold-start cost is minimal; Core can be loaded into trim-aggressive contexts (AOT, tiny self-contained apps).

### Negative / Trade-offs
- We re-implement small utilities that already exist in popular libraries (`Result<T>`, `Error`, simple guards). Accepted — they're small, stable, and tested.
- Domain code can't log or emit metrics directly; it must surface state and let the orchestration layer handle observability. We consider this a feature, not a bug.
- Contributors must occasionally be told "no, that NuGet package can't go in Core". Codified in `CONTRIBUTING.md` and enforced in PR review and architecture tests.

## Alternatives considered

- **Allow `Microsoft.Extensions.*` in Core.** Rejected — couples Core to the ASP.NET Core release cadence and ecosystem, even for consumers who don't use ASP.NET Core. The ergonomic win (built-in `ILogger<T>`) is small; the lock-in is large.
- **Allow `System.Text.Json` for serialisation helpers.** Rejected — Core doesn't need to serialise. Serialisation is an adapter concern (event store, transport).
- **Allow a tiny FP helper library (e.g. LanguageExt.Core).** Rejected — see [ADR 0001](0001-result-pattern.md). The dependency cost outweighs the convenience for the four or five types we'd use.
- **No formal rule, just discipline.** Rejected — without an enforced rule, "just one small dep" accumulates and the property is silently lost.
