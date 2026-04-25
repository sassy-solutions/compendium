---
_layout: landing
title: Compendium
description: A pragmatic .NET framework for building event-sourced, multi-tenant SaaS applications.
---

# Compendium

> A pragmatic .NET framework for building event-sourced, multi-tenant SaaS applications.

Compendium distills years of building event-sourced SaaS into a small set of focused .NET 9 packages: DDD primitives, CQRS handlers, an event store, multi-tenancy, and ready-to-use adapters for PostgreSQL, Redis, Zitadel, Stripe, LemonSqueezy, Listmonk, OpenRouter, and ASP.NET Core.

## Why Compendium?

- **Zero-dependency Core** — Pure DDD primitives (`AggregateRoot<TId>`, `ValueObject`, `Result<T>`, `Error`) with no dependencies beyond the .NET BCL. See [ADR 0003](adr/0003-zero-dep-core.md).
- **CQRS + Event Sourcing built-in** — Command/query dispatchers, event store interfaces, and a PostgreSQL adapter wired out of the box. See [ADR 0005](adr/0005-event-sourcing-vs-state.md).
- **Multi-tenancy native** — Tenant context, resolution, and scoping baked into the primitives. See [ADR 0004](adr/0004-multi-tenancy-strategy.md).
- **Result pattern everywhere** — No control-flow exceptions. Every fallible operation returns `Result<T>`. See [ADR 0001](adr/0001-result-pattern.md).
- **Hexagonal architecture, strict** — Ports and adapters, with a clear separation between Core, Application, and Adapters. See [ADR 0002](adr/0002-hexagonal-architecture.md).
- **Modular adapters** — Pick only what you need.

## Where to start

- New to Compendium? → [Getting Started](getting-started.md)
- Want to understand the design? → [Concepts](concepts/event-sourcing.md) and [Architecture Decisions](adr/README.md)
- Looking for a specific adapter? → [Adapters](adapters/aspnetcore.md)
- Wondering what's next? → [Roadmap](roadmap.md)

## Concepts

The four pages below explain the framework's load-bearing ideas — read them in any order:

- [Event Sourcing](concepts/event-sourcing.md) — append-only log as the source of truth, aggregates, projections, snapshots
- [Hexagonal Architecture](concepts/hexagonal-architecture.md) — Core / Application / Adapters, ports vs adapters, the zero-dep Core rule
- [Result Pattern](concepts/result-pattern.md) — `Result<T>` and `Error` instead of exceptions for control flow
- [Multi-tenancy](concepts/multi-tenancy.md) — tenant context, multi-source resolution, isolation strategies

## Status

Compendium is at **`v1.0.0-preview.1`**. APIs in `Compendium.Core` and `Compendium.Abstractions.*` are intended to be stable; adapter APIs may evolve based on production feedback. See [CHANGELOG](https://github.com/sassy-solutions/compendium/blob/main/CHANGELOG.md) for what has shipped.

## License

MIT © 2026 Sassy Solutions. See [LICENSE](https://github.com/sassy-solutions/compendium/blob/main/LICENSE).
