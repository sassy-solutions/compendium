---
description: Write tests for the current branch's changes (or for a named project) using the Compendium test conventions.
allowed-tools: Bash, Read, Edit, Write, Grep, Glob, Agent
argument-hint: [--project <Name>] [--coverage] [--integration]
---

# /tests

Launches a test-author agent that loads the **`compendium-test-author`** skill (`compendium/.claude/skills/compendium-test-author/SKILL.md`) and writes tests for the current branch — or for a named project.

## Behaviour

Inputs `$ARGUMENTS` (parse loosely):
- `--project <Name>` → restrict scope to `src/.../{Name}/` and `tests/Unit/{Name}.Tests/`. Skip git diff.
- `--coverage` → also run `/coverage` (or invoke ReportGenerator) at the end and append the report to the agent's final message.
- `--integration` → additionally write/update tests under `tests/Integration/Compendium.IntegrationTests/` for the same scope.

Default (no flags) = scope to the diff between `HEAD` and `$(git merge-base HEAD main)`.

## What the agent does

1. Resolve scope (project name or `git diff` set of `src/**/*.cs`).
2. Load the `compendium-test-author` skill and follow its **Process** section literally.
3. For each type in scope, add tests covering happy path + every branch + every `Result.Failure` + edge cases + (if multi-tenant) tenant-isolation + (if async with `CancellationToken`) cancellation.
4. Run `dotnet test tests/Unit/{Project}.Tests -c Release` until green.
5. If `--coverage` : run coverlet + ReportGenerator, attach summary.
6. If `--integration` : also generate integration tests using `IAsyncLifetime` + existing fixtures (`PostgreSqlFixture`, `RedisFixture`, `RequiresDockerFactAttribute`).
7. **Never** modify production code. If a real bug surfaces, emit `BUG_FOUND: ...` and stop.
8. End with the coverage report block defined in the skill.

## Constraints

- Conventions are **non-negotiable** — see the skill's "Hard constraints" section.
- Any branch created here is named `feat/tests-{project-slug}` (or `feat/tests-{branch-name}` if scoped to a diff).
- Commit message format: `test({project}): add unit tests covering {area}`.
- No package version bumps. No new top-level dependency. No `--no-verify`.

## Examples

```
/tests
/tests --project Compendium.Application
/tests --project Compendium.Adapters.Redis --coverage
/tests --integration
```

## Implementation note

This command is meant to be invoked as a slash command. The harness expands `$ARGUMENTS`, then the model spawns a subagent (general-purpose) with the skill loaded and the resolved scope.
