# Load tests

The `Compendium.LoadTests` project (under `tests/LoadTests/Compendium.LoadTests`)
hosts a small collection of [NBomber 6](https://nbomber.com) scenarios that
exercise the in-memory implementations of the framework's hot-path
abstractions. Their purpose is **informational baselining** — they measure
the cost of the framework's own code paths on a developer machine, with no
external dependencies (no PostgreSQL, no Redis, no network).

These scenarios are **not gated in CI**. They are intended to be run
manually before merging a change that could plausibly affect throughput
(touching `IEventStore`, `IProjectionManager`, `IIdempotencyStore`, or any
adapter on the request path), and to be cited in the PR description for
review.

## Running a scenario

```bash
dotnet run --project tests/LoadTests/Compendium.LoadTests -c Release -- \
    --scenario <name> [--duration 30s] [--artifacts artifacts/load] [--no-warmup]
```

Flags:

- `--scenario <name>` (required) — one of `eventstore`, `projection`,
  `idempotency`, `tenant`.
- `--duration <value>` — total run length. Accepts `30s`, `2m`, `500ms`, or
  a bare integer interpreted as seconds. Default: `15s`.
- `--artifacts <dir>` — output directory. Default: `artifacts/load`.
- `--no-warmup` — skip the 2-second warm-up phase that NBomber would
  otherwise prepend to the run.

Each run produces:

| File                                                | Producer        |
|-----------------------------------------------------|-----------------|
| `<artifacts>/<scenario>-report.html`                | NBomber         |
| `<artifacts>/<scenario>-report.{csv,md,txt}`        | NBomber         |
| `<artifacts>/<scenario>.json`                       | This project    |

The JSON summary is what to commit/share for run-over-run comparisons; the
HTML report is the human-readable view.

## Scenarios

### `eventstore` — `IEventStore.AppendEventsAsync` throughput

Repeatedly appends a 10-event batch to a fresh aggregate against the
in-memory `InMemoryEventStore`. 32 NBomber copies run in parallel, so this
exercises the writer-lock contention.

- **Metric**: append operations per second (NBomber `Ok.Request.RPS`).
- **Derived**: events per second &asymp; `Ok.Request.RPS &times; 10`.
- **Measured dev-box baseline (Apple M-series, .NET 9, Release, 10&#8202;s
  run)**: &asymp; **60&#8202;000 ops/s** &rarr; **&asymp; 600&#8202;000 events/s**
  sustained, mean latency 1&#160;ms, p99 &asymp; 15&#160;ms.

### `projection` — projection catch-up rate

Pre-populates a single aggregate with 5&#8202;000 events, then on each
NBomber iteration resets a `ProjectionBase`-derived projection and runs
`ProjectionManager.RebuildProjectionAsync` from version 0 to N. Run with a
single copy because each iteration is intentionally heavy (full replay).

- **Metric**: rebuild iterations per second + per-iteration latency
  (NBomber latency percentiles).
- **Derived**: events processed per second &asymp; `5_000 / mean_latency_seconds`.
- **Measured dev-box baseline**: &asymp; **290 rebuilds/s** &rarr;
  **&asymp; 1.5&#8202;M events/s** for the `CountingProjection` (which does
  only a counter update), mean iteration 3&#160;ms, p99 &asymp; 5&#160;ms.
  Real projections will be lower — this measures the manager + replay
  overhead, not the projection body.

### `idempotency` — `IIdempotencyStore` reads/writes under concurrency

64 NBomber copies hammering an in-memory `IIdempotencyStore` with an 80/20
read/write split, against 10&#8202;000 pre-populated keys.

- **Metric**: lookup operations per second + per-call latency percentiles.
- **Measured dev-box baseline**: &asymp; **1.3&#8202;M ops/s**, mean latency
  &asymp; 30&#160;&micro;s, p95 &asymp; 10&#160;&micro;s, p99 &asymp; 2.5&#160;ms
  (the store is a `ConcurrentDictionary`; the long-tail comes from GC pauses).
- **What this tells you**: the abstraction itself adds negligible cost; if
  a real Redis-backed run is dramatically slower, the bottleneck is in the
  network / serialisation, not in the framework's idempotency contract.

### `tenant` — `CompositeTenantResolver` chain throughput

64 NBomber copies driving a `CompositeTenantResolver` ( header &rarr; host )
backed by an `InMemoryTenantStore` of 200 tenants. The traffic mix is
deliberate: 60&#160;% header hits (fast path), 20&#160;% host hits (chain
walks past the header resolver), 20&#160;% misses (chain walks the full
list before returning `null`).

- **Metric**: resolutions per second, with mean and p99 latency.
- **Measured dev-box baseline**: &asymp; **2.9&#8202;M resolutions/s**, mean
  latency &asymp; 50&#160;&micro;s, p95 &asymp; 80&#160;&micro;s.
- **What this tells you**: walking a two-resolver chain on every request
  adds tens of nanoseconds per call against the in-memory store; it is not
  a scaling concern.

## CI policy

These scenarios are **excluded** from `dotnet test` invocations because
they are an executable, not a test project (`<IsTestProject>false</IsTestProject>`
in the csproj). The CI pipeline should still build the project (it is in
`Compendium.sln`); use a test filter that targets `tests/Unit/` and
`tests/Integration/` directly when running tests in CI.

If a scenario is added that requires a backing service (PostgreSQL, Redis),
keep it self-contained — start the dependency in `Build(...)` (e.g. via
Testcontainers) and document it here so reviewers know what running it
implies.
