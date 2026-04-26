# Changelog

All notable changes to Compendium will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0-preview.4] - 2026-04-27

### Changed

- **Projections can now use DI dependencies.** `IProjectionManager.RegisterProjection<T>()`,
  `IProjectionManager.RebuildProjectionAsync<T>()`,
  `ILiveProjectionProcessor.RegisterProjection<T>()`, and
  `ServiceCollectionExtensions.AddProjection<T>()` no longer require
  `where TProjection : IProjection, new()`. Projections are resolved through the
  injected `IServiceProvider`, so any constructor dependency (logger, connection
  string, cache, metrics) is supported. Existing parameterless projections keep
  working as long as they are registered in DI; `AddProjection<T>()` now uses
  `TryAddSingleton<T>()` to register them automatically. Resolves #35.

### Added

- **Saga pattern, two flavors clearly separated.** Compendium now ships two
  distinct saga abstractions so users don't have to guess which kind they're
  using:
  - `Compendium.Abstractions.Sagas.ProcessManagers` — **DDD orchestration saga**
    (Vaughn Vernon's "Process Manager"): a stateful coordinator that groups
    aggregate operations inside one bounded context. Includes
    `ProcessManager<TState>` base class, `ProcessManagerOrchestrator`, and
    in-memory + PostgreSQL repositories.
  - `Compendium.Abstractions.Sagas.Choreography` — **Event-driven saga**:
    `IHandle<TEvent>` handlers, `IChoreographyRouter`, `IChoreographyContext`
    with correlation/causation propagation, `[Compensation]` metadata.
- `Compendium.Adapters.PostgreSQL.Sagas` — `PostgresProcessManagerRepository`
  with auto-schema creation, JSONB state snapshot, multi-tenant aware.
- DI helpers: `services.AddProcessManagers()`,
  `services.AddEventChoreography(...)`, and
  `services.AddPostgreSqlProcessManagerRepository(...)`.
- `docs/sagas.md` — decision tree, side-by-side comparison, migration guide
  from the deprecated `ISaga` API.

### Deprecated

- `Compendium.Application.Saga.ISaga`, `ISaga<TData>`, `SagaOrchestrator`,
  `ISagaOrchestrator`, `ISagaRepository`, `ISagaStepExecutor`,
  `ISagaFactory<,>` — kept with `[Obsolete]` for one minor version. The legacy
  API conflated orchestration and choreography; use `IProcessManager` or
  `IEventChoreography` instead. Removal scheduled for v1.0.

## [1.0.0-preview.3] - 2026-04-26

### Added

- Documentation site (DocFX, multi-version, GitHub Pages) (#17).
- 5 Architecture Decision Records (#14).
- Public `ROADMAP.md` (#15).
- Getting-started guide and 3 runnable samples (#20).
- 4 concept pages: event sourcing, hexagonal architecture, Result pattern, multi-tenancy (#21).
- 8 adapter how-to guides (AspNetCore, LemonSqueezy, Listmonk, OpenRouter, PostgreSQL, Redis, Stripe, Zitadel) (#22).

### Changed

- CodeQL Default Setup switched from `default` to `extended` query suite — adds maintainability/quality queries on top of security (csharp + actions).
- Dependabot now skips semver-major bumps on `Microsoft.Extensions.*`, `Microsoft.AspNetCore.*`, `Serilog.Settings.Configuration`, and `System.Text.Json` until the project moves to .NET 10 alongside Nexus (#25). Patch and minor bumps continue to flow.

### Security

- Pinned `softprops/action-gh-release` to commit SHA in `.github/workflows/release.yml` (#16, CodeQL `actions/unpinned-tag`, CWE-829, alert #28). 3rd-party action refs are now immutable.

## [1.0.0-preview.2] - 2026-04-25

### Added

- `Compendium.Adapters.Shared` — PII masking utilities used across adapters (introduced in #3).

### Changed

- Dependabot updates: `actions/upload-artifact` 4→7 (#4), `softprops/action-gh-release` 2→3 (#5), `actions/checkout` 4→6 (#6), `actions/cache` 4→5 (#7).
- OSS governance: CODEOWNERS, PR/issue templates, `SECURITY.md`, Code of Conduct, Dependabot config.

### Security

- CI: minimal `permissions:` block on workflows (#1, CodeQL `actions/missing-workflow-permissions`).
- Sanitize user-controlled path in tenant validation logs (#2, CodeQL `cs/log-forging`).
- Remove email from adapter logs for GDPR data minimization (#3, CodeQL `cs/exposure-of-sensitive-information`, 14 alerts closed).

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

[Unreleased]: https://github.com/sassy-solutions/compendium/compare/v1.0.0-preview.4...HEAD
[1.0.0-preview.4]: https://github.com/sassy-solutions/compendium/releases/tag/v1.0.0-preview.4
[1.0.0-preview.3]: https://github.com/sassy-solutions/compendium/releases/tag/v1.0.0-preview.3
[1.0.0-preview.2]: https://github.com/sassy-solutions/compendium/releases/tag/v1.0.0-preview.2
[1.0.0-preview.1]: https://github.com/sassy-solutions/compendium/releases/tag/v1.0.0-preview.1
