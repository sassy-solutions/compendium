# End-to-End Test Scenarios

## Overview

This document defines comprehensive end-to-end test scenarios that validate the complete Compendium Framework integration, from command handling through event sourcing, projection rebuilds, queries, idempotency, and multi-tenancy.

## Test Scenarios

### Scenario 1: Complete Order Lifecycle (Happy Path)

**Description**: Test the full lifecycle of an order from creation through completion, including event sourcing, projections, and queries.

**Components Tested**:
- CQRS (Command/Query dispatchers)
- PostgreSQL Event Store
- Projection Manager
- Idempotency Service
- Redis Idempotency Store

**Test Flow**:
1. **Create Order** (`PlaceOrderCommand`)
   - Dispatch command through CommandDispatcher
   - Verify idempotency key is stored
   - Aggregate creates `OrderPlaced` event
   - Event appended to PostgreSQL with version 1

2. **Add Order Lines** (`AddOrderLineCommand` x3)
   - Dispatch 3 commands to add order lines
   - Verify optimistic concurrency (expected version 1, 2, 3)
   - Events: `OrderLineAdded` x3

3. **Complete Order** (`CompleteOrderCommand`)
   - Dispatch completion command
   - Events: `OrderCompleted`
   - Final aggregate version: 5

4. **Rebuild OrderSummary Projection**
   - Initialize `OrderSummaryProjection`
   - Rebuild from event stream (5 events)
   - Verify projection state:
     - OrderId
     - Status = "Completed"
     - TotalLines = 3
     - CreatedAt timestamp
   - Verify checkpoint saved at version 5

5. **Query Order Summary**
   - Dispatch `GetOrderSummaryQuery`
   - Verify response matches projection state
   - Verify query performance < 100ms

**Expected Results**:
- ✅ All 5 events appended successfully
- ✅ Projection rebuilt in < 1 second
- ✅ Query returns correct order summary
- ✅ Idempotency keys stored for all commands
- ✅ No concurrency conflicts

---

### Scenario 2: Idempotency - Duplicate Command Handling

**Description**: Verify that duplicate commands are detected and prevented by the idempotency service.

**Components Tested**:
- Idempotency Service
- Redis Idempotency Store
- Command Dispatcher

**Test Flow**:
1. **First Command Execution**
   - Dispatch `PlaceOrderCommand` with idempotency key `order-123-creation`
   - Command executes successfully
   - Idempotency key stored in Redis with result

2. **Duplicate Command (< 24 hours)**
   - Dispatch same command with same idempotency key
   - IdempotencyService detects duplicate
   - Returns cached result WITHOUT re-executing command
   - Verify NO new events appended

3. **Different Command, Same Key**
   - Dispatch `AddOrderLineCommand` with key `order-123-creation`
   - Should fail with idempotency conflict error

4. **Same Command, Different Key**
   - Dispatch `PlaceOrderCommand` with key `order-456-creation`
   - Should execute successfully (different key)

**Expected Results**:
- ✅ Duplicate command returns cached result
- ✅ NO duplicate events in event store
- ✅ Idempotency key conflicts detected
- ✅ Different keys execute independently

---

### Scenario 3: Multi-Tenancy Isolation

**Description**: Verify complete data isolation between tenants at all layers.

**Components Tested**:
- Multi-tenancy context
- PostgreSQL Event Store (tenant_id filtering)
- Projection Manager (tenant isolation)
- Query handlers (tenant filtering)

**Test Flow**:
1. **Tenant A - Create Order**
   - Set tenant context: `tenant-a`
   - Dispatch `PlaceOrderCommand` (order-a-001)
   - Event stored with `tenant_id = 'tenant-a'`

2. **Tenant B - Create Order**
   - Set tenant context: `tenant-b`
   - Dispatch `PlaceOrderCommand` (order-b-001)
   - Event stored with `tenant_id = 'tenant-b'`

3. **Tenant A - Query Orders**
   - Set tenant context: `tenant-a`
   - Query all orders
   - Verify ONLY `order-a-001` returned

4. **Tenant B - Query Orders**
   - Set tenant context: `tenant-b`
   - Query all orders
   - Verify ONLY `order-b-001` returned

5. **Tenant A - Rebuild Projection**
   - Rebuild OrderSummary for tenant A
   - Verify projection contains ONLY tenant A events

6. **Cross-Tenant Query Attempt**
   - Set tenant context: `tenant-a`
   - Attempt to query `order-b-001` directly
   - Should return NotFound or AccessDenied

**Expected Results**:
- ✅ Events stored with correct tenant_id
- ✅ Queries return ONLY tenant-specific data
- ✅ Projections isolated by tenant
- ✅ Cross-tenant access prevented

---

### Scenario 4: High Volume Event Processing (Performance)

**Description**: Validate system performance under high event volume (1000+ events).

**Components Tested**:
- Event Store write performance
- Projection rebuild performance
- Live projection processing

**Test Flow**:
1. **Batch Event Append**
   - Create aggregate with 1000 events
   - Append all events in single transaction
   - Measure append time

