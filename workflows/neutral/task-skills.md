# Task → Skill Map — Original repo (canonical)

> The original repo's `.agents/` ecosystem is fully documented in its `AGENTS.md` — this page is
> the quick index. Any tool can use these: each skill is a plain `SKILL.md` markdown file — read
> it with your editor/agent before the task, follow its steps. FSH recipes win **structurally**;
> there is no external plugin suite in this package by design (verified: the original AGENTS.md
> already carries rules × skills × workflows on disk).

## Skills (`.agents/skills/*/SKILL.md` — read before the task)

| Task | Skill |
|---|---|
| API endpoint / business op in an existing module | `add-feature` |
| DB entity / table | `add-entity` (+ `create-migration`) |
| Whole module (bounded context) | `add-module` |
| React page (list+create) | `add-react-page` |
| Full slice (backend + React) | `add-full-slice` |
| EF migration | `create-migration` |
| Cross-module event (Outbox) | `add-integration-event` |
| Endpoint permission end-to-end | `add-permission` |
| Unit tests (xUnit/Shouldly/NSubstitute/AutoFixture) | `testing-guide` |
| Read queries (paged/filter/sort) | `query-patterns` |
| Mediator source-gen API reference | `mediator-reference` |

## Rule files (`.agents/rules/*.md` — read the one for your area)

`architecture.md` · `api-conventions.md` · `database.md` · `eventing.md` · `caching.md` ·
`jobs.md` · `resilience.md` · `storage.md` · `security.md` · `realtime.md` · `logging.md` ·
`testing.md` · `integration-testing.md` · `buildingblocks-protection.md` · `modules/*.md` ·
`frontend/shared.md` · `frontend/admin.md` · `frontend/dashboard.md`

## Workflows (`.agents/workflows/*.md`)

`code-reviewer` · `feature-scaffolder` · `module-creator` · `architecture-guard` · `migration-helper`

## Index that beats all of the above

The repo's `AGENTS.md` "Rules index" table is the canonical entry point — if this page and
AGENTS.md disagree, AGENTS.md wins.