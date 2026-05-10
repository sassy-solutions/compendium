---
description: Run dotnet test with coverage collection and produce a per-project coverage report (line + branch).
allowed-tools: Bash, Read
argument-hint: [--project <Name>] [--no-html]
---

# /coverage

Run unit tests of `Compendium.sln` with **XPlat Code Coverage**, fuse the cobertura outputs via **ReportGenerator**, and print a per-project summary.

## Behaviour

Default :
1. `dotnet tool restore` (uses the local manifest at `.config/dotnet-tools.json` — installs ReportGenerator if missing).
2. ```bash
   dotnet test Compendium.sln -c Release \
     --collect:"XPlat Code Coverage" \
     --filter "FullyQualifiedName!~IntegrationTests&FullyQualifiedName!~LoadTests" \
     --results-directory artifacts/coverage/
   ```
3. ```bash
   dotnet tool run reportgenerator \
     -reports:'artifacts/coverage/**/coverage.cobertura.xml' \
     -targetdir:artifacts/coverage/report \
     -reporttypes:'Html;TextSummary;MarkdownSummary'
   ```
4. Print `artifacts/coverage/report/Summary.txt` content (or `Summary.md` if friendlier).

Args :
- `--project <Name>` → only run that test project: `dotnet test tests/Unit/{Name}.Tests`.
- `--no-html` → skip HTML target, generate only text summary (faster, headless).

## Output

```
=== Coverage by project (line / branch) ===
Compendium.Core                      92.4% / 88.1%
Compendium.Multitenancy              97.0% / 91.5%
Compendium.Application                0.0% /  0.0%   <-- gap
...
```

Plus path of the HTML report : `artifacts/coverage/report/index.html`.

## Notes

- The CI already collects coverage on PRs (no threshold yet — gate will be added in Phase E of the campaign).
- Integration / load tests are excluded from the unit-coverage run — use the integration test workflow for those.
- The `artifacts/coverage/` directory is `.gitignore`d (or should be — add it if not).
