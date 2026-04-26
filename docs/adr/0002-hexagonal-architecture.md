# 0002. Hexagonal architecture (strict)

* Status: Accepted
* Date: 2026-04-25
* Deciders: @sassy-solutions/maintainers

## Context

Compendium powers an event-sourced, multi-tenant SaaS (Nexus). The codebase has to remain testable and replaceable across a long horizon: identity providers change (Zitadel today, something else tomorrow), billing providers change, persistence may evolve, and consumers integrate with a long tail of external systems.

Two forces dominate:

1. **Domain stability.** The event-sourced domain — aggregates, invariants, events — should be immune to churn from any specific HTTP framework, ORM, or third-party SDK.
2. **Adapter swappability.** We ship multiple adapters for the same port (PostgreSQL today, others foreseeable; Listmonk, LemonSqueezy, OpenRouter, …). Consumers must be able to pick a subset, swap one out, or stub one in tests without touching domain code.

A naive "layered" architecture where `Application` references EF Core, or where `Core` knows about HTTP, would couple us to those choices for the lifetime of the framework.

## Decision

Strict hexagonal (ports & adapters):

- **Core** (`Compendium.Core`) — domain primitives, aggregates, value objects, `Result<T>`, `Error`, domain events. Zero NuGet dependencies (see [ADR 0003](0003-zero-dep-core.md)).
- **Abstractions** (`Compendium.Abstractions.*`) — ports as interfaces only. One assembly per concern: `Identity`, `Billing`, `Email`, `AI`. No implementations, no transitive SDK dependencies.
- **Application** (`Compendium.Application`) — orchestration: CQRS dispatchers, handlers, pipelines. Depends on Core + Abstractions, never on a concrete adapter.
- **Infrastructure** (`Compendium.Infrastructure`) — generic infrastructure building blocks (projections, outbox, caching primitives) that aren't tied to a specific vendor.
- **Adapters** (`Compendium.Adapters.*`) — concrete implementations: PostgreSQL event store, Redis cache, Zitadel OIDC, ASP.NET Core middleware, Listmonk, LemonSqueezy, OpenRouter. Each adapter implements ports from `Abstractions.*`.
- **Multitenancy** (`Compendium.Multitenancy`) — cross-cutting tenant resolution and scoping (see [ADR 0004](0004-multi-tenancy-strategy.md)).

Direction of dependency is enforced: **Adapters → Application → Abstractions → Core**. Core never knows that adapters exist. The dependency rule is checked by the architecture tests in `tests/Architecture`.

## Consequences

### Positive
- Unit tests for handlers and aggregates run in-memory with fakes — no database, no HTTP, no Docker.
- Replacing an adapter (e.g. swapping email provider) is a one-line registration change; nothing in `Core`, `Abstractions`, or `Application` moves.
- Clear extension story: a consumer ships its own `Compendium.Adapters.*` package and registers it.
- The boundary forces designers to name each port explicitly, which surfaces missing abstractions early.

### Negative / Trade-offs
- More boilerplate than a "just call EF Core directly" style: DI registration, mapping between domain and persistence shapes, port definitions before any code runs.
- More projects in the solution → slower cold builds, more `csproj` files to maintain.
- Newcomers need to learn "where does this go?" (port vs. adapter vs. application service). Documented in `CONTRIBUTING.md`.
- Some integrations are awkward to fit into a port (e.g. webhooks initiated by the third party); we accept adapter-specific surface there.

## Alternatives considered

- **Onion architecture.** Rejected — semantically close, but the canonical layering is fuzzier about whether infrastructure depends on application or vice versa. We want the boundary explicit, not "concentric".
- **Clean Architecture (Uncle Bob).** Rejected — adds a `UseCases` layer between application and entities that, for a framework (not an application), duplicates what handlers already do.
- **Layered "N-tier" (Web → Service → Data).** Rejected — couples the domain to a persistence shape and bakes the HTTP framework into the dependency graph.
- **No architectural boundary; one assembly.** Rejected — fine for a single app, fatal for a framework whose users will pick adapters à la carte.
