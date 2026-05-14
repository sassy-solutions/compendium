# External adapters

Heavy adapters that ship from their own repositories per [ADR-0006](../adr/0006-multi-repo-adapter-split.md). Each is released independently from the framework on its own version cadence.

| Adapter | Repository | NuGet package | Description |
|---|---|---|---|
| Stripe | [sassy-solutions/compendium-adapter-stripe](https://github.com/sassy-solutions/compendium-adapter-stripe) | [`Compendium.Adapters.Stripe`](https://www.nuget.org/packages/Compendium.Adapters.Stripe) | Billing: subscriptions, checkouts, customer portal, HMAC-signed webhooks |
| LemonSqueezy | [sassy-solutions/compendium-adapter-lemonsqueezy](https://github.com/sassy-solutions/compendium-adapter-lemonsqueezy) | [`Compendium.Adapters.LemonSqueezy`](https://www.nuget.org/packages/Compendium.Adapters.LemonSqueezy) | Billing: subscriptions, checkouts, license keys, webhooks (JSON:API) |
| Zitadel | [sassy-solutions/compendium-adapter-zitadel](https://github.com/sassy-solutions/compendium-adapter-zitadel) | [`Compendium.Adapters.Zitadel`](https://www.nuget.org/packages/Compendium.Adapters.Zitadel) | Identity: users, tokens, organizations (multi-tenant) |
| Listmonk | [sassy-solutions/compendium-adapter-listmonk](https://github.com/sassy-solutions/compendium-adapter-listmonk) | [`Compendium.Adapters.Listmonk`](https://www.nuget.org/packages/Compendium.Adapters.Listmonk) | Email: transactional, newsletters, subscribers |
| OpenRouter | [sassy-solutions/compendium-adapter-openrouter](https://github.com/sassy-solutions/compendium-adapter-openrouter) | [`Compendium.Adapters.OpenRouter`](https://www.nuget.org/packages/Compendium.Adapters.OpenRouter) | AI: access to 100+ LLMs through one OpenAI-compatible API |

## Writing a new adapter

Generate a new repository from the [`template-compendium-adapter-dotnet`](https://github.com/sassy-solutions/template-compendium-adapter-dotnet) GitHub template. The template ships:

- xUnit + FluentAssertions + NSubstitute + AutoFixture + Bogus test stack
- 90 % line-coverage CI gate
- MinVer-driven versioning from git tags
- Tag-triggered NuGet publishing (nuget.org + GitHub Packages)
- Renovate + Dependabot configs
- `compendium-test-author` skill and `/tests` / `/coverage` slash commands

See the template README for the full bootstrap procedure.

## In-framework adapters

These stay in the framework monorepo for now:

- [`Compendium.Adapters.AspNetCore`](aspnetcore.md) — thin glue (middleware, ProblemDetails mappers, DI helpers)
- [`Compendium.Adapters.PostgreSQL`](postgresql.md) — event-store + projection store backed by PostgreSQL
- [`Compendium.Adapters.Redis`](redis.md) — distributed cache + locking strategy

PostgreSQL and Redis are kept in-tree because their tests are coupled to the framework's integration test suite. A future ADR may extract them once that coupling is unwound.
