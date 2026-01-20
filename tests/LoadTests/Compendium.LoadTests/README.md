# Compendium Load Testing - PostgreSQL Connection Pooling

NBomber-based load testing for PostgreSQL connection pooling optimization.

## Prerequisites

- .NET 9.0 SDK
- Docker (optional, for TestContainers)
- External PostgreSQL database (optional)

## Running Load Tests

### Option 1: Using External Database (Recommended for Development)

```bash
# Set environment variable with connection string
export EVENTSTORE_CONNECTION_STRING="Host=51.159.205.157;Database=season_events_development;Username=season-pascal;Password=T#j#4}{r5X.]}uPx>*DI;Port=17711;Timeout=30;Command Timeout=30"

# Run load tests
dotnet run --project tests/LoadTests/Compendium.LoadTests -c Release
```

### Option 2: Using TestContainers (Requires Docker)

```bash
# Ensure Docker is running
docker ps

# Run load tests (will automatically start PostgreSQL container)
dotnet run --project tests/LoadTests/Compendium.LoadTests -c Release
```

## Test Scenarios

### 1. Append Events (Write Load)
- **Duration**: 90 seconds
- **Load Pattern**:
  - 30s @ 50 req/sec injection
  - 60s @ 100 concurrent connections
- **Operation**: Append 10 events per request

### 2. Read Events (Read Load)
- **Duration**: 60 seconds
- **Load Pattern**: 100 concurrent connections
- **Operation**: Read all events for random aggregates

### 3. Mixed Operations (70% Read / 30% Write)
- **Duration**: 60 seconds
- **Load Pattern**: 100 concurrent connections
- **Operation**:
  - 70% reads from existing aggregates
  - 30% writes (5 events per request)

### 4. Burst Load (Stress Test)
- **Duration**: 30 seconds
- **Load Pattern**: 200 req/sec injection
- **Operation**: Append 20 events per request

### 5. 1000 Concurrent Connections (Main Test)
- **Duration**: 120 seconds total
- **Load Pattern**:
  - 10s ramp: 0 → 100 connections
  - 20s ramp: 100 → 500 connections
  - 30s ramp: 500 → 1000 connections
  - 60s sustain: 1000 connections
- **Operation Mix**:
  - 50% reads from existing aggregates
  - 30% writes (10 events per request)
  - 20% existence checks

## Current Configuration Under Test

```csharp
MaxPoolSize = 20           // Application-level semaphore
CommandTimeout = 30        // seconds
TableName = "event_store_loadtest"
AutoCreateSchema = true
BatchSize = 1000
```

**Npgsql Connection String** (defaults):
- Pooling = true
- Minimum Pool Size = 0
- Maximum Pool Size = 100
- Connection Idle Lifetime = 300s
- Timeout = 15s

## Results

Results are saved to `load-test-results/` folder:
- `compendium-connection-pooling-report.html` - Interactive HTML report with charts
- `compendium-connection-pooling-report.txt` - Text summary

## Key Metrics to Monitor

1. **Request Throughput**: Requests/sec achieved
2. **Response Time**: Mean, median, p95, p99 latencies
3. **Error Rate**: Failed requests / Total requests
4. **Connection Pool Utilization**: From application semaphore
5. **Database Connections**: Monitor PostgreSQL connection count

## Expected Results (Current Config)

With MaxPoolSize=20, expect:
- ✅ Scenarios 1-3: Should complete successfully
- ⚠️ Scenario 4 (Burst): May show delays due to connection pool bottleneck
- ❌ Scenario 5 (1000 concurrent): Will fail or show high latency (20 < 1000)

## Troubleshooting

### Container fails to start
```bash
# Check Docker is running
docker ps

# Check disk space
df -h
```

### Connection timeouts
- Verify external database is accessible
- Check firewall rules
- Verify connection string credentials

### Out of memory errors
- Reduce concurrent connection counts in scenarios
- Increase Docker memory limits
- Use external database instead of TestContainers

## Next Steps

After baseline results:
1. Increase `MaxPoolSize` to 100-200 (Task 10.3)
2. Add Npgsql pooling parameters to connection string (Task 10.3)
3. Re-run tests and compare metrics
4. Optimize PostgreSqlEventStore if needed (Task 10.4)
5. Add connection pooling metrics (Task 10.5)

## References

- [NBomber Documentation](https://nbomber.com)
- [Npgsql Connection Pooling](https://www.npgsql.org/doc/connection-string-parameters.html)
- [PostgreSQL Connection Limits](https://www.postgresql.org/docs/current/runtime-config-connection.html)
- [Connection Pooling Analysis](../../../docs/performance/npgsql-connection-pooling.md)
