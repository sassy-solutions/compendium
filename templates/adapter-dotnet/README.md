# `template-compendium-adapter-dotnet`

Starter for a new **Compendium** adapter (.NET 9, single-vendor, lives in its own repository).

Aligns with [ADR 0006](../../docs/adr/0006-multi-repo-adapter-split.md) (split heavy adapters into per-adapter repositories). Encodes the [`compendium-test-author`](.claude/skills/compendium-test-author/SKILL.md) skill so `/tests` and `/coverage` work out of the box.

## What you get

```
.
├── src/Compendium.Adapters.Sample/        — the adapter project (rename Sample → <Vendor>)
│   ├── DependencyInjection/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Options/SampleOptions.cs
│   └── SampleAdapter.cs                   — illustrates the IAdapter (or any port) shape
├── tests/Unit/Compendium.Adapters.Sample.Tests/
│   ├── DependencyInjection/ServiceCollectionExtensionsTests.cs
│   ├── Options/SampleOptionsTests.cs
│   └── GlobalUsings.cs
├── .github/workflows/ci.yml               — build + test + 90% coverage gate
├── .claude/skills/compendium-test-author/SKILL.md
├── .claude/commands/{tests,coverage}.md
├── .config/dotnet-tools.json              — pins ReportGenerator
├── Directory.Build.props
├── Directory.Packages.props               — central package management
├── Compendium.Adapters.Sample.sln
├── global.json                            — pins .NET 9 SDK
└── LICENSE
```

## Conventions enforced (copy from Compendium framework)

| Aspect | Choice |
|---|---|
| Test framework | xUnit 2.9.3 |
| Assertions | FluentAssertions 6.12.1 — never `Assert.*` |
| Mocks | NSubstitute 5.1.0 — never Moq |
| Coverage | coverlet.collector 6.0.2 + ReportGenerator (local tool) |
| Result pattern | `Result<T>` from `Compendium.Abstractions` (NuGet) |
| Async | `async Task` + cancellation tokens — never `Thread.Sleep`, never `.Result` |
| Test naming | `{SUT}Tests` / `{Method}_{Scenario}_{Expected}` |
| Test layout | AAA explicit (`// Arrange / // Act / // Assert`) |
| File header | Sassy Solutions copyright block |
| HTTP mocking (when applicable) | `RichardSzalay.MockHttp` 7.0.0 |
| Container fixtures (integration) | `Testcontainers` 4.11.0 + `IAsyncLifetime` + `[RequiresDockerFact]` |
| CI gate | ≥ 90 % line coverage on the unit-testable surface (DB-bound types may be exempted with documented reason) |

## How to scaffold a new adapter

```bash
# 1. Pick a vendor name (use PascalCase: Stripe, PostgreSQL, Redis…)
export VENDOR=Stripe

# 2. Copy the template to a new directory next to your Compendium clone
cp -r templates/adapter-dotnet ../compendium-adapter-${VENDOR,,}
cd ../compendium-adapter-${VENDOR,,}

# 3. Find-and-replace placeholders (BSD sed on macOS — adapt for GNU sed)
find . -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.sln' -o -name '*.md' -o -name '*.yml' -o -name '*.json' -o -name '*.props' \) -exec sed -i '' -e "s/Sample/${VENDOR}/g" -e "s/sample/${VENDOR,,}/g" {} +

# 4. Rename folders/files
git mv src/Compendium.Adapters.Sample              src/Compendium.Adapters.${VENDOR}
git mv src/Compendium.Adapters.${VENDOR}/Compendium.Adapters.Sample.csproj \
       src/Compendium.Adapters.${VENDOR}/Compendium.Adapters.${VENDOR}.csproj
git mv src/Compendium.Adapters.${VENDOR}/SampleAdapter.cs                   \
       src/Compendium.Adapters.${VENDOR}/${VENDOR}Adapter.cs
git mv src/Compendium.Adapters.${VENDOR}/Options/SampleOptions.cs           \
       src/Compendium.Adapters.${VENDOR}/Options/${VENDOR}Options.cs

git mv tests/Unit/Compendium.Adapters.Sample.Tests           tests/Unit/Compendium.Adapters.${VENDOR}.Tests
git mv tests/Unit/Compendium.Adapters.${VENDOR}.Tests/Compendium.Adapters.Sample.Tests.csproj \
       tests/Unit/Compendium.Adapters.${VENDOR}.Tests/Compendium.Adapters.${VENDOR}.Tests.csproj
git mv tests/Unit/Compendium.Adapters.${VENDOR}.Tests/Options/SampleOptionsTests.cs \
       tests/Unit/Compendium.Adapters.${VENDOR}.Tests/Options/${VENDOR}OptionsTests.cs

mv Compendium.Adapters.Sample.sln Compendium.Adapters.${VENDOR}.sln

# 5. Initialise git and verify build
git init
git add .
dotnet build -c Release
dotnet test  -c Release
```

## What you still need to do per repo

After scaffolding :

- **Author the actual adapter code.** Replace `SampleAdapter` with the real implementation of whatever port (`IEventStore`, `IIdentityProvider`, `IBillingProvider`, `IEmailSender`, …) you're filling.
- **NuGet publishing.** Add `NUGET_API_KEY` to repo secrets ; the included `release.yml` (TODO — add when first needed) packs and pushes on `v*` tags.
- **Branch protection.** Require `build-test` (CI), at least one review, no force-push to `main`.
- **Renovate or Dependabot.** Renovate config at `renovate.json` — track Compendium NuGets so a framework release auto-PRs the adapter. Dependabot for npm-style scheduled dep bumps.
- **Integration tests** (optional but recommended for adapters with external systems). Add `tests/Integration/Compendium.Adapters.<Vendor>.IntegrationTests/` with `Testcontainers` if needed. Keep them out of the unit CI job.

## Local-dev mode (when you're modifying both framework and adapter)

Edit `Directory.Packages.props` to add a project reference instead of the NuGet :

```xml
<ItemGroup Condition="'$(LinkLocalCompendium)' == 'true'">
  <PackageReference Remove="Compendium.Abstractions" />
  <ProjectReference Include="../compendium/src/Abstractions/Compendium.Abstractions/Compendium.Abstractions.csproj" />
</ItemGroup>
```

Then `dotnet build -p:LinkLocalCompendium=true`.

## License

MIT — same as Compendium itself.
