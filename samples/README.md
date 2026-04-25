# Compendium samples

Three runnable projects that map to the three milestones of a typical Compendium adoption: define an aggregate, persist its events, then plug in a third-party adapter.

| Sample | What it shows | How to run | Required setup |
| --- | --- | --- | --- |
| [`01-QuickStart-OrderAggregate`](01-QuickStart-OrderAggregate/) | `AggregateRoot`, `IDomainEvent`, in-memory event log + projection, `ICommandDispatcher` / `IQueryDispatcher` wiring | `dotnet run` | None — pure in-memory |
| [`02-MultiTenant-WithPostgres`](02-MultiTenant-WithPostgres/) | `Compendium.Multitenancy` + `Compendium.Adapters.PostgreSQL` — two tenants share an event store; row-level isolation by `tenant_id` | `docker compose up -d` then `dotnet run` | Docker (PostgreSQL 16 on port 5433) |
| [`03-AI-WithOpenRouter`](03-AI-WithOpenRouter/) | `Compendium.Adapters.OpenRouter` implementing `IAIProvider`; a single chat completion with offline fallback | `dotnet run` | Optional: `OPENROUTER_API_KEY` env var (otherwise an offline stub is used) |

## Build them all

```bash
dotnet build samples/ -c Release
```

CI builds every sample on every PR; running them is left to humans because samples 02 and 03 need real services.

## Adding your own sample

1. Create `samples/NN-YourSample/YourSample.csproj` with `<OutputType>Exe</OutputType>`.
2. The repo's [`samples/Directory.Build.props`](Directory.Build.props) already opts samples out of NuGet packing and disables doc generation.
3. Add the project to `Compendium.sln` (`dotnet sln add ...`) and update the table above.
