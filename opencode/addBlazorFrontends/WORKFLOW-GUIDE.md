# Enhanced Workflow Guide — Task→Skill Map + Two-Mode Orchestration

> This guide maps **every common task** to the right skill/tool, and explains the **two orchestration modes**:
> 1. **Swarm mode** (opencode-swarm plugin) — multi-agent parallel with authority rules + gates
> 2. **Manual multi-session mode** (this protocol) — single agent, live docs, Kanban board, handoff

Read `readme.md` first for protocol overview. This file is the detailed task→tool reference + new-project bootstrap.

---

## 1. Task → Skill Map (FSH-flavored)

| Task | Skill | When to use |
|---|---|---|
| **Add API endpoint / business op** | `add-feature` | Vertical slice in existing module: command/query + handler + validator + endpoint |
| **Add DB entity / table** | `add-entity` | New domain entity with EF config + migration in existing module |
| **Add whole module (bounded context)** | `add-module` | New `Modules.{Name}` + `.Contracts` + `IModule` + DbContext + permissions + migrations |
| **Add React page (list+create)** | `add-react-page` | API module → page → lazy route → (admin) permission gate → Playwright test |
| **Add Blazor page (list+detail+create)** | `add-blazor-page` | Service → page → route → (admin) permission gate → bunit test |
| **Add full slice (backend + React)** | `add-full-slice` | Composes `add-feature` + `add-react-page` |
| **Add Blazor form (create/edit)** | `implement-blazor-form` | MudForm with validation on Blazor WASM page |
| **Add Blazor list (paged/filterable/sortable)** | `implement-blazor-list` | MudTable list page |
| **Create EF migration** | `create-migration` | After changing entities/EF config — central Migrations project |
| **Publish cross-module event** | `add-integration-event` | Outbox + idempotent handler in another module |
| **Add permission end-to-end** | `add-permission` | Server constant + endpoint gate + (admin) catalog + route guard |
| **Mirror permission to C# + policy** | `add-permission-csharp` | Backend adds endpoint permission → C# constant + policy |
| **Add MAUI native feature** | `add-maui-hybrid-feature` | Push, biometric, camera, offline queue, deep linking |
| **Write tests** | `testing-guide` | xUnit + Shouldly + NSubstitute + AutoFixture, AAA naming |
| **Implement read queries** | `query-patterns` | Paginated lists, search/filter/sort, single-entity fetches (DbContext LINQ) |
| **Setup Blazor JWT auth** | `setup-blazor-auth` | ITokenStore (localStorage) + AuthStateProvider + DelegatingHandler + Permissions |
| **Setup Blazor SignalR** | `setup-blazor-realtime` | HubConnection, notifications, chat |
| **Setup Dashboard SSE** | `setup-blazor-sse` | Server-Sent Events for dashboard-blazor only |

---

## 2. Two Orchestration Modes

### A. Swarm Mode (opencode-swarm plugin)

**Requirements**: `.opencode/opencode-swarm.json` with `parallelization_enabled: true` + plugins installed.

```bash
# Phase flow
/swarm epic 1              # Decide phase 1 (promote/demote based on coupling)
/swarm wave 1              # Dispatch wave 1 (parallel, one coder per disjoint task)
/swarm wave 2              # ... subsequent waves
/swarm review 1            # reviewer + test_engineer gate (or full 5-member council)
/swarm complete 1          # Phase complete: drift verify + evidence write
```

**Agent roster (lean default)**: architect, coder, explorer, reviewer, test_engineer  
**Disabled by default**: sme, critic, docs, designer — enable in config if needed.

**Authority rules** (in `.opencode/opencode-swarm.json`):
- coder: write-scoped to `src/Modules/**`, `clients/**`, `opencode/**`, `.agents/**`, `deploy/**`
- coder: DENY `src/BuildingBlocks/**`, `src/Host/**` (architect-only)

**Gates** (ordered):
1. Build + typecheck (`dotnet build` / `dotnet test --no-build`)
2. Lint (`dotnet format --verify-no-changes` + project linters)
3. SAST (Semgrep + built-in)
4. Mutation test (80% kill rate — opt-in via QA profile)
5. Drift verify (`critic_drift_verifier` agent)
6. Review council (`reviewer` + `test_engineer` minimum)

---

### B. Manual Multi-Session Mode (this protocol)

**No plugin required**. Single opencode session, state in `live/` + `board.md` + `00-Index.md`.

| Step | Command / Action | Artifact updated |
|---|---|---|
| **Start** | `opencode` → read `00-Index.md`, `live/README.md`, `STATUS.md` | Context loaded |
| **Plan** | Decompose → add tasks to `board.md` (Backlog) + `00-Index.md` | Plan visible |
| **Execute** | Work one task → update `board.md` (Doing→Review→Done) → append to `live/sess-*.md` | Trace captured |
| **Sync** | `git commit -m "feat: ..."` (conventional) | History immutable |
| **Handoff** | Write `STATUS.md` (one line) + `board.md` next-task pointers | Zero-loss transfer |

**Session log template** (`live/_template.md`):
```markdown
## Session N — YYYY-MM-DD — <title>

### Context
- Branch: `feature/xyz`
- Base: `main` @ <sha>
- Goal: <one sentence>

### Work
- [ ] Task 1.1 — <desc> — <status>
- [ ] Task 1.2 — <desc> — <status>

### Decisions
- <decision> — <rationale>

### Blockers / Follow-ups
- <blocker> → <action>

### Next session
- Pick up: <task id>
```
```

**Wave DAG + Critical Path** (every new build phase `plan.md` opens with a wave block):

```markdown
## Wave DAG
Nodes = task IDs · edges = `depends` · waves = topological partitions (serial by convention)
mermaid:
  flowchart LR
    t1.1 --> t1.3 --> t1.5
    t1.2 --> t1.3
    t1.4 --> t1.6
