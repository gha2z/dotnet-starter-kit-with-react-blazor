# Enhanced Development Workflow — Task→Skill Map (FSH + dotnet plugin suite)

**Last updated: 2026-08-09 18:55, by: opencode (model: deepseek-v4-flash-free).**
Applies to: FSH Blazor WASM + MAUI frontends AND incoming SaaS apps (React + .NET). This is the
executable guide the session ritual points to (readme step 8). Canonical sources: `AGENTS.md`
(project map + golden rules) + `.agents/rules/**` (conventions) — this guide only resolves
WHICH skill to load WHEN.

## 1. Skill sources (all loaded)

| Source | Location | What |
|--------|----------|------|
| FSH recipes | `.agents/skills/*/SKILL.md` (repo) | FSH-owned workflows: `add-feature`, `add-entity`, `add-module`, `add-full-slice`, `add-react-page`, `add-blazor-page`, `create-migration`, `add-integration-event`, `add-permission`, `add-permission-csharp`, `setup-blazor-auth`, `setup-blazor-realtime`, `setup-blazor-sse`, `implement-blazor-form`, `implement-blazor-list`, `mediator-reference`, `query-patterns`, `testing-guide`, `add-maui-hybrid-feature` |
| Plugin suite | `~/.config/opencode/dotnet-skills/plugins/*/skills` (global config) | Whole plugins: dotnet-msbuild, dotnet-diag, dotnet-maui. Curated: dotnet (`setup-local-sdk`), dotnet-aspnetcore (`dotnet-webapi`, `configuring-opentelemetry-dotnet`, `minimal-api-file-upload`), dotnet-blazor (`author-component`, `collect-user-input`, `coordinate-components`, `fetch-and-send-data`, `plan-ui-change`, `support-prerendering`, `use-js-interop`), dotnet-data (`optimizing-ef-core-queries`), dotnet-test (`run-tests`, `test-anti-patterns`, `assertion-quality`, `test-gap-analysis`, `test-smell-detection`, `coverage-analysis`, `crap-score`, `grade-tests`, `find-untested-sources`, `detect-static-dependencies`, `generate-testability-wrappers`, `migrate-static-to-wrapper`, `test-tagging`, `filter-syntax`, `platform-detection`, `test-analysis-extensions`), dotnet-nuget (`convert-to-cpm`), dotnet-upgrade (`migrate-dotnet8-to-dotnet9`, `migrate-dotnet9-to-dotnet10`, `migrate-dotnet10-to-dotnet11`, `migrate-nullable-references`, `thread-abort-migration`, `dotnet-aot-compat`), dotnet-template-engine (`template-authoring`, `template-comparison`, `template-discovery`, `template-instantiation`, `template-smart-defaults`, `template-validation`) |

**Rules:** FSH recipe wins on overlap (FSH owns its conventions). Plugin skills fill gaps.
Intentional exclusions in global config: `configure-auth`/`create-blazor-project` (FSH owns),
`create-datadriven-aspnetcore` (competes with `add-feature`), `code-testing-agent`/
`writing-mstest-tests`/`mtp-hot-reload` (FSH is xUnit — use `testing-guide`).

## 2. Task→Skill map

