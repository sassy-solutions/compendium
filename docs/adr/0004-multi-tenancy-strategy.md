# 0004. Multi-tenancy strategy

* Status: Accepted
* Date: 2026-04-25
* Deciders: @sassy-solutions/maintainers

## Context

Compendium is SaaS-first: every domain operation must know which tenant it acts on, and a request from tenant A must never read or write data belonging to tenant B. The tenant identifier shows up in several places that a real request can carry simultaneously:

- An explicit header (`X-Tenant-ID`) set by an API gateway or SDK.
- A subdomain (`acme.example.com`) used for branded login and marketing routes.
- One or more JWT claims — Zitadel exposes `urn:zitadel:iam:org:id`; generic OIDC providers use `org_id` or `tenant_id`.

A real request can carry several of these *at once* (e.g. JWT + subdomain on a tenant-branded portal). If they disagree, the request is either misconfigured or actively malicious — and we want a default that fails closed.

The decision lives at the framework layer because every consumer needs the same guarantees. Doing it per-app would mean each consumer re-invents tenant resolution (and at least one would get it subtly wrong).

## Decision

The framework provides `Compendium.Multitenancy` with these moving parts:

- **`TenantContext`** — a small immutable record carrying `TenantId` plus its provenance (which source produced it). Registered as a scoped service; one per request.
- **`TenantResolver` / `JwtClaimTenantResolver`** — pluggable resolvers that read the tenant from a specific source (header, subdomain, JWT). Multiple resolvers run per request.
- **`TenantConsistencyValidator`** (`ITenantConsistencyValidator`) — runs after resolution and rejects the request if two resolvers produced different non-empty tenant IDs. Default policy: **fail closed** — any disagreement → reject.
- **`TenantContextAccessor`** — the only sanctioned read path for downstream code (handlers, repositories, adapters).
- **`TenantPropagatingDelegatingHandler`** — propagates the resolved tenant on outbound HTTP calls, so adapter calls (Stripe, Listmonk, …) keep tenant context.
- **`TenantIsolation`** — guard rails used by the PostgreSQL adapter to scope every query to the resolved tenant.

The framework does **not** prescribe a physical isolation model (shared DB vs. DB-per-tenant vs. RLS). It enforces *logical* tenant scoping at every layer it owns; consumers choose physical isolation when they wire the adapter.

## Consequences

### Positive
- Cross-tenant access is impossible by default: any code path that forgets to scope a query reads from `TenantContext`, and `TenantContext` is request-scoped and validated.
- Multi-source consistency catches misconfigured gateways (e.g. JWT for tenant A but `X-Tenant-ID` for tenant B) and trivial token-replay attempts.
- Tenant context is propagated to outbound calls, so audit logs and third-party requests carry the correct identity.
- Consumers stay free to pick the physical isolation model that fits their compliance posture.

### Negative / Trade-offs
- **Detection-side attack surface.** The validator itself is now a security-critical component: a bug that returns "consistent" when sources disagree silently breaks isolation. Mitigated with focused unit tests and contract tests in `tests/Architecture`.
- **Failure mode is loud.** A configuration drift between gateway and IdP rejects every request from that path. We consider this correct behaviour, but it means careful rollout for the multi-source policy.
- **Resolver order matters subtly.** When only one source is present, that source wins; this is by design but documented because it surprises newcomers.
- Header-based resolvers must only be trusted when set by an authenticated gateway — explicit warning in `CONTRIBUTING.md`.

## Alternatives considered

- **Single source only (header).** Rejected — fine for trusted-gateway deployments, but doesn't catch the "JWT and gateway disagree" class of bugs, and rules out branded-subdomain login flows.
- **Database-per-tenant as the only model.** Rejected — strong isolation but operationally heavy (migrations × tenants), and the framework should not force that cost on small consumers.
- **Postgres Row-Level Security as the only mechanism.** Rejected as the *sole* mechanism — RLS is an excellent backstop and consumers can use it, but framework-level scoping is needed for non-Postgres adapters (Redis, third-party APIs).
- **No framework support; let consumers solve it.** Rejected — guarantees inconsistency across consumers and recreates the very class of bugs the framework exists to prevent.