2. **Projection Rebuild**
   - Rebuild projection from 1000 events
   - Track progress reports
   - Measure rebuild time

3. **Verify Performance Targets**
   - Event append: < 5 seconds for 1000 events
   - Projection rebuild: > 10,000 events/minute
   - Memory usage: < 100MB for 10k events

**Expected Results**:
- ✅ Append 1000 events in < 5 seconds
- ✅ Rebuild rate > 10,000 events/minute
- ✅ Memory usage < 100MB
- ✅ Progress reporting accurate

---

### Scenario 5: Concurrent Commands with Optimistic Concurrency

**Description**: Verify optimistic concurrency control prevents lost updates under concurrent load.

**Components Tested**:
- Event Store optimistic concurrency
- Command handlers
- Aggregate version management

**Test Flow**:
1. **Create Initial Aggregate**
   - Create order with version 1

2. **Concurrent Commands (10 parallel)**
   - Dispatch 10 `AddOrderLineCommand` concurrently
   - All commands expect current version 1
   - Only ONE should succeed

3. **Sequential Retry**
   - Failed commands retry with correct expected version
   - All commands eventually succeed

4. **Verify Final State**
   - Aggregate version = 11 (1 create + 10 lines)
   - All 10 order lines present
   - NO lost updates

**Expected Results**:
- ✅ Only 1 of 10 concurrent commands succeeds initially
- ✅ 9 commands fail with ConcurrencyConflict error
- ✅ Retry mechanism succeeds for all
- ✅ Final aggregate state correct

---

### Scenario 6: Projection Checkpoint Resume

**Description**: Verify projection rebuilds can resume from checkpoints after interruption.

**Components Tested**:
- Projection Manager
- PostgreSQL Projection Store
- Checkpoint persistence

**Test Flow**:
1. **Start Rebuild**
   - Rebuild projection with 1000 events
   - Save checkpoint at event 500

2. **Simulate Interruption**
   - Stop projection rebuild at event 500
   - Verify checkpoint saved in database

3. **Resume Rebuild**
   - Restart projection rebuild
   - Should resume from event 501
   - Verify NO re-processing of events 1-500

4. **Complete Rebuild**
   - Process remaining 500 events
   - Final checkpoint at event 1000

**Expected Results**:
- ✅ Checkpoint saved at event 500
- ✅ Resume starts from event 501
- ✅ No duplicate event processing
- ✅ Final projection state correct

---

### Scenario 7: Error Handling and Recovery

**Description**: Verify system handles errors gracefully and provides useful feedback.

**Components Tested**:
- Command validation
- Event deserialization
- Projection error handling
- Result pattern

**Test Flow**:
1. **Invalid Command**
   - Dispatch command with invalid data
   - Verify validation error returned
   - Verify NO event appended

2. **Event Deserialization Failure**
   - Store event with corrupted data
   - Attempt to rebuild projection
   - Verify error captured and reported

3. **Projection Error**
   - Event triggers business rule violation in projection
   - Verify projection status = Failed
   - Verify error message logged

4. **Idempotency Error**
   - Redis unavailable
   - Command should still execute (graceful degradation)
   - Warning logged

**Expected Results**:
- ✅ Validation errors returned as Result.Failure
- ✅ Deserialization errors logged with details
- ✅ Projection failures captured in state
- ✅ System continues operating despite non-critical failures

---

### Scenario 8: Live Projection Processing

**Description**: Verify projections update in real-time as new events are appended.

**Components Tested**:
- Live Projection Processor
- Projection Manager
- Event Store streaming

**Test Flow**:
1. **Start Live Processor**
   - Register `OrderSummaryProjection`
   - Start live processing

2. **Append Events**
   - Create order (event 1)
   - Add 3 order lines (events 2-4)
   - Complete order (event 5)

3. **Verify Real-Time Updates**
   - Query projection after each event
   - Verify state updates immediately
   - Max latency < 500ms per event

4. **Stop Processor**
   - Gracefully shutdown processor
   - Verify final checkpoint saved

**Expected Results**:
- ✅ Projection updates within 500ms of event append
- ✅ All 5 events processed
- ✅ Final state matches expectations
- ✅ Checkpoint saved on shutdown

---

### Scenario 9: Saga Orchestration (Multi-Step Workflow)

**Description**: Test long-running saga that coordinates multiple aggregates.

**Components Tested**:
- Saga Orchestrator
- Saga State persistence
- Command compensation
- Integration events

**Test Flow**:
1. **Start OrderFulfillment Saga**
   - Trigger: `OrderCompleted` event
   - Saga state: Created

2. **Step 1: Reserve Inventory**
   - Saga sends `ReserveInventoryCommand`
   - Success: Inventory reserved
   - Saga state: InventoryReserved

3. **Step 2: Process Payment**
   - Saga sends `ProcessPaymentCommand`
   - Success: Payment processed
   - Saga state: PaymentProcessed

4. **Step 3: Ship Order**
   - Saga sends `ShipOrderCommand`
   - Success: Order shipped
   - Saga state: Completed

