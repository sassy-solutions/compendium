# 02 — Multi-tenant with PostgreSQL

Two tenants share a single `event_store` table; isolation is enforced by `PostgreSqlEventStore` reading the active `ITenantContext` and stamping every row with `tenant_id`. Reading events for a stream while another tenant is active returns an empty result.

## What it shows

- `AddCompendiumMultitenancy()` registers `ITenantContext` / `ITenantContextSetter`
- `AddPostgreSqlEventStore(...)` configures the adapter with `AutoCreateSchema = true`
- `ITenantContextSetter.SetTenant(...)` flips the active tenant per operation
- The exact same aggregate ID is invisible across tenants — proving row-level isolation

## Prerequisites

- .NET 9 SDK
- Docker (only used to run a throw-away PostgreSQL 16 container on port `5433`)

## Run it

```bash
# 1. Start PostgreSQL (port 5433 to avoid clashes with your local dev DB).
docker compose up -d

# 2. Run the sample.
dotnet run -c Release

# 3. (When you're done) tear it down.
docker compose down -v
```

If Postgres isn't reachable, `Program.cs` prints a clear instruction and exits with code 1 — no silent crash.

## Expected output

```text
=== Compendium MultiTenant + PostgreSQL ===

  ✓ Tenant acme    placed order ...
  ✓ Tenant globex  placed order ...

  • Tenant acme    read <acme-id>   → 1 event(s)
      - OrderPlaced v1 @ ...
  • Tenant globex  read <globex-id> → 1 event(s)
      - OrderPlaced v1 @ ...

  • Tenant globex  read <acme-id>   → 0 event(s) (cross-tenant — expected: empty)

Done.
```

## Going further

- See `src/Adapters/Compendium.Adapters.PostgreSQL/` for the schema and concurrency model.
- See `docs/concepts/multi-tenancy.md` for resolution strategies (header, host, JWT claim).
- See `docs/adapters/postgresql.md` for production tuning (pool sizes, timeouts).
