# 01 — QuickStart: Order Aggregate

A zero-dependency, in-memory walkthrough of the smallest interesting Compendium app: define an aggregate, dispatch a command, project an event into a read model, and query it.

## What it shows

- `AggregateRoot<TId>` with `AddDomainEvent` / `IncrementVersion`
- `IDomainEvent` via `DomainEventBase`
- `Result` / `Result<T>` for success / failure paths
- `ICommand` / `IQuery` + handlers wired through `ICommandDispatcher` / `IQueryDispatcher`
- An in-memory event log and projection updated synchronously from the command handler

No databases, no Docker, no cloud APIs.

## Run it

```bash
dotnet run -c Release
```

Expected output (the `OrderId` and timestamps will differ):

```text
=== Compendium QuickStart: Order aggregate ===

  ✓ Order placed: c5d58d25-d8da-4889-a86c-954426ec6551
  ✓ Projection: OrderSummary { OrderId = c5d58d25-d8da-4889-a86c-954426ec6551, CustomerId = cust-001, TotalAmount = 49.95, Status = Placed }

Domain events captured (1):
    • OrderPlaced [EventId=..., AggregateId=..., AggregateType=Order, Version=1, EventVersion=1, OccurredOn=...]

Done.
```

## Files

- `Program.cs` — the entire sample in one file (events → aggregate → command/handler → query/handler → projection → composition root).
- `QuickStart.OrderAggregate.csproj` — references `Compendium.Core`, `Compendium.Abstractions`, and `Compendium.Application`.

## What to read next

- `samples/02-MultiTenant-WithPostgres/` — same building blocks against a real PostgreSQL event store, scoped per tenant.
- `docs/concepts/event-sourcing.md`
- `docs/getting-started.md`