5. **Verify Final State**
   - All 3 steps completed
   - Integration events published
   - Saga state persisted

**Expected Results**:
- ✅ Saga completes all 3 steps
- ✅ Saga state persisted at each step
- ✅ Integration events published
- ✅ No orphaned state on failure

---

### Scenario 10: Full Stack Integration (Docker Compose)

**Description**: Run complete E2E test against full infrastructure stack.

**Components Tested**:
- PostgreSQL (Event Store + Projections)
- Redis (Idempotency)
- API (ASP.NET Core)
- All framework components

**Test Flow**:
1. **Start Infrastructure**
   - docker-compose up
   - PostgreSQL ready
   - Redis ready
   - API healthy

2. **Execute E2E Workflow**
   - POST /api/orders (create order)
   - POST /api/orders/{id}/lines (add lines)
   - POST /api/orders/{id}/complete (complete)
   - GET /api/orders/{id}/summary (query)

3. **Verify Health Checks**
   - /health/ready returns 200
   - PostgreSQL health = Healthy
   - Redis health = Healthy

4. **Teardown**
   - docker-compose down
   - Verify clean shutdown

**Expected Results**:
- ✅ All containers start successfully
- ✅ Health checks pass
- ✅ API requests succeed
- ✅ Data persisted correctly
- ✅ Clean shutdown

---

## Test Data

### Sample Aggregates

**Order Aggregate**:
- OrderId: Guid
- CustomerId: string
- Status: OrderStatus enum
- OrderLines: List<OrderLine>
- CreatedAt: DateTimeOffset
- CompletedAt: DateTimeOffset?

**Events**:
- `OrderPlaced(OrderId, CustomerId, CreatedAt)`
- `OrderLineAdded(OrderId, LineId, ProductId, Quantity, Price)`
- `OrderCompleted(OrderId, CompletedAt)`

**Projections**:
- `OrderSummaryProjection`: Order summary for queries
- `OrderStatisticsProjection`: Aggregated statistics

### Test Configuration

**Event Volume Targets**:
- Small: 10 events
- Medium: 100 events
- Large: 1,000 events
- Performance: 10,000 events

**Timeouts**:
- Command execution: 5 seconds
- Projection rebuild: 60 seconds for 1000 events
- Query execution: 100ms
- Live processing latency: 500ms

**Performance Targets**:
- Event append: 1000+ events/second
- Projection rebuild: 10,000+ events/minute
- Query response: < 100ms
- Memory usage: < 100MB for 10k events

## Implementation Notes

### Test Organization

```
tests/Integration/EndToEnd/
├── Scenarios/
│   ├── OrderLifecycleE2ETests.cs          # Scenario 1
│   ├── IdempotencyE2ETests.cs             # Scenario 2
│   ├── MultiTenancyE2ETests.cs            # Scenario 3
│   ├── PerformanceE2ETests.cs             # Scenario 4
│   ├── ConcurrencyE2ETests.cs             # Scenario 5
│   ├── ProjectionCheckpointE2ETests.cs    # Scenario 6
│   ├── ErrorHandlingE2ETests.cs           # Scenario 7
│   ├── LiveProjectionE2ETests.cs          # Scenario 8
│   ├── SagaOrchestrationE2ETests.cs       # Scenario 9
│   └── DockerComposeE2ETests.cs           # Scenario 10
├── Fixtures/
│   ├── E2ETestFixture.cs                  # Shared test infrastructure
│   └── DockerComposeFixture.cs            # Docker compose setup
├── TestAggregates/
│   ├── OrderAggregate.cs                  # Test aggregate
│   └── OrderEvents.cs                     # Domain events
├── TestProjections/
│   ├── OrderSummaryProjection.cs          # Query projection
│   └── OrderStatisticsProjection.cs       # Statistics projection
└── TestCommands/
    ├── PlaceOrderCommand.cs
    ├── AddOrderLineCommand.cs
    └── CompleteOrderCommand.cs
```

### CI/CD Integration

**Pipeline Steps**:
1. Build solution
2. Start docker-compose (PostgreSQL + Redis)
3. Wait for health checks
4. Run E2E tests
5. Collect test results and logs
6. Teardown infrastructure

**Test Categories**:
- `[Trait("Category", "E2E")]` - All E2E tests
- `[Trait("Category", "Performance")]` - Performance tests
- `[Trait("Category", "Docker")]` - Requires docker-compose

## Success Criteria

All scenarios must:
- ✅ Execute successfully in CI/CD pipeline
- ✅ Meet performance targets
- ✅ Verify data integrity
- ✅ Validate error handling
- ✅ Confirm multi-tenant isolation
- ✅ Demonstrate idempotency
- ✅ Complete within timeout limits

## Next Steps

1. Implement test aggregates and events
2. Create test fixtures with infrastructure setup
3. Implement each scenario as xUnit test class
4. Configure docker-compose for test infrastructure
5. Integrate tests into CI/CD pipeline
6. Document test results and metrics
