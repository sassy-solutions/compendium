# Changelog

All notable changes to Compendium will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0-preview.2] - 2026-04-26

### Changed

- CodeQL Default Setup switched from `default` to `extended` query suite — adds maintainability/quality queries on top of security (csharp + actions).

### Security

- Pinned `softprops/action-gh-release` to commit SHA in `.github/workflows/release.yml` (CodeQL `actions/unpinned-tag`, CWE-829). 3rd-party action refs are now immutable.

## [1.0.0-preview.1] - 2026-04-24

### Added

First public preview release of Compendium, extracted from the
[Nexus](https://github.com/sassy-solutions/Nexus) monorepo.

**Core & Abstractions**

- `Compendium.Core` — DDD primitives: `AggregateRoot<TId>`, `ValueObject`, `Result<T>`, `Error`, `IDomainEvent` (zero external dependencies).
- `Compendium.Abstractions` — Shared ports and interfaces across layers.
- `Compendium.Abstractions.Identity` — Identity provider port.
- `Compendium.Abstractions.Email` — Email sender port.
- `Compendium.Abstractions.Billing` — Billing/subscription port.
- `Compendium.Abstractions.AI` — LLM/AI provider port.

**Application & Infrastructure**

- `Compendium.Application` — CQRS dispatchers, `ICommandHandler`, `IQueryHandler`.
- `Compendium.Infrastructure` — Resilience, telemetry, serialization primitives.
- `Compendium.Multitenancy` — Tenant context, scope, hierarchy.

**Adapters**

- `Compendium.Adapters.AspNetCore` — ASP.NET Core integration (health checks, problem details, DI glue).
- `Compendium.Adapters.PostgreSQL` — Event store and projection store on PostgreSQL.
- `Compendium.Adapters.Redis` — Cache, idempotency store.
- `Compendium.Adapters.Zitadel` — Zitadel identity adapter (`Compendium.Abstractions.Identity`).
- `Compendium.Adapters.Stripe` — Stripe billing adapter (`Compendium.Abstractions.Billing`).
- `Compendium.Adapters.LemonSqueezy` — LemonSqueezy billing adapter.
- `Compendium.Adapters.Listmonk` — Listmonk email adapter (`Compendium.Abstractions.Email`).
- `Compendium.Adapters.OpenRouter` — OpenRouter AI adapter (`Compendium.Abstractions.AI`).

**Extensions & Testing**

- `Compendium.Extensions.ExternalAdapters` — Composition helpers for external adapters.
- `Compendium.Testing` — Test utilities, fixtures, fakes for downstream consumers.

### Notes

- All 19 packages target `.NET 9` and are published on [nuget.org](https://www.nuget.org/packages?q=Compendium).
- Git history preserved from the originating Nexus monorepo via `git filter-repo`.
- Full MIT license.

[Unreleased]: https://github.com/sassy-solutions/compendium/compare/v1.0.0-preview.2...HEAD
[1.0.0-preview.2]: https://github.com/sassy-solutions/compendium/releases/tag/v1.0.0-preview.2
[1.0.0-preview.1]: https://github.com/sassy-solutions/compendium/releases/tag/v1.0.0-preview.1
