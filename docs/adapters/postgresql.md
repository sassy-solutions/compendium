# Compendium.Adapters.PostgreSQL

> PostgreSQL-backed event store, streaming event store, projection store, and projection checkpoint store.

## Install

```bash
dotnet add package Compendium.Adapters.PostgreSQL
```

## Configuration

`appsettings.json`:

```json
{
  "Compendium": {
    "EventStore": {
      "ConnectionString": "Host=localhost;Port=5432;Database=app;Username=app;Password=secret",
      "TableName": "event_store",
      "AutoCreateSchema": false,
      "MinimumPoolSize": 50,
      "MaximumPoolSize": 200,
      "MaxPoolSize": 200,
      "CommandTimeout": 60
    }
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.PostgreSQL;

builder.Services.AddPostgreSqlEventStore(options =>
{
    builder.Configuration.GetSection("Compendium:EventStore").Bind(options);
});

builder.Services.AddPostgreSqlProjectionStore(options =>
{
    builder.Configuration.GetSection("Compendium:EventStore").Bind(options);
});

builder.Services.AddPostgreSqlProjectionCheckpointStore(options =>
{
    builder.Configuration.GetSection("Compendium:EventStore").Bind(options);
});

// Streaming event store depends on the singleton registered above; call last.
builder.Services.AddPostgreSqlStreamingEventStore(options =>
{
    builder.Configuration.GetSection("Compendium:EventStore").Bind(options);
});
```

A connection-string overload is available for each method when you want to skip the options builder:

```csharp
builder.Services.AddPostgreSqlEventStore(
    builder.Configuration.GetConnectionString("EventStore")!);
```

### `PostgreSqlOptions`

Bound from configuration section `Compendium:EventStore`.

| Property | Default | Description |
|---|---|---|
| `ConnectionString` | `""` | PostgreSQL connection string (required). |
| `TableName` | `"event_store"` | Event storage table name. |
| `AutoCreateSchema` | `false` | Create the schema on startup. Off by default — prefer migrations. |
| `BatchSize` | `1000` | Batch size for bulk operations. |
| `CommandTimeout` | `60` | Per-command timeout, in seconds. |
| `MaxPoolSize` | `200` | Application-level concurrency limit (semaphore). |
| `MinimumPoolSize` | `50` | Npgsql minimum pool size — pre-warms connections. |
| `MaximumPoolSize` | `200` | Npgsql maximum pool size. Must be `<` server `max_connections`. |
| `ConnectionIdleLifetime` | `900` | Seconds before idle connections are closed (15 min). |
| `ConnectionLifetime` | `3600` | Connection recycling period in seconds (1 hour). |
| `ConnectionTimeout` | `30` | Seconds to wait for a connection from the pool. |
| `Keepalive` | `30` | TCP keepalive interval, in seconds. `0` disables. |
| `EnablePooling` | `true` | Enable Npgsql connection pooling. Disable only for debugging. |

## Usage

Resolve `IEventStore` (or the streaming variant) from DI and use it through the standard Compendium ports:

```csharp
public class OrderHandler
{
    private readonly IEventStore _eventStore;

    public OrderHandler(IEventStore eventStore) => _eventStore = eventStore;

    public async Task<Result<Unit>> Handle(PlaceOrder cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.OrderId, cmd.TenantId, cmd.Lines);
        return await _eventStore.AppendAsync(
            streamId: order.Id.ToString(),
            tenantId: cmd.TenantId,
            expectedVersion: 0,
            events: order.UncommittedEvents,
            ct);
    }
}
```

For projections, inject `IProjectionStore` to read/write projection state and `IProjectionCheckpointStore` to track stream position. `IStreamingEventStore` exposes a live `Subscribe`-style API for projection workers.

## Gotchas

- `AddPostgreSqlStreamingEventStore` **must** be called after `AddPostgreSqlEventStore` — it depends on the singleton registered there.
- `AutoCreateSchema` is off by default. In production, run the schema script as a migration step rather than at app start.
- Connection pooling is two-tiered: Npgsql's pool (`MinimumPoolSize`/`MaximumPoolSize`) plus a semaphore-based application limit (`MaxPoolSize`). Tune both together.
- Set `MaximumPoolSize` below your server's `max_connections`, leaving headroom for other clients.
- Multi-tenant streams: always pass `tenantId` to `AppendAsync`/`ReadAsync`. Cross-tenant reads are an explicit choice, not a default.

## See also

- [API Reference](../api/Compendium.Adapters.PostgreSQL.html)
- [Event Sourcing concepts](../concepts/event-sourcing.md)
- Sample app — coming via POM-182.
