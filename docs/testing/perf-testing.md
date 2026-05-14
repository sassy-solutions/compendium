# Performance testing

Compendium has **zero wall-clock assertions in its xUnit test suites**. Performance is measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) in dedicated projects under `tests/Perf/`.

## Why

Wall-clock assertions like `stopwatch.ElapsedMilliseconds.Should().BeLessThan(N)` are a known anti-pattern in unit-test suites:

- Self-hosted runners, GitHub-hosted runners, developer laptops, and coverage-instrumented runs all have different baseline performance. A threshold tuned for one will flake on another.
- Concurrency on shared CI fluctuates: a noisy neighbour pod gets you a 3× slowdown one run, a 1× the next.
- "Relaxing" the threshold to suppress the flake doesn't measure anything useful — it just hides regressions until they're catastrophic.
- xUnit runs each test once; you cannot get a statistically meaningful number from a single sample.

The historical workaround was `gh pr merge --admin` to bypass the flake. That's not a durable solution: it normalises ignoring CI, and once one test is flaky everyone stops trusting CI.

## Where benchmarks live

```
tests/Perf/
├── Compendium.Core.PerfTests/
│   ├── Compendium.Core.PerfTests.csproj
│   ├── Program.cs                          (BenchmarkRunner entry point)
│   ├── Fixtures.cs                         (minimal test fixtures)
│   └── Benchmarks/
│       ├── ResultBenchmarks.cs             (Result, Error, extensions)
│       ├── DomainPrimitiveBenchmarks.cs    (Entity, ValueObject, AggregateRoot)
│       ├── DomainRuleBenchmarks.cs         (BusinessRule, Specification)
│       └── EventBenchmarks.cs              (DomainEvent, IntegrationEvent)
└── Compendium.Infrastructure.PerfTests/
    ├── Compendium.Infrastructure.PerfTests.csproj
    ├── Program.cs
    └── Benchmarks/
        ├── EncryptionBenchmarks.cs         (AES encrypt / decrypt)
        └── ResilienceBenchmarks.cs         (CircuitBreaker, RetryPolicy)
```

## Running

Run all benchmarks for a project:

```bash
dotnet run -c Release --project tests/Perf/Compendium.Core.PerfTests
```

Filter to one benchmark class:

```bash
dotnet run -c Release --project tests/Perf/Compendium.Core.PerfTests -- --filter '*ValueObject*'
```

BenchmarkDotNet emits `BenchmarkDotNet.Artifacts/results/*.md` files with allocation tables and timing distributions.

## CI

`.github/workflows/perf-bench.yml` runs the benchmark projects nightly at 03:00 UTC and on manual dispatch. The results are uploaded as workflow artifacts (90-day retention). The workflow is **informational** — it does not gate any PR. Regressions are caught by comparing artifacts across runs.

The unit-test CI workflow (`.github/workflows/ci.yml`) does **not** run benchmarks. Benchmarks take minutes per method (BenchmarkDotNet runs each iteration N times to converge on a statistical mean); they would dominate PR-time CI.

## Policy

If you're tempted to add `stopwatch.ElapsedMilliseconds.Should().BeLessThan(N)` to a test, **don't**. Either:

1. **Write a BenchmarkDotNet method** in the appropriate `tests/Perf/*.PerfTests` project. Use `[Benchmark]` for the hot path; `[GlobalSetup]` for expensive prep; `[MemoryDiagnoser]` for allocation tracking; `[Params]` for varying input sizes.
2. **Remove the timing concern from the test entirely.** The functional assertions still belong in the xUnit test. The timing assertion does not.

If the unit-test suite finds an actual performance regression — for example, a method went from O(n) to O(n²) — the failure mode should be:

- `dotnet test` takes 30+ seconds instead of <1 second → triggers a separate investigation (the test runtime is itself the signal).
- Or: a BenchmarkDotNet regression in the nightly run (preferred — gives you a number, not a yes/no).

## Migration notes

This policy was introduced when migrating away from a pattern of CI flakes caused by 15+ wall-clock assertions across `Compendium.Core.Tests` and `Compendium.Infrastructure.Tests`. The original tests were:

- 10 `*_PerformanceTest_*` methods in `Compendium.Core.Tests` (Result, Error, Entity, ValueObject, AggregateRoot, BusinessRule, DomainEvent, IntegrationEvent, Specification, ResultExtensions)
- 4 `*_PerformanceTest_*` methods in `Compendium.Infrastructure.Tests` (ProjectionManager, Encryption, CircuitBreaker, RetryPolicy)
- 5 inline `Stopwatch.ElapsedMilliseconds.Should().BeLessThan(...)` assertions bolted onto otherwise-functional tests (InMemoryEventStore, LockingStrategy, TracingService, MetricsCollector)

The pure micro-benchmarks were migrated to `tests/Perf/`. The functional tests had their timing assertions removed; their correctness checks remain.

## Not yet migrated

- `ProjectionManager` throughput benchmark — the original 10,000-event scenario needs an `IEventStore` + `IEventDeserializer` setup that doesn't currently fit cleanly into a BenchmarkDotNet `[GlobalSetup]`. Tracked as follow-up; the original xUnit version was deleted (functional coverage is already provided by other tests in `ProjectionManagerTests`).
- `InMemoryEventStore` 1k/10k batch throughput benchmark — same reasoning; follow-up.

When migrating these, add a new benchmark class in `tests/Perf/Compendium.Infrastructure.PerfTests/Benchmarks/`.
