# Compendium.Adapters.Redis

> Redis connection multiplexer, idempotency store, and projection checkpoint store.

## Install

```bash
dotnet add package Compendium.Adapters.Redis
```

## Configuration

`appsettings.json`:

```json
{
  "Compendium": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "DefaultDatabase": 0,
      "KeyPrefix": "compendium",
      "ConnectTimeout": 5000,
      "CommandTimeout": 5000,
      "RetryCount": 3,
      "RetryDelayMs": 1000
    }
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.Redis;

builder.Services.AddRedis(options =>
{
    builder.Configuration.GetSection("Compendium:Redis").Bind(options);
});

builder.Services.AddRedisIdempotencyStore(options =>
{
    builder.Configuration.GetSection("Compendium:Redis").Bind(options);
});

builder.Services.AddRedisProjectionCheckpointStore(
    configure: options =>
    {
        builder.Configuration.GetSection("Compendium:Redis").Bind(options);
    },
    defaultExpiration: TimeSpan.FromDays(7));
```

A connection-string overload is available for each method:

```csharp
builder.Services.AddRedis("localhost:6379");
builder.Services.AddRedisIdempotencyStore("localhost:6379");
```

### `RedisOptions`

Bound from configuration section `Compendium:Redis`.

| Property | Default | Description |
|---|---|---|
| `ConnectionString` | `"localhost:6379"` | Redis connection string. |
| `DefaultDatabase` | `0` | Redis database number. |
| `KeyPrefix` | `"compendium"` | Prefix for all keys this adapter writes. |
| `ConnectTimeout` | `5000` | Connection timeout in milliseconds. |
| `CommandTimeout` | `5000` | Command timeout in milliseconds. |
| `RetryCount` | `3` | Retry attempts for transient failures. |
| `RetryDelayMs` | `1000` | Delay between retries in milliseconds. |
| `ValidateConnectionString` | `true` | Validate the connection string at startup. |

## Usage

`IConnectionMultiplexer` is registered as a singleton — inject it directly when you need raw access:

```csharp
public class CacheReader
{
    private readonly IConnectionMultiplexer _redis;

    public CacheReader(IConnectionMultiplexer redis) => _redis = redis;

    public Task<RedisValue> GetAsync(string key) =>
        _redis.GetDatabase().StringGetAsync($"compendium:{key}");
}
```

`IIdempotencyService` (backed by `RedisIdempotencyStore`) wraps command execution so that a duplicate idempotency key returns the cached result instead of re-running the operation:

```csharp
public class CreateOrderHandler
{
    private readonly IIdempotencyService _idempotency;

    public CreateOrderHandler(IIdempotencyService idempotency) => _idempotency = idempotency;

    public Task<Result<OrderId>> Handle(CreateOrder cmd, CancellationToken ct) =>
        _idempotency.ExecuteAsync(
            key: cmd.IdempotencyKey,
            tenantId: cmd.TenantId,
            operation: () => CreateOrderInternalAsync(cmd, ct),
            ct);
}
```

`RedisProjectionCheckpointStore` (resolves `IProjectionCheckpointStore`) tracks the last processed event position per projection — the configured TTL doubles as a recovery window.

## Gotchas

- The connection multiplexer is a process-wide singleton; never `Dispose()` it from consumer code — the adapter manages its lifecycle.
- Idempotency operations surface infrastructure failures (timeouts, serialization, dropped connections) as `Result.Failure` rather than throwing. Inspect the `Error` to decide whether to retry.
- `KeyPrefix` is applied per adapter instance; if you share a Redis instance across services, give each one its own prefix to avoid collisions.
- Default checkpoint TTL is 7 days — long enough to recover most projections from a checkpoint, short enough to evict abandoned ones. Override per store if you replay frequently.
- All three registrations can be called independently; you do not need the multiplexer registration if you only want the idempotency or checkpoint store.

## See also

- [API Reference](../api/Compendium.Adapters.Redis.html)
- Sample app — coming via POM-182.
