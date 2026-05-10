# Compendium.LoadTests

Baseline NBomber load scenarios for the Compendium framework's hot-path
abstractions. All scenarios run against the in-memory implementations
(no Postgres, no Redis, no network) so the suite is runnable locally with
a single `dotnet run`.

See [`docs/testing/load-tests.md`](../../../docs/testing/load-tests.md) for
the full list of scenarios, the metric each one measures, and the
expected dev-machine baseline numbers.

## Quick start

```bash
dotnet run --project tests/LoadTests/Compendium.LoadTests -c Release -- \
    --scenario eventstore --duration 30s
```

Available scenarios: `eventstore`, `projection`, `idempotency`, `tenant`.

Output is written to `artifacts/load/<scenario>.json` plus the standard
NBomber HTML / CSV / Markdown reports.

These scenarios are **informational** — they are not part of the CI test
filter, and they do not gate merges.
