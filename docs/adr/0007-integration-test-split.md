# 0007. Refactor integration tests for adapter extraction

* Status: Accepted
* Date: 2026-05-14
* Deciders: @sassy-solutions/maintainers

## Context

[ADR-0006](0006-multi-repo-adapter-split.md) split five heavy adapters into per-adapter repositories. Two remain in this repository — `Compendium.Adapters.PostgreSQL` and `Compendium.Adapters.Redis` — because their tests are entangled with the framework's `tests/Integration/Compendium.IntegrationTests/` suite.

An audit of that suite (2026-05-14) classified the 24 test classes :

| Category | Files | Tests | What |
|---|---|---|---|
| Generic / InMemory | 8 | 43 | CQRS pipelines, choreography, telemetry — already use in-memory |
| Truly PG-specific | 3 | 20 | `PostgreSqlEventStoreIntegrationTests`, `PostgresProcessManagerStateReloadTests`, `ProjectionManagerIntegrationTests` (checkpoint table) |
| Truly Redis-specific | 2 | 10 | `RedisIdempotencyStoreIntegrationTests`, `ConcurrentIdempotencyE2ETests` |
| Framework testing via PG | 10 | 47 | `OrderLifecycle`, `MultiTenancy`, `ProjectionRebuild`, `SagaRetry`, `Concurrency`, `ErrorHandling`, `LiveProjection`, `Security/TenantIsolation`, `SnapshotMidStream`, `ProjectionRebuildEdgeCases` |
| Framework testing via PG+Redis | 1 | 6 | `IdempotencyE2ETests` |

The "Framework testing via PG/Redis" category is the problem. These tests aren't checking what PostgreSQL or Redis does — they're checking what the framework does, with PG/Redis as a concrete storage implementation. They were written against PG because (a) it was available and (b) `InMemoryEventStore` didn't (and arguably still doesn't) satisfy all the semantic constraints they exercise.

Without resolving this, extracting `Compendium.Adapters.PostgreSQL` either :

1. **Leaves the framework with no end-to-end coverage of CQRS / sagas / projections.** A regression in `ProcessManagerOrchestrator` would not be caught by any test that lives in this repo. Unacceptable.
2. **Duplicates the entire integration suite into both `compendium-adapter-postgresql` and `compendium-adapter-redis`.** Maintenance burden, drift hazard, and confusion about which copy is canonical.

## Decision

Refactor `tests/Integration/Compendium.IntegrationTests/` so that **all framework-behaviour E2E scenarios use the in-memory implementations by default**, and lift the PG- and Redis-specific tests out to the respective adapter repositories.

Concretely :

1. The framework's `InMemoryEventStore`, `InMemoryIdempotencyStore`, and `InMemoryProjectionCheckpointStore` must satisfy the same semantic contracts that the framework's E2E scenarios exercise. Specifically:
   - Optimistic concurrency on `AppendEventsAsync(expectedVersion)` returning `Conflict`.
   - Projection rebuild from a position checkpoint (idempotent re-replay).
   - Snapshot mid-stream (rehydration from snapshot + tail events).
   - Tenant isolation (per-tenant streams).
   - Idempotency-key TTL semantics.

   Any gap between InMemory and Postgres semantics is treated as a framework bug, not an adapter feature.

2. Framework-behaviour E2E tests (10 PG + 1 PG/Redis + 0 Redis-only) get refactored to construct `InMemoryEventStore` / `InMemoryIdempotencyStore` instead of taking the PG/Redis fixture. They live in `tests/Integration/Compendium.IntegrationTests/`.

3. Adapter-specific tests (3 PG + 2 Redis) move to the adapter repositories :
   - `compendium-adapter-postgresql/tests/Integration/Compendium.Adapters.PostgreSQL.IntegrationTests/`
   - `compendium-adapter-redis/tests/Integration/Compendium.Adapters.Redis.IntegrationTests/`
   These keep their `[RequiresDockerFact]` markers and the existing fixtures (`PostgreSqlFixture`, `RedisFixture`) which move with them.

4. Each adapter repo also gets **one smoke E2E** (`OrderLifecycleSmokeE2ETests` for PG, `IdempotencySmokeE2ETests` for Redis) — a representative scenario from the framework's E2E suite, ported to use the real adapter. This is the cross-cutting contract test that proves "the framework still works when wired through this adapter".

