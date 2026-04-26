# Compendium.Adapters.PostgreSQL

PostgreSQL-backed event store and projection store for Compendium. Designed for high-throughput SaaS workloads with proper connection pooling and bulk-insert support.

## Install

```bash
dotnet add package Compendium.Adapters.PostgreSQL
```

You also need a PostgreSQL 14+ instance reachable from your service.

## Configuration

```json
{
  "Compendium": {
    "EventStore": {
      "ConnectionString": "Host=localhost;Database=compendium;Username=app;Password=secret",
      "AutoCreateSchema": false,
      "BatchSize": 1000
    }
  }
}
```

```csharp
builder.Services.Configure<PostgreSqlOptions>(
    builder.Configuration.GetSection(PostgreSqlOptions.SectionName));
```

Options (`PostgreSqlOptions`):

| Option | Default | Description |
|---|---|---|
| `ConnectionString` | _required_ | Standard Npgsql connection string |
| `MaxPoolSize` | `200` | Application-level concurrency cap (SemaphoreSlim) |
| `CommandTimeout` | `60s` | Single-query timeout |
| `TableName` | `event_store` | Event store table |
| `AutoCreateSchema` | `false` | Whether to auto-create tables on startup (dev only) |
| `BatchSize` | `1000` | Bulk-write batch size |
| `MinimumPoolSize` | `50` | Npgsql min pool — prewarmed connections |
| `MaximumPoolSize` | `200` | Npgsql max pool (must be ≤ Postgres `max_connections`) |
| `ConnectionIdleLifetime` | `900s` | Idle connection close timeout |
| `ConnectionLifetime` | `3600s` | Hard connection recycle |
| `ConnectionTimeout` | `30s` | Pool wait timeout |
| `Keepalive` | `30s` | TCP keepalive (`0` to disable) |
| `EnablePooling` | `true` | Disable only for debugging |

## Usage

Once registered, the event store is available via the `IEventStore` port (from `Compendium.Abstractions`):

```csharp
public sealed class PlaceOrderHandler(IEventStore eventStore)
    : ICommandHandler<PlaceOrderCommand>
{
    public async Task<Result> Handle(PlaceOrderCommand cmd, CancellationToken ct)
    {
        var orderResult = OrderAggregate.Create(cmd.CustomerId, cmd.Amount);
        if (orderResult.IsFailure) return orderResult.Error;

        await eventStore.AppendAsync(
            orderResult.Value.Id.ToString(),
            orderResult.Value.DomainEvents,
            expectedVersion: 0,
            ct);

        return Result.Success();
    }
}
```

For schema bootstrapping in a non-dev environment, run the migrations under `tests/Integration/.../Database/` as a reference, or write your own migrations.

## Gotchas

- **`AutoCreateSchema` is false by default — on purpose.** In production you want migrations under version control, not implicit DDL on startup.
- **`MaximumPoolSize` must be smaller than the server's `max_connections`.** A microservice deployed in 10 replicas with `MaximumPoolSize: 200` is asking for 2000 connections; many Postgres deployments cap at 100–200.
- **`BatchSize: 1000` is tuned for Postgres on SSD.** On constrained hardware (small RDS), drop it to 200–500.
- **Connection idle lifetime under proxies.** If you front Postgres with PgBouncer in transaction mode, keep `ConnectionIdleLifetime` shorter than the proxy's idle timeout to avoid stale-handle errors.

## See also

- [API Reference: Compendium.Adapters.PostgreSQL.Configuration](../api/Compendium.Adapters.PostgreSQL.Configuration.html)
- [Event Sourcing concept](../concepts/event-sourcing.md)
- [`samples/02-MultiTenant-WithPostgres`](https://github.com/sassy-solutions/compendium/tree/main/samples/02-MultiTenant-WithPostgres)
