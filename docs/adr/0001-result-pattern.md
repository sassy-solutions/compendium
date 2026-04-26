# 0001. Result pattern over exceptions for business errors

* Status: Accepted
* Date: 2026-04-25
* Deciders: @sassy-solutions/maintainers

## Context

The .NET ecosystem traditionally signals failure with exceptions. For a framework powering long-lived multi-tenant SaaS, throwing on every business rule violation has three concrete drawbacks we care about:

1. **CPU cost on the hot path.** Exception construction captures a stack trace; under load (CQRS handlers running tens of thousands of validations per second) this is measurable and avoidable.
2. **Flow readability.** Business errors expressed as `throw` are invisible to the type system. A reader of a handler cannot tell, without reading every called method, which failures are recoverable and which are bugs.
3. **Typing of business errors.** We need rich, structured errors (code, message, kind) that can be serialised at the API boundary, mapped to ProblemDetails, and asserted on in tests. Exceptions push us toward stringly-typed `Message` parsing or proliferating exception subclasses.

We also need a clear separation between *expected business outcomes* (validation failed, not found, conflict) and *unexpected situations* (database is down, OOM, programmer mistake) so observability and retry policies can treat them differently.

## Decision

Every fallible business operation returns `Result<T>` (or `Result` for void). Errors are represented as `Error` records with a stable `Code`, a human-readable `Message`, and a kind discriminator (validation, not-found, conflict, unauthorized, …).

- `Compendium.Core.Results.Result<T>` and `Error` live in the zero-dependency Core (see [ADR 0003](0003-zero-dep-core.md)).
- Command and query handlers, domain factories, and adapter operations all return `Result<T>`.
- Exceptions are reserved for: programmer bugs (assertion failures, never-meant-to-happen branches), infrastructure that genuinely cannot be a `Result` (cancellation, OOM), and a thin translation layer at the HTTP edge.
- `Result.Failure(...)` and `Result.Success(...)` are the only sanctioned construction paths. No `null` for "absent value" — use `Result.Failure(Error.NotFound(...))`.

## Consequences

### Positive
- Errors are part of the type signature → handler contracts are self-describing.
- Cheaper than `throw` on validation-heavy hot paths.
- Trivial to map at the boundary: one `Result.Match` → ProblemDetails or HTTP status.
- Tests assert on `Error.Code`, not on string-matched exception messages.
- Forces authors to think about each failure mode at the call site.

### Negative / Trade-offs
- More verbose than throwing — every call site decides to bubble up, recover, or transform.
- Learning curve for .NET developers used to exceptions; PR review must enforce the pattern.
- Mixing `Result` and `try/catch` in the same method requires discipline; we accept the boilerplate.
- No language-level `?` operator like Rust — composition relies on extension methods (`Map`, `Bind`, `Match`).

## Alternatives considered

- **Exceptions everywhere.** Rejected: hides failure modes from the type system, expensive on hot paths, conflates bugs with business outcomes.
- **LanguageExt `Either<L, R>`.** Rejected: large dependency surface, opinionated FP idioms, and pulls a non-trivial transitive graph into Core — incompatible with [ADR 0003](0003-zero-dep-core.md).
- **OneOf discriminated unions.** Rejected: flexible but unopinionated; we want a single canonical `Error` shape across the framework, not per-operation union types.
- **Nullable reference types as the "failure" signal.** Rejected: can express absence but not *why* — and silent `null`s are exactly what we want to eliminate.