5. The framework's `tests/Integration/Compendium.IntegrationTests/Fixtures/` directory keeps only InMemory-related fixtures. `PostgreSqlFixture.cs`, `RedisFixture.cs`, and `RequiresDockerAttribute.cs` move to the adapter repos (the docker attribute is duplicated; it's a 60-line file with no logical owner besides "tests that need Docker").

## Consequences

### Positive

- **Framework can release v1.0.0 with a fully unit+integration-tested behaviour surface that requires no Docker.** Contributors run `dotnet test` locally without infrastructure.
- **Each adapter repo owns its own integration verification.** A PR to `compendium-adapter-postgresql` runs its own smoke E2E + adapter-specific tests; not coupled to framework CI.
- **No duplication of the framework E2E corpus.** The 47+6 framework tests live in one place — the framework repo — using InMemory.
- **Forces the InMemory implementations to be first-class citizens.** Any gap is a real bug. Today they may be silently weaker than PG; making them equal makes the framework more testable for downstream consumers (who can use InMemory in *their* tests).

### Negative

- **InMemory has to actually match Postgres semantics for the assertions the tests make.** This may require non-trivial implementation work — projection checkpoint storage, snapshot persistence, etc. Estimated 1-2 days for the InMemory upgrades.
- **The smoke E2E in each adapter repo duplicates ONE scenario.** Accepted — the duplication is bounded and intentional, and exists precisely to verify the adapter against a known-good framework behaviour.
- **`[RequiresDockerFact]` semantics needs to live in two repos.** Minor duplication; the file is 60 LOC with no logic.

### Neutral

- The "Framework testing via PG" tests **lose visibility into PG-specific edge cases**. That's correct — those edge cases belong in adapter-specific tests, which the adapter repo owns. If a PG SERIALIZABLE-level concurrency bug exists, it should be a test in `compendium-adapter-postgresql`, not a framework test.

## Alternatives considered

- **Path A : duplicate the suite into both adapter repos.** Rejected — see Negative #2.
- **Path C : leave PG+Redis in the framework forever.** Rejected — ADR-0006 explicitly schedules their extraction. Indefinite delay is implicit reversal.
- **Run framework E2E tests against both InMemory and Postgres via `[Theory] [InlineData(typeof(InMemoryEventStore))] [InlineData(typeof(PostgresEventStore))]`.** Rejected — the framework would still depend on the PostgreSQL package; doesn't help extraction. Could be done as a separate adapter-repo concern.
- **Use Testcontainers' PostgreSqlContainer in framework CI but consume Compendium.Adapters.PostgreSQL via NuGet.** Rejected — same problem (framework still pulls in the adapter package transitively for tests).

## Migration plan

1. **PR — InMemory semantic gaps audit.** Test-only: for each framework E2E currently using PG, write the equivalent against InMemory and observe what breaks. Document the gaps.
2. **PR(s) — InMemory implementation upgrades.** Address each gap: optimistic concurrency, projection checkpoint store, snapshot store, tenant isolation. One PR per concern. Existing PG-using tests stay green throughout.
3. **PR — refactor framework E2E to InMemory.** The 10 PG-using tests and 1 PG+Redis test switch fixtures. PG-specific tests are not touched.
4. **PR — extract `compendium-adapter-postgresql` repo.** Same pattern as the five previous extractions: filter-repo for src + adapter-specific tests + fixture; new sln; NuGet metadata; tag preview.9.
5. **PR — extract `compendium-adapter-redis` repo.** Same.
6. **PR — framework cleanup.** Remove the PG/Redis src dirs and adapter-specific tests; flip the framework gate to ≥90% line on the unit-testable surface (no exceptions); update CHANGELOG.
7. **Tag `v1.0.0` on framework + each adapter repo.**

ETA : 2–3 days of focused work.

## How to use this ADR

When opening a PR that touches `tests/Integration/Compendium.IntegrationTests/`, the rule is :

- Adding a test for **framework behaviour** (CQRS pipeline, sagas, projections, security, multi-tenancy) → write it against InMemory.
- Adding a test for **adapter behaviour** (transaction semantics, schema migrations, vendor quirks) → it goes in the adapter repo, not here.

When in doubt : if the test would still make sense against an in-memory store, it's a framework test. If it specifically requires the adapter's vendor behaviour, it's an adapter test.
