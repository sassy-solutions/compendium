# Compendium.Adapters.Redis

Redis adapter for caching and (where useful) cross-instance coordination. Built on `StackExchange.Redis`.

## Install

```bash
dotnet add package Compendium.Adapters.Redis
```

You need a Redis 6+ instance.

## Configuration

```json
{
  "Compendium": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "KeyPrefix": "compendium",
      "DefaultDatabase": 0
    }
  }
}
```

```csharp
builder.Services.Configure<RedisOptions>(
    builder.Configuration.GetSection(RedisOptions.SectionName));
```

Options (`RedisOptions`):

| Option | Default | Description |
|---|---|---|
| `ConnectionString` | `localhost:6379` | StackExchange.Redis connection string |
| `DefaultDatabase` | `0` | Redis logical DB number |
| `ConnectTimeout` | `5000ms` | Initial connect timeout |
| `CommandTimeout` | `5000ms` | Per-command timeout |
| `RetryCount` | `3` | Retries on transient failures |
| `RetryDelayMs` | `1000ms` | Delay between retries |
| `KeyPrefix` | `compendium` | Prefix added to every key |
| `ValidateConnectionString` | `true` | Validate string at startup |

## Usage

```csharp
public sealed class CachedOrderRepository(ICacheStore cache, IOrderRepository inner)
    : IOrderRepository
{
    public async Task<Order?> FindAsync(OrderId id, CancellationToken ct)
    {
        var key = $"order:{id}";
        var cached = await cache.GetAsync<Order>(key, ct);
        if (cached is not null) return cached;

        var order = await inner.FindAsync(id, ct);
        if (order is not null)
            await cache.SetAsync(key, order, TimeSpan.FromMinutes(5), ct);

        return order;
    }
}
```

## Gotchas

- **Multi-tenancy in Redis is your responsibility.** A flat key namespace shared across tenants is a leak waiting to happen. Always include the tenant ID in the cache key (e.g. `order:{tenantId}:{orderId}`).
- **`KeyPrefix` is per-application, not per-tenant.** Use it for environment isolation (`compendium-prod`, `compendium-staging`) — not for tenancy.
- **Don't store secrets in Redis without TLS.** The default `ConnectionString` does not enable TLS; in production, use `rediss://` or set `ssl=true`.
- **`SkipSslValidation` style flags do not exist here on purpose.** If your cluster uses a self-signed cert, terminate it at a sidecar/proxy or import the CA properly.
- **AOF vs RDB.** If you use Redis as a durable store (rare for cache), set the persistence mode in the Redis config — Compendium can't influence what happens after the data leaves your service.

## See also

- [API Reference: Compendium.Adapters.Redis.Configuration](../api/Compendium.Adapters.Redis.Configuration.html)
- [Multi-tenancy concept](../concepts/multi-tenancy.md)
