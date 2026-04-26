# Compendium

> A pragmatic .NET framework for building event-sourced, multi-tenant SaaS applications.

[![CI](https://github.com/sassy-solutions/compendium/actions/workflows/ci.yml/badge.svg)](https://github.com/sassy-solutions/compendium/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Compendium.Core.svg)](https://www.nuget.org/packages/Compendium.Core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)

Compendium is the framework that powers [Nexus](https://sassy.solutions), Sassy Solutions' multi-tenant platform engineering product. It distills years of building event-sourced SaaS into a small set of focused packages: DDD primitives, CQRS handlers, an event store, multi-tenancy, and ready-to-use adapters for PostgreSQL, Redis, Zitadel, and more.

## Why Compendium?

- **Zero-dependency Core** — Pure DDD primitives (`AggregateRoot<TId>`, `ValueObject`, `Result<T>`, `Error`) with no external dependencies beyond the .NET BCL.
- **CQRS + Event Sourcing built-in** — Command/query dispatchers, event store interfaces, and a PostgreSQL adapter wired out of the box.
- **Sagas, two flavors** — `ProcessManager<TState>` for DDD-style orchestration sagas and `IHandle<TEvent>` for event-driven choreography sagas, each clearly named so you don't have to guess which pattern you're using. See [docs/sagas.md](docs/sagas.md).
- **Multi-tenancy native** — Tenant context, resolution, and scoping baked into the primitives — not bolted on.
- **Result pattern everywhere** — No control-flow exceptions. Every fallible operation returns `Result<T>` with structured `Error` values.
- **Modular adapters** — Pick only what you need: PostgreSQL, Redis, Zitadel, Listmonk, LemonSqueezy, OpenRouter, ASP.NET Core.
- **Battle-tested in production** — Powers Nexus, a multi-tenant platform engineering product.

## Quick start

Install the packages you need:

```bash
dotnet add package Compendium.Core
dotnet add package Compendium.Application
dotnet add package Compendium.Adapters.PostgreSQL
```

Define an event-sourced aggregate:

```csharp
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Results;

public sealed class OrderAggregate : AggregateRoot<OrderId>
{
    private OrderStatus _status;
    private decimal _amount;

    private OrderAggregate(OrderId id) : base(id) { }

    public static Result<OrderAggregate> Create(CustomerId customerId, decimal amount)
    {
        if (amount <= 0)
            return Result.Failure<OrderAggregate>(
                Error.Validation("Order.Amount.Invalid", "Amount must be positive"));

        var order = new OrderAggregate(OrderId.New());
        order.AddDomainEvent(new OrderCreated(order.Id, customerId, amount));
        return Result.Success(order);
    }

    public void Apply(OrderCreated @event)
    {
        _status = OrderStatus.Pending;
        _amount = @event.Amount;
    }
}
```

Wire it up in `Program.cs`:

```csharp
using Compendium.Application.CQRS;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register Compendium CQRS dispatchers (command/query handlers are resolved via IServiceProvider).
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();

// Register your command/query handlers, then wire the PostgreSQL event store adapter
// using the options published by Compendium.Adapters.PostgreSQL.

var app = builder.Build();
```

## Architecture

```
Core (zero deps) → Abstractions → Application → Infrastructure → Adapters
                                       ↓
                              Multitenancy (cross-cutting)
```

- **Core** — Domain primitives with no external dependencies.
- **Abstractions** — Ports (interfaces) for infrastructure concerns: identity, billing, email, AI.
- **Application** — CQRS orchestration: command/query handlers, dispatchers.
- **Infrastructure** — Generic infrastructure concerns: projections, outbox, caching.
- **Adapters** — Concrete integrations with external systems.
- **Multitenancy** — Tenant resolution and scoping, usable across all layers.

## Packages

| Package | Purpose | NuGet |
|---------|---------|-------|
| `Compendium.Core` | DDD primitives, Result pattern, domain events | [![NuGet](https://img.shields.io/nuget/v/Compendium.Core.svg)](https://www.nuget.org/packages/Compendium.Core/) |
| `Compendium.Abstractions` | Shared infrastructure port interfaces | [![NuGet](https://img.shields.io/nuget/v/Compendium.Abstractions.svg)](https://www.nuget.org/packages/Compendium.Abstractions/) |
| `Compendium.Abstractions.AI` | AI provider contracts | [![NuGet](https://img.shields.io/nuget/v/Compendium.Abstractions.AI.svg)](https://www.nuget.org/packages/Compendium.Abstractions.AI/) |
| `Compendium.Abstractions.Billing` | Billing provider contracts | [![NuGet](https://img.shields.io/nuget/v/Compendium.Abstractions.Billing.svg)](https://www.nuget.org/packages/Compendium.Abstractions.Billing/) |
| `Compendium.Abstractions.Email` | Email provider contracts | [![NuGet](https://img.shields.io/nuget/v/Compendium.Abstractions.Email.svg)](https://www.nuget.org/packages/Compendium.Abstractions.Email/) |
| `Compendium.Abstractions.Identity` | Identity provider contracts | [![NuGet](https://img.shields.io/nuget/v/Compendium.Abstractions.Identity.svg)](https://www.nuget.org/packages/Compendium.Abstractions.Identity/) |
| `Compendium.Application` | CQRS dispatchers, handlers, pipelines | [![NuGet](https://img.shields.io/nuget/v/Compendium.Application.svg)](https://www.nuget.org/packages/Compendium.Application/) |
| `Compendium.Infrastructure` | Projections, outbox, infrastructure building blocks | [![NuGet](https://img.shields.io/nuget/v/Compendium.Infrastructure.svg)](https://www.nuget.org/packages/Compendium.Infrastructure/) |
| `Compendium.Multitenancy` | Tenant context, resolution, and scoping | [![NuGet](https://img.shields.io/nuget/v/Compendium.Multitenancy.svg)](https://www.nuget.org/packages/Compendium.Multitenancy/) |
| `Compendium.Testing` | Test helpers, fakes, TestContainers fixtures | [![NuGet](https://img.shields.io/nuget/v/Compendium.Testing.svg)](https://www.nuget.org/packages/Compendium.Testing/) |
| `Compendium.Extensions.ExternalAdapters` | Unified registration for external adapters | [![NuGet](https://img.shields.io/nuget/v/Compendium.Extensions.ExternalAdapters.svg)](https://www.nuget.org/packages/Compendium.Extensions.ExternalAdapters/) |
| `Compendium.Adapters.AspNetCore` | ASP.NET Core integration (middleware, auth, problem details) | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.AspNetCore.svg)](https://www.nuget.org/packages/Compendium.Adapters.AspNetCore/) |
| `Compendium.Adapters.PostgreSQL` | PostgreSQL event store & repositories | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.PostgreSQL.svg)](https://www.nuget.org/packages/Compendium.Adapters.PostgreSQL/) |
| `Compendium.Adapters.Redis` | Redis cache adapter | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.Redis.svg)](https://www.nuget.org/packages/Compendium.Adapters.Redis/) |
| `Compendium.Adapters.Zitadel` | Zitadel OIDC identity adapter | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.Zitadel.svg)](https://www.nuget.org/packages/Compendium.Adapters.Zitadel/) |
| `Compendium.Adapters.Listmonk` | Listmonk email adapter | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.Listmonk.svg)](https://www.nuget.org/packages/Compendium.Adapters.Listmonk/) |
| `Compendium.Adapters.LemonSqueezy` | LemonSqueezy billing adapter | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.LemonSqueezy.svg)](https://www.nuget.org/packages/Compendium.Adapters.LemonSqueezy/) |
| `Compendium.Adapters.OpenRouter` | OpenRouter AI provider adapter | [![NuGet](https://img.shields.io/nuget/v/Compendium.Adapters.OpenRouter.svg)](https://www.nuget.org/packages/Compendium.Adapters.OpenRouter/) |

## Documentation

The full documentation site is being built at [sassy-solutions.github.io/compendium](https://sassy-solutions.github.io/compendium/) (DocFX-powered). In the meantime:

- [ROADMAP.md](ROADMAP.md) — themes, what's next, and what's out of scope
- [CONTRIBUTING.md](CONTRIBUTING.md) — build, test, conventions
- [docs/adr/](docs/adr/) — architecture decision records
- Source under `src/` and the Nexus consumer code for end-to-end examples

## Who's using Compendium?

- **[Nexus](https://sassy.solutions)** — Multi-tenant platform engineering by Sassy Solutions.

Using Compendium in your project? Open a PR to add yourself to this list.

## Contributing

Contributions, issues, and feedback are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on code style, commit conventions, and the development loop.

## License

MIT &copy; 2026 Sassy Solutions. See [LICENSE](LICENSE) for details.
