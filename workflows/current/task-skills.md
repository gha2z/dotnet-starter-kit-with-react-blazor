# Task → Skill Map — Current Environment (canonical)

> One map, no copies. FSH recipes (`.agents/skills/`) win on **structure**; the dotnet plugin
> suite (global opencode config) informs **technique** — it never overrides an FSH rule.
> Load the skill (`skill` tool / read the SKILL.md) before the task, not during.

## FSH skills (this repo, `.agents/skills/`)

| Task | Skill |
|---|---|
| API endpoint / business op in an existing module | `add-feature` |
| DB entity / table | `add-entity` (+ `create-migration`) |
| Whole module (bounded context) | `add-module` |
| React page (list+create) | `add-react-page` |
| Blazor page (list+detail+create) | `add-blazor-page` |
| Full slice (backend + React) | `add-full-slice` |
| Blazor form / list | `implement-blazor-form` · `implement-blazor-list` |
| EF migration (FSH way) | `create-migration` |
| Cross-module event (Outbox) | `add-integration-event` |
| Endpoint permission end-to-end / C# mirror + policy | `add-permission` · `add-permission-csharp` |
| MAUI native feature | `add-maui-hybrid-feature` |
| Unit tests (xUnit/Shouldly/NSubstitute/AutoFixture) | `testing-guide` |
| Read queries (paged/filter/sort) | `query-patterns` |
| Blazor JWT auth / SignalR / dashboard SSE | `setup-blazor-auth` · `setup-blazor-realtime` · `setup-blazor-sse` |
| Mediator source-gen API reference | `mediator-reference` |

## dotnet plugin suite (opencode global config — supplements, never overrides)

| Technique need | Skill |
|---|---|
| ASP.NET Core minimal API semantics / OpenAPI | `dotnet-webapi` |
| EF Core query optimization (N+1, tracking, compiled) | `optimizing-ef-core-queries` |
| Run/troubleshoot `dotnet test` | `run-tests` · `filter-syntax` |
| Test anti-patterns / assertion quality / coverage | `test-anti-patterns` · `assertion-quality` · `coverage-analysis` |
| Blazor component / JS interop / forms / data fetching | `author-component` · `use-js-interop` · `collect-user-input` · `fetch-and-send-data` |
| MAUI (DI, Shell nav, theming, lifecycle…) | `maui-dependency-injection` · `maui-shell-navigation` · `maui-theming` · `maui-app-lifecycle` |
| MSBuild perf / modernization | `build-perf-baseline` · `build-perf-diagnostics` · `msbuild-antipatterns` · `convert-to-cpm` |
| .NET upgrades / diagnostics / AOT | `migrate-dotnet*-to-dotnet*` · `dotnet-aot-compat` · `dump-collect` · `dotnet-trace-collect` |

**Conflict rule:** when a dotnet-skill pattern would violate an FSH rule (module boundaries,
mediator style, validation, tenant isolation, Golden Rules in AGENTS.md), **FSH wins**. Dotnet
skills may inform *how* (query shape, test commands) but never *what* the structure must be.

## Workflow playbooks (`.agents/workflows/`)

`code-reviewer` · `feature-scaffolder` · `module-creator` · `architecture-guard` · `migration-helper`
— task playbooks for recurring job shapes; read the one matching your task type.

## Pre-flight convention

Before writing code that extends an existing surface (new Blazor page, new endpoint), read one
working sibling in the same project first (API/component conventions) — prevents
build-error-fix loops.