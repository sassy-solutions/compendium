# Sagas in Compendium

> Two patterns share the name "Saga". Compendium gives each its own type so you don't
> have to guess which one you're using. This document explains the difference, helps
> you pick the right flavor, and shows how to use both.

## TL;DR

| | Process Manager (DDD) | Event Choreography |
|---|---|---|
| Type | `ProcessManager<TState>` / `IProcessManager` | `IHandle<TEvent>` / `IEventChoreography` |
| Coordination | Centralized (the saga drives) | Distributed (services react) |
| State | Owned by the saga, persisted | Lives in aggregates; saga state is implicit |
| Best when | A workflow inside one bounded context | A chain across many bounded contexts |
| Compensation | Explicit step-by-step rollback | Compensation events published by participants |
| Visibility | Single record, one place to look | Spread across logs / tracing |
| Trade-off | Coupling concentrated in one component | Hard to see the full workflow at a glance |

If you're not sure: start with a **Process Manager** when the work is in one service,
and reach for **Event Choreography** when the work crosses services and you don't want
a central component to know about every step.

## Why two flavors?

The original [Sagas paper (Garcia-Molina, 1987)](https://www.cs.cornell.edu/andru/cs711/2002fa/reading/sagas.pdf)
defined sagas as long-lived transactions composed of local transactions with
compensations. Modern usage has split into two distinct patterns that both inherit
the name:

1. **Orchestration / Process Manager** — Vaughn Vernon's preferred term in *DDD
   Distilled*. A central, stateful coordinator drives a workflow across multiple
   aggregates inside one bounded context.

2. **Choreography / Event-driven Saga** — Chris Richardson's
   [microservices.io](https://microservices.io/patterns/data/saga.html). No central
   coordinator: each service reacts to integration events and publishes its own.

Frameworks like MassTransit, NServiceBus and Wolverine all use the bare word "Saga"
and disagree subtly on what it means. Compendium uses two distinct types so you
always know which pattern you're committing to.

## Process Manager (orchestration)

A `ProcessManager<TState>` is a stateful aggregate-coordinator: it receives events,
decides what to do next, and emits commands or further events. The saga's state is
persisted, so it can survive restarts and resume from where it left off.

### When to use it

- The workflow lives inside a single bounded context.
- You want one place to look to see "where is order #123 in the fulfillment process?"
- Steps are sequential and the failure / compensation logic is non-trivial.
- You need timeouts ("if no payment within 24h, cancel the reservation").

### Shape

```csharp
using Compendium.Application.Sagas.ProcessManagers;

public sealed class OrderProcessState
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class OrderProcessManager : ProcessManager<OrderProcessState>
{
    private OrderProcessManager(Guid id, OrderProcessState state)
        : base(id, state, new[] { "ReserveInventory", "ProcessPayment", "ShipOrder" })
    {
    }

    public static OrderProcessManager Create(string orderId, decimal amount) =>
        new(Guid.NewGuid(), new OrderProcessState { OrderId = orderId, Amount = amount });
}
```

### Driving it

```csharp
services.AddProcessManagers();
services.AddPostgreSqlProcessManagerRepository(o => o.ConnectionString = "...");
services.AddSingleton<IProcessManagerStepExecutor, OrderStepExecutor>();

// Somewhere in the application:
var pm = OrderProcessManager.Create("order-123", 250m);
await orchestrator.StartAsync(pm);
await orchestrator.ExecuteStepAsync(pm.Id, "ReserveInventory");
await orchestrator.ExecuteStepAsync(pm.Id, "ProcessPayment");
// On failure:
await orchestrator.CompensateAsync(pm.Id);
```

The `IProcessManagerStepExecutor` is your hook to issue real commands per step
(e.g. send a `ReserveInventoryCommand` via the CQRS dispatcher) and roll them back in
`CompensateAsync`. Compendium does not auto-compensate on step failure — that decision
is yours, so you can choose to retry transient errors first.

## Event Choreography

`IHandle<TEvent>` is for the choreography flavor: every participant reacts to the
events it cares about and publishes the next event in the chain. There is no central
saga record. The "saga" emerges from the chain of events.

### When to use it

- The workflow crosses multiple bounded contexts.
- You want services to remain loosely coupled.
- Eventual consistency is acceptable.
- You don't want one component to know about every step of the workflow.

### Shape

```csharp
using Compendium.Abstractions.Sagas.Choreography;

public sealed class ShipmentChoreography :
    IHandle<PaymentCaptured>,
    IHandle<PaymentRefunded>
{
    public Task<Result> HandleAsync(PaymentCaptured @event, IChoreographyContext ctx, CancellationToken ct = default)
        => ctx.PublishAsync(new PrepareShipmentRequested(@event.OrderId), ct);

    [Compensation(typeof(PaymentCaptured))]
    public Task<Result> HandleAsync(PaymentRefunded @event, IChoreographyContext ctx, CancellationToken ct = default)
        => ctx.PublishCompensationAsync(new ShipmentCancelled(@event.OrderId), ct);
}
```

### Wiring it

```csharp
services.AddEventChoreography(typeof(ShipmentChoreography).Assembly);

// Anywhere — typically the integration-event consumer of your bus / outbox:
await router.DispatchAsync(integrationEvent);
```

`AddEventChoreography` scans the supplied assemblies for `IHandle<TEvent>`
implementations and registers them transiently. The default in-memory publisher
fans events back through the router so the chain runs end-to-end in tests and
samples; in production you replace `IIntegrationEventPublisher` with your outbox
adapter.

The `[Compensation(typeof(...))]` attribute is metadata only — it tags handlers as
compensation steps for telemetry and documentation. It does not change runtime
behavior.

## Decision tree

```
Is the workflow inside ONE bounded context?
├── Yes → Do you need centralized progress tracking and step-by-step compensation?
│         ├── Yes → ProcessManager<TState>
│         └── No  → Plain CommandHandler / domain events suffice — you don't need a saga.
└── No  → Are services loosely coupled and can tolerate eventual consistency?
          ├── Yes → EventChoreography (IHandle<TEvent>)
          └── No  → Reconsider the boundary: a workflow that needs strong consistency
                    across services is a smell. Consider merging the contexts or
                    introducing an integration ProcessManager that owns the cross-context
                    coordination explicitly.
```

## Migrating from `ISaga` (deprecated)

The old `Compendium.Application.Saga.ISaga` and `SagaOrchestrator` types are kept for
one minor version with `[Obsolete]` warnings and will be removed in v1.0. Mapping:

| Old (`Compendium.Application.Saga`) | New |
|---|---|
| `ISaga`, `ISaga<TData>` | `IProcessManager`, `IProcessManager<TState>` |
| `SagaOrchestrator` | `ProcessManagerOrchestrator` |
| `ISagaRepository` | `IProcessManagerRepository` |
| `ISagaStepExecutor` | `IProcessManagerStepExecutor` |
| `ISagaFactory<,>` | (removed — use a static `Create(...)` method on your saga class) |
| `SagaStatus`, `SagaStep`, `SagaStepStatus` | Same names, now in `Compendium.Abstractions.Sagas.Common` |

`EventChoreography` is genuinely new — there was no equivalent in the old API.

## Limits and caveats

- **Timeouts** — not yet built into the orchestrator. If your saga needs "fire after
  24h" behavior, use a separate scheduler (e.g. Quartz, Hangfire) that calls back
  into the orchestrator.
- **Visibility for choreography** — there's no single "saga" record to query. Wire
  up distributed tracing (the `CorrelationId` on every event is propagated for you)
  and consume integration events into a dedicated read model if you need a UI.
- **PostgreSQL adapter** — the schema is created on first use of
  `PostgresProcessManagerRepository.InitializeAsync()`. In production, manage
  migrations explicitly rather than relying on auto-create.

## References

- Vaughn Vernon, *Domain-Driven Design Distilled* — chapter on Process Managers.
- Chris Richardson, [Saga pattern on microservices.io](https://microservices.io/patterns/data/saga.html).
- Hector Garcia-Molina & Kenneth Salem, [Sagas (1987)](https://www.cs.cornell.edu/andru/cs711/2002fa/reading/sagas.pdf).
- [MassTransit Saga State Machines](https://masstransit.io/documentation/patterns/saga).
- [NServiceBus Sagas](https://docs.particular.net/nservicebus/sagas/).