Waves: W1 [t1.1,t1.2,t1.4] → W2 [t1.3] → W3 [t1.5,t1.6]
Critical path: t1.1 → t1.3 → t1.5   (longest dependency chain — gate here first)
```

- "Parallel-safe" requires **disjoint owned files** per the `coordination.ps1` overlap check — never
  inferred from the DAG alone.
- The critical path is where gate risk concentrates: if a wave on it slips, downstream waves absorb
  the delay. Check the critical-path task (`-Gate` + per-unit verify) before parallel satellites.

---

## 3. Plugin Roster + Token Policy

### `.opencode/opencode.json` — installed plugins

| Plugin | Version | Purpose |
|---|---|---|
| `opencode-autotitle` | 0.1.3 | Auto session titles from first user message |
| `opencode-notify` | 0.3.1 | Desktop notifications on completion / input wait |
| `envsitter-guard` | 0.0.4 | Blocks secret leaks in `.env` / config |
| `@gotgenes/opencode-agent-identity` | 3.1.1 | Persistent agent identity across sessions |
| `opencode-context-analysis-plugin` | local | `/context` command — blast radius, dependency graph |

### Model allocation

| Tier | Models | Agents | Max tokens/session |
|---|---|---|---|
| **Reasoning** | nemotron-3-ultra (this session) | architect, critic | 200k |
| **Standard** | nemotron-3-ultra | coder, reviewer, test_engineer, explorer | 128k |
| **Lite** | gemini-flash / gpt-4o-mini | docs, summarization, grep | 64k |

Configure in `.opencode/opencode-swarm.json` → `agents.<name>.model`.

---

## 4. QA Gates (Enforced)

| Gate | Tool | Trigger | Failure = |
|---|---|---|---|
| Build + typecheck | `dotnet build` / `dotnet test --no-build` | Pre-commit / pre-PR | Block commit |
| Lint | `dotnet format --verify-no-changes` + linters | Pre-commit | Block commit |
| SAST | `sast_scan` (Semgrep + built-in) | Phase complete | Block phase |
| Mutation test | `mutation_test` (80% kill) | Phase complete (opt-in) | Warn / Block |
| Drift verify | `critic_drift_verifier` agent | Phase complete | Block phase |
| Review council | `reviewer` + `test_engineer` (min) | Phase complete | Block phase |
| **Summary close-out** | `coordination.ps1 -CloseOut` (manual mode) | Wave/phase complete | Block commit |

---

## 5. New SaaS Project (From This Repo — no standalone)

There is **no standalone workflow repository** — it was retired by user decision. To seed a new
SaaS, use `opencode/init-saas-workflow.ps1`, which clones **this** repo as its source:

```powershell
pwsh opencode/init-saas-workflow.ps1 -Name AcmeSaaS -WorkDir C:\dev\AcmeSaaS -FromClone C:\repos\dotnet-starter-kit-with-react-blazor-main
```

What it does (each step prints + can be skipped with a switch):

1. **Clone/copy** the source repo into `-WorkDir` (fresh `.git`, no history baggage).
2. **Rename ritual** — replacement map over file names, `.csproj`, `.slnx`, namespaces, appsettings,
   `config.json`, CSS/JS, test names: `FSH.Starter`→`AcmeSaaS.Starter`, `FSH.`→`AcmeSaaS.` (project
   namespaces), `FSH`→`AcmeSaaS` (remaining identifiers), `FullStackHero`→`AcmeSaaS` (brand). Then a
   **verification sweep**: grep residual references + full build + `Architecture.Tests` → prints a
   residual report for manual review.
3. **Strip** (guided prompts) — `-StripReact`, `-StripMaui`, `-StripModules Catalog,Billing,…`.
4. **Clean** — remove project-private dirs (`opencode/addBlazorFrontends/`, `.swarm/`, …).
5. **Wire workflow** — `.opencode/opencode-swarm.json`, `.opencode/skill-routing.yaml`, `.gitignore`
   entries, slim `AGENTS.md`.
6. **Restore + build gate** — fails loudly with the fix list if the rename broke something.
7. **First commit + session bootstrap** — clean-clone commit, `opencode/live/` skeleton + `STATUS.md`.
8. **Print next steps** — the exact commands to open the first session.

Full detail lives in `opencode/AGENTIC-GUIDE.md` §4 (template + clone paths). The `{{PLACEHOLDERS}}`
parameterization is replaced by the rename ritual above; the protocol core
(`AGENTIC-GUIDE.md`, `_tracks-template/`, this workflow) is copied by hand from this repo when needed.

---

## 6. File Inventory (Protocol Core)

| File | Purpose |
|---|---|
| `readme.md` | Protocol overview + quick start |
| `WORKFLOW-GUIDE.md` | **This file** — task→skill, modes, tokens, gates, new-project bootstrap |
| `coordination.ps1` | Zone map + session commands (board, index, live, status, handoff) |
| `verify.ps1` | Build + test + smoke publish (FSH defaults) |
| `verify-hybrid.ps1` | MAUI Hybrid + PWA verification |
| `live/_template.md` | Session log template |
| `live/README.md` | Session file index |
| `live/board.md` | Kanban board template |
| `live/sess-main.md` | Main session log |
| `00-Index.md` | Task registry + phase summary |
| `00_summary/_template.md` | Wave summary template (incl. `## Lessons / Process Improvements`) |
| `STATUS.md` | One-line handoff state |
| `99-Glossary.md` | Project terms |
| `00-Setup.md` | Environment setup checklist |
| `AGENTIC-GUIDE.md` | Standalone user guide |

---

*Version: see git tag / `00-Setup.md`. Source: this repo (workflow is repo-local — no standalone).*