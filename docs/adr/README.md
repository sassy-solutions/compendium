# Architecture Decision Records

ADRs document the structural choices of Compendium. Format: [MADR](https://adr.github.io/madr/).

| # | Title | Status | Date |
|---|---|---|---|
| 0001 | [Result pattern over exceptions](0001-result-pattern.md) | Accepted | 2026-Q2 |
| 0002 | [Hexagonal architecture (strict)](0002-hexagonal-architecture.md) | Accepted | 2026-Q2 |
| 0003 | [Zero-dependency Core](0003-zero-dep-core.md) | Accepted | 2026-Q2 |
| 0004 | [Multi-tenancy strategy](0004-multi-tenancy-strategy.md) | Accepted | 2026-Q2 |
| 0005 | [Event sourcing over state-stored](0005-event-sourcing-vs-state.md) | Accepted | 2026-Q2 |
| 0006 | [Multi-repo adapter split](0006-multi-repo-adapter-split.md) | Accepted | 2026-05-14 |
| 0007 | [Integration test split (InMemory default)](0007-integration-test-split.md) | Accepted | 2026-05-14 |

## Process
- Propose new ADR via PR with status `Proposed`
- Discuss in PR review
- On merge → status `Accepted` (or `Rejected`)
- Superseding an ADR = new ADR + update old's status to `Superseded by ####`

## Template
See [0000-template.md](0000-template.md).
