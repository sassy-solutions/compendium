# 0005. Event sourcing over state-stored persistence

* Status: Accepted
* Date: 2026-04-25
* Deciders: @sassy-solutions/maintainers

## Context

The domains Compendium targets (multi-tenant SaaS, platform engineering, billing-aware workflows) share three characteristics:

1. **Audit is not optional.** Customers, ops, and compliance need to know *who* did *what* and *when*, including operations that no longer reflect the current state.
2. **Multiple read shapes per write.** A "subscription" aggregate feeds a billing dashboard, a usage report, an admin audit view, and a tenant-facing activity feed — each with a different shape and different staleness tolerance.
3. **Time-travel and replay matter.** Reproducing a bug, backfilling a new projection, or recovering a corrupted read model is routine, not exceptional.

A pure CRUD/state model loses information on every write: the previous values are gone unless we duplicate them into an audit table, and even then we've already had to predict which fields to log. Layering audit on top of CRUD ends up reinventing event sourcing badly.

The cost of event sourcing is real — schema evolution, idempotency, projection lag — but it's a cost we'd otherwise pay piecemeal in a CRUD system through audit tables, change-data-capture, and bespoke history queries.

## Decision

The domain Core is **append-only event-sourced**:

- Aggregates (`AggregateRoot<TId>` in `Compendium.Core`) raise domain events; the event stream is the source of truth.
- The PostgreSQL adapter (`Compendium.Adapters.PostgreSQL`) implements the event store. State is rebuilt by replaying events.
- Read models are **projections**: derived, eventually consistent, replayable from the event log. Live in `Compendium.Infrastructure` (generic projection plumbing) plus per-consumer projection handlers.
- Outbox pattern for cross-boundary publishing of events to external systems, ensuring at-least-once delivery without distributed transactions.
- Snapshots are an optimisation, not a substitute for the log; an aggregate's history is always reconstructible from events alone.

CRUD-shaped state *is* permitted at the edges where it makes sense (e.g. cached lookups, idempotency keys, projection checkpoints) — but never as the source of truth for a domain aggregate.

## Consequences

### Positive
- Audit comes for free: the event log *is* the audit log. Compliance queries are projections, not bolt-ons.
- Multiple read models per write are natural: add a new projection, replay history, done — no schema migration on writes.
- Bug reproduction and backfills are tractable: replay the relevant slice of the log into a sandbox.
- Aggregate invariants live entirely in the domain code that produces events, not split between code and database constraints.
- Outbox + event log gives us reliable downstream integration without 2PC.

### Negative / Trade-offs
- **Event schema migration is hard and forever.** Once an event is in the log, it's in the log. We commit to additive evolution (new fields with defaults), upcasters for breaking changes, and never editing historical events.
- **Eventual consistency is real.** Read models lag the write side. Consumers need to understand "you just wrote, but the dashboard hasn't caught up yet" and design UX accordingly.
- **Higher learning curve.** Developers familiar with EF Core CRUD need to internalise commands, events, projections, and the read/write split before they can confidently build features.
- **Tooling is thinner.** Off-the-shelf admin UIs assume CRUD; querying "current state" requires a projection, not a `SELECT`.
- **Projection rebuilds can be expensive.** Replaying years of events to seed a new read model is slow; we mitigate with snapshots and selective replays.

## Alternatives considered

- **Pure CRUD via EF Core.** Rejected — fastest path to a working app, but pushes audit, history, and read-model variety into ad-hoc tables that drift out of sync with the domain. We've lived this; it ages badly.
- **Full EF Core with change tracking + audit interceptors.** Rejected — better than naive CRUD, but the audit shape is dictated by the table shape, not by domain intent. Hard to reconstruct *why* something changed.
- **CQRS without event sourcing (state-stored writes, separate read models).** Rejected — gives us the read-shape flexibility, but we still lose history on every write and have to bolt on auditing. If we're paying for the CQRS split, the marginal cost of event sourcing is small and the upside is large.
- **Event sourcing only for select aggregates ("hybrid").** Rejected as a default — drawing the line is harder than it looks, and the CRUD aggregates inevitably grow audit needs and end up half-converted. We pick one model and apply it consistently in Core.
