# Polly Resilience Patterns

This module provides production-ready resilience patterns using [Polly 8.x](https://www.pollydocs.org/) for protecting infrastructure operations against transient failures.

## Features

- **Circuit Breaker**: Prevents cascading failures by temporarily blocking requests after repeated failures
- **Retry with Exponential Backoff**: Automatically retries transient failures with increasing delays
- **Timeout**: Cancels operations that exceed configured duration
- **Jitter**: Adds randomization to retry delays to prevent thundering herd
- **Pre-configured Pipelines**: Optimized defaults for PostgreSQL and Redis

## Quick Start

### 1. Register Services

```csharp
services.AddPollyResilience();
```

### 2. Use Factory to Create Pipelines

```csharp
public class MyService
{
    private readonly ResiliencePipeline _pipeline;

    public MyService(PollyResiliencePipelineFactory factory)
    {
        _pipeline = factory.CreatePostgreSqlPipeline();
    }

    public async Task<User> GetUserAsync(string id, CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            // Your database operation here
            return await _dbContext.Users.FindAsync(id, token);
        }, ct);
    }
}
```

## Configuration

### Default Settings

**PostgreSQL** (`PollyResilienceOptions.PostgreSqlDefaults()`):
- Timeout: 5 seconds
- Max Retry Attempts: 3
- Base Retry Delay: 100ms (exponential backoff)
- Circuit Breaker Failure Threshold: 50%
- Circuit Breaker Sampling Duration: 30 seconds
- Circuit Breaker Minimum Throughput: 10 requests
- Circuit Breaker Break Duration: 60 seconds

**Redis** (`PollyResilienceOptions.RedisDefaults()`):
- Timeout: 2 seconds (faster for cache)
- Max Retry Attempts: 3
- Base Retry Delay: 50ms (shorter for cache)
- Circuit Breaker Failure Threshold: 50%
- Circuit Breaker Sampling Duration: 30 seconds
- Circuit Breaker Minimum Throughput: 20 requests (higher for cache)
- Circuit Breaker Break Duration: 30 seconds (shorter recovery)

### Custom Configuration

```csharp
var options = new PollyResilienceOptions
{
    Timeout = TimeSpan.FromSeconds(10),
    MaxRetryAttempts = 5,
    BaseRetryDelay = TimeSpan.FromMilliseconds(200),
    CircuitBreakerFailureThreshold = 0.7, // 70% failures
    CircuitBreakerMinimumThroughput = 15,
    CircuitBreakerBreakDuration = TimeSpan.FromSeconds(90)
};

var pipeline = factory.CreatePostgreSqlPipeline(options);
```

### Disable Retries

Set `MaxRetryAttempts = 0` to disable retry policy (useful for testing):

```csharp
var options = new PollyResilienceOptions
{
    MaxRetryAttempts = 0 // Only timeout and circuit breaker
};
```

## Transient Error Detection

### PostgreSQL Transient Errors

The pipeline automatically retries these PostgreSQL errors:
- `NpgsqlException` with transient error codes (08000, 08003, 08006, 53000, 53300, 57P03)
- `TimeoutException`
- `SocketException`
- `IOException`
- Connection-related errors

### Redis Transient Errors

The pipeline automatically retries these Redis errors:
- `RedisConnectionException`
- `RedisTimeoutException`
- `RedisServerException` (READONLY, LOADING states)
- `TimeoutException`
- `SocketException`
- `IOException`

## Circuit Breaker States

### Closed (Normal Operation)
All requests pass through normally. Failures are tracked.

### Open (Blocking Requests)
After failure threshold is reached, circuit opens and blocks all requests immediately without attempting the operation. Throws `BrokenCircuitException`.

### Half-Open (Testing Recovery)
After break duration elapses, circuit allows a test request through:
- If successful → Circuit closes and normal operation resumes
- If fails → Circuit re-opens for another break duration

## Dependency Injection

### Basic Registration

```csharp
services.AddPollyResilience();
```

### Custom Configuration via Actions

```csharp
services.AddPollyResilience(
    configurePostgreSql: opts =>
    {
        opts.Timeout = TimeSpan.FromSeconds(10);
        opts.MaxRetryAttempts = 5;
    },
    configureRedis: opts =>
    {
        opts.Timeout = TimeSpan.FromSeconds(3);
        opts.MaxRetryAttempts = 2;
    }
);
```

### Custom Configuration via Options

```csharp
var pgOptions = new PollyResilienceOptions { /* ... */ };
var redisOptions = new PollyResilienceOptions { /* ... */ };

services.AddPollyResilience(pgOptions, redisOptions);
```

## Testing

The module includes comprehensive tests covering:
- Successful operations (no retries)
- Transient failures with exponential backoff
- Max retries exhaustion
- Timeout enforcement
- Circuit breaker opening after threshold
- Circuit breaker half-open transition
- Concurrent request handling

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~PollyResiliencePipelineTests"
```

## Telemetry

Optional telemetry listener for monitoring:

```csharp
services.AddSingleton<ResilienceTelemetryListener>();
```

Logs:
- Circuit breaker state changes (Opened, Closed, Half-Opened)
- Retry attempts with delay information
- Timeout events

## Best Practices

1. **Use Appropriate Timeouts**: Set realistic timeouts based on operation complexity
2. **Monitor Circuit Breaker State**: Integrate with your monitoring/alerting system
3. **Tune Thresholds**: Adjust failure threshold and break duration based on real-world metrics
4. **Test Failure Scenarios**: Ensure your application handles `BrokenCircuitException` gracefully
5. **Disable Retries for Non-Idempotent Operations**: Set `MaxRetryAttempts = 0` for operations that shouldn't be retried

## Architecture

```
ResiliencePipeline (Polly)
    ↓
1. Timeout Strategy (outermost)
    ↓
2. Retry Strategy (exponential backoff + jitter)
    ↓
3. Circuit Breaker Strategy (innermost)
    ↓
Your Operation
```

Execution order:
1. Timeout starts monitoring
2. If operation fails with transient error, retry handles it
3. Circuit breaker tracks all failures and may block future requests
4. If timeout expires, operation is cancelled

## License

Licensed under the MIT License. See LICENSE file for details.