| Task | Load FIRST (FSH) | Supplement (plugin) |
|------|------------------|---------------------|
| New Blazor page | `add-blazor-page` | `plan-ui-change` → `author-component` |
| Blazor form | `implement-blazor-form` | `collect-user-input` |
| Blazor list | `implement-blazor-list` | `fetch-and-send-data` |
| JS interop | — | `use-js-interop` |
| Prerender / shared state | — | `support-prerendering`, `coordinate-components` |
| Blazor auth / realtime / SSE | `setup-blazor-auth` / `setup-blazor-realtime` / `setup-blazor-sse` | — |
| MAUI feature | `add-maui-hybrid-feature` | dotnet-maui skills (`maui-shell-navigation`, `maui-data-binding`, `maui-dependency-injection`, `maui-theming`, `maui-app-lifecycle`, `maui-collectionview`, `maui-safe-area`), `dotnet-maui-doctor` |
| Backend feature / endpoint | `add-feature`, `query-patterns`, `mediator-reference` | `dotnet-webapi` |
| Entity + migration | `add-entity` → `create-migration` | `optimizing-ef-core-queries` |
| Module | `add-module` | — |
| Cross-module event | `add-integration-event` | — |
| Permission | `add-permission` / `add-permission-csharp` | — |
| Write tests | `testing-guide` | `run-tests` (exact command + filter syntax) |
| Review tests | — | `test-anti-patterns`, `assertion-quality`, `test-gap-analysis`, `test-smell-detection`, `grade-tests` |
| Coverage | — | `coverage-analysis`, `crap-score`, `find-untested-sources` |
| Testability refactor | — | `detect-static-dependencies`, `generate-testability-wrappers`, `migrate-static-to-wrapper` |
| Slow build | — | `build-perf-baseline` → `build-perf-diagnostics` (`binlog-generation` / `binlog-failure-analysis`) |
| Crash / dump triage | — | `dotnet-trace-collect`, `dump-collect`, `android-tombstone-symbolication`, `apple-crash-symbolication` |
| Perf audit | — | `analyzing-dotnet-performance`, `microbenchmarking` |
| SDK / workload issues | — | `setup-local-sdk`, `dotnet-maui-doctor` |
| CPM (new SaaS) | — | `convert-to-cpm` |
| .NET version upgrade | — | `migrate-dotnet10-to-dotnet11` (etc.), `dotnet-aot-compat` |
| Scaffold new SaaS repo | — | `template-instantiation`, `template-validation` |

## 3. Session ritual v2 (supersedes readme step 8)

1. `git status` → confirm clean/expected state
2. Session id from user; model identity verified fresh (never from memory — readme convention)
3. Read STATUS.md + all `live/*.md` (fresh) + latest `00_summary/`
4. Read the relevant `.agents/rules/frontend/*.md`
5. Load task skills per the map above (FSH recipe first, plugin supplements)
6. Heartbeat + coordination gate before any staging/build
7. Run `verify.ps1` (lock first) / `verify-hybrid.ps1` per cadence
8. Close-out: implementation summary + board rows + commit approval cycle

## 4. QA gates (quality minimums)

- **Backend:** `dotnet build src/FSH.Starter.slnx` 0 warnings (TreatWarningsAsErrors) →
  targeted suite → `architecture-guard`; use `run-tests` for exact commands
- **Blazor:** build target 0 warnings → bUnit suite → `verify.ps1` (clean obj/bin) at handoff
- **Review gate:** `code-reviewer` workflow + audit skills (`test-anti-patterns`,
  `assertion-quality`) before commit; `coverage-analysis` for coverage claims
- **Handoff:** paste verification block from verify.ps1 output

## 5. SaaS bootstrap recipe (incoming apps)

1. Scaffold repo (`dotnet new` template / `template-instantiation`)
2. Wire `Directory.Packages.props` via `convert-to-cpm` (Central Package Management)
3. Copy this repo's `.agents/**` structure (rules + skills) + AGENTS.md conventions
4. Copy the global opencode.jsonc skills block (Section 1) + plugin install commands
5. Set up coordination docs (STATUS.md, board, session files) if multi-session
6. OpenCode swarm (`opencode-swarm.json`, user-level "mega") is OPTIONAL/experimental —
   subagents bypass the coordination gate/lock protocol; use in isolated worktrees only.
   The repo's `.opencode/opencode-swarm.json` stays `{}`.

## 6. Notes / known limits

- Plugin skills are machine-level (global config) — not committed; document in repo setup docs
- No FSH↔plugin skill-name collisions exist (verified 2026-08-09)
- opencode config merges: global skills paths apply in every repo; project configs may add, not remove
