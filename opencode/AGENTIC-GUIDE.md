# Agentic Workflow Guide — FullStackHero .NET Starter Kit

**Last updated: 2026-08-12, by: opencode (model: auto/coding — unresolved).**

The single source of truth for driving agentic coding on this repo **and** for bootstrapping
future SaaS projects from it. Read this before starting any session. The **session ritual,
coordination rules, and close-out are canonical in `workflows/current/session-protocol.md`**
(self-starting, tool-agnostic); this repo's live track keeps its own protocol details in
`opencode/addBlazorFrontends/readme.md`.

**Requirements live in `docs/spec/`** — one file per app/stream, authored by the human. Plans trace
to `FR-###` ids from those files. Never invent requirements in a session; read the spec.

---

## 0. Tracks — how this workflow scales

A **track** is a work-stream with its own owner, disjoint zone, roadmap (`00-Index.md`), status
(`STATUS.md`), live session state, and spec (`docs/spec/NNN-<name>.md`). The active track today is
`opencode/addBlazorFrontends/` (React→Blazor→MAUI parity to zero gaps). Future tracks (erp, pos,
consumer, operator) start from `opencode/_tracks-template/`.

> **New track = new owner + disjoint zone + (spec | index | status | live).** If any of those isn't
> real, it's a **phase**, not a track.

One session = one feature (or one planning pass), never a whole phase. The human-facing driving
loop is described in the session protocol (`workflows/current/session-protocol.md` §1–§3) —
this file's successor to the retired `HUMAN-GUIDE.md`.

---

## 1. Mental model — two operating modes

| Mode | When | Mechanism |
|---|---|---|
| **Swarm (single-session, gated)** | One cohesive chunk of code work (a page, a feature, a fix) that can be planned → coded → reviewed → tested in ONE session | opencode-swarm: architect plans, critic gates the plan, coder implements, reviewer + test_engineer gate the result. Config in `.opencode/opencode-swarm.json`. |
| **Manual multi-session (parallel lanes)** | Work that needs device/emulator verification (MAUI), long-running E2E, or two disjoint zones in parallel | `sess-main` + `sess-maui` lanes coordinated via `opencode/addBlazorFrontends/live/*.md` + `coordination.ps1` + `verify.ps1` |

**Decision rule:** if the work is fully verifiable by build + bUnit tests, use **Swarm**. If it
requires a running emulator, a device, a browser with a live backend, or touches two sessions'
zones at once, use the **manual protocol** (or run Swarm in the lane that owns the zone).

Both modes share the same ground rules: never commit without user approval, never `git push`,
structured logging, docs travel with the change (Golden Rule 10).

---

## 2. Before any session — prerequisites

```powershell
# Environment check (once per machine)
dotnet --version                 # 10.x required
node --version                   # 20+ for React apps
opencode --version
git status                       # clean or expected in-progress work

# Workflow check (once per repo)
Test-Path .opencode/opencode-swarm.json     # Swarm config present
Test-Path .agents/skills/                   # FSH skill catalog present
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Start   # manual-mode gate
```

- **Session identity:** the user names each session (`sess-main`, `sess-maui`, …). If unnamed, ask.
- **Model identity:** never guess/copy. Read the raw-message JSON for `modelID`/`providerID`.
  `auto/*` combos resolve through the omniroute gateway logs — if unverifiable, write
  `model: <modelID> (unresolved)`. See `readme.md` → "Model Identity Convention".
- **AI memory & knowledge tools:** this stack keeps cross-session state in three places — (1)
  magic-context project memory: `ctx_memory` (durable facts), `ctx_search` (history/git recall) —
  (2) swarm knowledge base: `knowledge_recall` / `knowledge_query` (semantic + filtered lookup),
  `knowledge_add` (new lessons), stored locally in `.swarm/knowledge.jsonl` (gitignored, never
  committed) — (3) session notes: `ctx_note`. Before re-asking the user something that may already
  be known, search these. They are agent-side state; docs and specs remain the human-side truth.
- **Read-fresh ceremony:** re-read `STATUS.md`, all `live/*.md`, and the phase plan **fresh**
  every session — never from memory. State moves fast in this repo.

### 2.1 Self-starting session ritual — run this automatically on every task

**When you receive any task prompt, do the following BEFORE starting work. No human reminder needed.**

> **Canonical protocol: `workflows/current/session-protocol.md`** — read it + the track's
> `STATUS.md` + `live/*.md` fresh, then run `pwsh workflows/current/coordination.ps1 -Session <sid> -Start`.
> For this repo's active track (`opencode/addBlazorFrontends/`), use that track's
> `coordination.ps1` with the same flags.

Key rules baked into the ritual (enforced by the canonical protocol + `coordination.ps1`):
- Verification numbers in docs ALWAYS come from a real run THIS SESSION — never from memory.
- Summary file in `00_summary/` is the **single tracking artifact** per wave (carries Lessons, file diff, verification).
- `STATUS.md` is an **append-only one-line ledger** — never edit existing rows; append one line per wave.
- `board.md` rows are for **cross-session coordination only** — single-session work does not add rows.
- Whole-file reads only — no offset-tunneling (causes pattern-escalation stops).
- Heartbeat every user turn; one lesson line per turn; close-out is fail-closed (`-CloseOut`).
- Stage by explicit paths only — never `git add -A`; never `git push`.

---

## 3. Driving the current front-end projects (Blazor WASM + MAUI)

### 3.1 Where things stand (refresh STATUS.md — this is a snapshot)

| Area | State |
|---|---|
| Admin Blazor (5175) | Suite green (164/164 bUnit), 0 warnings |
| Dashboard Blazor (5176) | Suite green (233/233 bUnit), 0 warnings |
| MAUI Hybrid | 5.1–5.9 in-zone delivered (`bc00bea5`), hybrid 12/12; blocked/external: push (Firebase/APNs 5.4), IAP (5.8), signing/CI (5.10) |
| Lazy loading + PWA | Done both apps (Pages RCLs + `resources.lazyAssembly`, manifest + SW + offline page) |
| ✅ Release-publish | **RESOLVED (wave 19, 2026-08-09)** — the Mono boot crash was a stale-publish artifact; clean publish (delete `obj/Release` first) boots to login ~4.3 s, 0 errors, no code change. Deployment-zone CDN config (`.br`/`.gz` + 103 Early Hints) remains guidance only. |

### 3.2 Remaining work items — session recipes

| # | Item | Mode | Session recipe |
|---|---|---|---|
| 6.x | PWA HTTPS-staging check, preload hints, infinite scroll | Swarm (dashboard zone = sess-main) | Load `add-blazor-page` + `query-patterns` skills; architect → coder → reviewer/test_engineer; verify with `verify.ps1`; bUnit test per change |
| 6.7 | Root README + migration guide | Manual (shared zone) | Announce on `live/board.md` first; docs-only session |
| 7.x | Parity re-audit against React (`clients/admin`, `clients/dashboard` — READ-ONLY reference) | Swarm + explore subagent | Audit convention: subagent writes full findings to a temp file, returns severity-ranked list |
| 5.4/5.8/5.10 | Push, IAP, signing/CI | Manual (sess-maui) | External deps (Firebase, Apple, signing certs). Work in `clients/FSH.Hybrid/` only; use `verify-hybrid.ps1` |

**Every page/feature recipe (Swarm):**

1. **Scope it** — one cohesive chunk: a page + its service + route + permission + test. Never "Phase 5".
2. **Load the skill** — `add-blazor-page` (new screen), `implement-blazor-list` / `implement-blazor-form`
   (page building blocks), `add-permission-csharp` (new endpoint permission), `add-feature` (backend slice).
3. **Pre-flight** — read an existing working page in the same project first (API conventions, component
   availability). Confirmed by `readme.md` "Pre-Flight Convention".
4. **Dispatch** — architect plans → critic gates → coder implements (in-zone only) → reviewer →
   test_engineer runs bUnit.
5. **Verify** — `pwsh opencode/addBlazorFrontends/verify.ps1` (clean obj/bin → 0-warning builds → both
   suites → icon audit). Take the verify lock first: `coordination.ps1 -LockVerify` / `-UnlockVerify`.
6. **DoD** — service + DI exist (no invented DTOs), bUnit asserts specific behavior, 0 warnings,
   manual steps flagged if not testable, docs use freshly-verified model identity. **Stage by explicit
   path** (`git add <paths>`, never `-A`), show `git diff --cached --stat`, wait for approval.

### 3.3 Manual multi-session protocol (sess-main / sess-maui)

- **Zones** (`coordination.ps1` fails closed — keep in sync with readme "Scope Restriction"):
  - `sess-main`: `clients/dashboard-blazor/**`, `clients/BlazorShared/**`, `clients/admin-blazor/**`
    (app code by default), `STATUS.md`, `00-Index.md`, `opencode/addBlazorFrontends/**` (per-file),
    Phase-02/03/04/06/07 docs
  - `sess-maui`: `clients/FSH.Hybrid/**`, `verify-hybrid.ps1`, Phase-05 docs, `maui-hybrid.md`
  - `shared`: `.agents/rules/frontend/**`, root `README.md` (announce on board.md first)
- **Never touch:** `clients/admin` + `clients/dashboard` (React parity source), `AGENTS.md`,
  `CLAUDE.md`, `GEMINI.md`, `.github/**`, `deploy/**`, `src/**`, other `.agents/**` rules/skills,
  `src/BuildingBlocks` (protected — approval required).
- **Cadence:** `coordination.ps1 -Heartbeat` every turn · `-Gate` before staging · lock before verify.
- **Locks:** stale `live/locks/verify.lock` (owner heartbeat > 12h) → announce on board.md, remove.

### 3.4 The verification gates (non-negotiable)

```powershell
pwsh opencode/addBlazorFrontends/verify.ps1           # both WASM apps: clean→build→tests→icon audit
pwsh opencode/addBlazorFrontends/verify-hybrid.ps1    # MAUI: 4 TFMs build, 0 warnings
dotnet build src/FSH.Starter.slnx                     # full solution at phase end
```

Expected baselines (**live numbers in `opencode/addBlazorFrontends/STATUS.md` — refresh there, not
here**): dashboard bUnit 233/233 · admin bUnit 164/164 · hybrid 12/12 ·
backend build 0 warnings · Architecture.Tests 51/51 · 17 role-permission integration tests.

---

## 4. Building a NEW SaaS from this codebase

### 4.1 The two paths

| Path | Command | Result | Use when |
|---|---|---|---|
| **Clone** (supported/recommended) | `git clone <this repo> <dir>` → `pwsh opencode/init-saas-workflow.ps1 -Name MySaaS` | Full fork incl. current front-end state, workflow wired, first track seeded | Any new SaaS — the one supported bootstrap |
| **Template** | `fsh new MySaaS` (workflow layer included by default via the `workflow` symbol) | Renamed, workflow-included project | Brand-new SaaS — fastest scaffold; see §4.2 |

### 4.2 Template path (`dotnet new fsh` / `fsh new`)

This repo is a `dotnet new` template (`shortName: fsh`) with an `fsh` CLI (Spectre.Console) wrapping
it. The template's `sourceName: FSH.Starter` drives renames (solution, namespaces, folders) with
derived symbols (kebab/underscore/display forms) — the safest rename machinery that exists.

**Workflow layer:** the template ships a `workflow` symbol (default `true`) — generated projects
carry `.agents/` (skills + rules), `AGENTS.md`, `.opencode/` (swarm config + skill routing), and the
`opencode/` workflow scripts (`AGENTIC-GUIDE.md`, `init-saas-workflow.ps1`). Project-private dirs
(`opencode/addBlazorFrontends/`, `opencode/Next apps`, `opencode/other`, `opencode/temp`) are
excluded from template output. Opt out with `--workflow false` (`dotnet new fsh -n MySaaS --workflow false`).

```powershell
dotnet tool install --global FullStackHero.CLI    # once
fsh new MySaaS -o ./MySaaS                        # scaffold + rename + workflow layer (default)
pwsh opencode/init-saas-workflow.ps1 -Name MySaaS -WorkDir ./MySaaS -SkipClone   # post-scaffold wiring
```

The pack lives at `.template.config/` + `templates/` in this repo and is guarded by the
`template-smoke.yml` CI workflow — keep it green when touching either path.

### 4.3 Clone path — `init-saas-workflow.ps1` (one command, minimal ceremony)

```powershell
pwsh opencode/init-saas-workflow.ps1 -Name AcmeSaaS -WorkDir C:\dev\AcmeSaaS -FromClone C:\repos\dotnet-starter-kit-with-react-blazor-main
```

What it does (each step prints + can be skipped with a switch):

1. **Clone/copy** the source repo into `-WorkDir` (fresh `.git`, no history baggage; excludes
   `.git`, `bin/obj`, `node_modules`, `.swarm`).
2. **Rename ritual** — replacement map over file names, `.csproj`, `.slnx`, namespaces,
   appsettings, `config.json`, CSS/JS, test names: `FSH.Starter`→`AcmeSaaS.Starter`,
   `FSH.`→`AcmeSaaS.` (project namespaces), `FSH`→`AcmeSaaS` (remaining identifiers),
   `FullStackHero`→`AcmeSaaS` (brand). Then a **verification sweep** (`2b`): grep residual references
   + full build + `Architecture.Tests` → prints a residual report for manual review.
3. **Strip** (guided prompts) — `-StripReact` (drop `clients/admin`, `clients/dashboard`),
   `-StripMaui`, `-StripModules Catalog,Billing,…`. A module strip is thorough: deletes the
   module + its Contracts, migration folder, `*.Tests` project, slnx entries, all `ProjectReference`
   lines pointing at it (incl. Api/DbMigrator/Migrations/Architecture.Tests csprojs), cross-module
   consumers (`IntegrationEventHandlers\` subscribers + module-scoped test files are deleted; the
   shared `DemoSeed\DemoSeeder.cs` keeps with its `// ─── <m> ───` region + `SeedTenant<m>*` call
   stripped; mixed files like `ContractsPurityTests` get using/`typeof`/marker lines stripped),
   XML-doc crefs to its types, the four registration sites (`typeof(...)`/`using` lines in
   Api + DbMigrator `Program.cs`), and dangling `<Folder Include>` items. Default: keep everything.
4. **Clean** — remove project-private dirs (`opencode/addBlazorFrontends/`, `opencode/Next apps`,
   `opencode/other`, `opencode/temp`, `.swarm/`, `superpowers/` origin planning docs,
   `templates/` dotnet-new pack project), phase docs, `live/` session state. Kept:
   `init-saas-workflow.ps1`, `AGENTIC-GUIDE.md`, `.opencode/`, `.agents/`.
5. **Wire workflow** — ensure `.opencode/opencode-swarm.json`, `.opencode/skill-routing.yaml`,
   `.gitignore` entries (`.swarm/`, `opencode/temp/`, `opencode/live/`). `AGENTS.md` + `.agents/`
   are **carried over as-is** from the source (no slimming) — the rename pass already rewrote
   references.
5b. **Seed the first track** — copies `opencode/_tracks-template/` → `opencode/<Name>/` with
   `*-template.md` files renamed to their real names (`00-Index.md`, `STATUS.md`, `readme.md`,
   `zones.md`, `plan.md`), plus `live/` skeleton (`board.md`, `locks/`, session stubs) and a
   starter `STATUS.md` — so the first agentic session starts clean. No phase docs yet.
6. **Restore + build gate** — `dotnet restore`, backend build 0-warning (0-error is a hard fail;
   warnings printed for manual review), optional `npm install` when React kept. Fails loudly with
   the fix list if the rename broke something.
7. **First commit + next steps** — `git add -A` (clean clone — safe), initial commit, then prints
   the exact commands to open the first session (see §4.4).

### 4.4 First sessions on the new SaaS

1. `cd <WorkDir>` · read `AGENTS.md` + `opencode/AGENTIC-GUIDE.md` (this file ships in the template).
2. Start one Swarm session: *"Scaffold the domain module for <YourDomain> using the add-module
   skill; wire the four registration sites."* — the skill encodes the module ritual
   (`Modules.{Name}` + `.Contracts`, `IModule`, DbContext, permissions, migrations folder, tests).
3. Add features with `add-feature` (command/query + handler + validator + endpoint) and
   `create-migration` after entity changes.
4. Add front-end screens with `add-blazor-page` (or React with `add-react-page`) — the Blazor
   apps already exist in the template and are wired to the API.

---

## 5. Skill catalog quick reference

> **Canonical task→skill map: `workflows/current/task-skills.md`** (FSH recipes vs dotnet-skills
> plugin suite, with the conflict rule). The table below is the historical quick view — add new
> skills only to the canonical map.

| Skill | Use for | Source |
|---|---|---|
| `add-module` | New bounded context (runtime + Contracts + registration) | `.agents/skills/` |
| `add-feature` / `query-patterns` | Vertical-slice backend feature / paged queries | `.agents/skills/` |
| `add-entity` + `create-migration` | New EF entity + the FSH-way migration | `.agents/skills/` |
| `add-permission` / `add-permission-csharp` | New endpoint permission (React/Blazor mirrors) | `.agents/skills/` |
| `add-blazor-page` / `implement-blazor-list` / `implement-blazor-form` | Blazor screens | `.agents/skills/` |
| `setup-blazor-auth` / `setup-blazor-realtime` / `setup-blazor-sse` | Blazor wiring | `.agents/skills/` |
| `add-react-page` / `add-full-slice` / `add-integration-event` | React screens / full API+UI slice / cross-module events | `.agents/skills/` |
| `testing-guide` / `mediator-reference` | FSH test conventions / Mediator source-gen API | `.agents/skills/` |
| dotnet-skills (`optimizing-ef-core-queries`, `run-tests`, …) | Technique-level guidance — **never override FSH structure rules** | global `~/.config/opencode/dotnet-skills/` |

**Conflict rule (FSH wins on structure, dotnet-skill informs technique):** when a dotnet-skill
pattern would violate an FSH rule (module boundaries, mediator style, validation, tenant isolation,
Golden Rules), FSH wins. dotnet-skills may inform *how* (query shape, test commands) but never
*what* the structure must be.

---

## 6. Script reference — what to execute when

| You want to… | Run |
|---|---|
| Check the workflow is wired | `Test-Path .opencode/opencode-swarm.json; Test-Path .agents/skills` |
| Start a manual-mode session | `pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Start` |
| Stamp heartbeat (every turn) | `pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Heartbeat` |
| Pre-stage gate | `pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Gate` |
| Close out a wave (summary + Lessons + STATUS refresh; blocks commit) | `pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -CloseOut` |
| Verify both WASM apps | `pwsh opencode/addBlazorFrontends/verify.ps1` |
| Verify MAUI | `pwsh opencode/addBlazorFrontends/verify-hybrid.ps1` |
| Scaffold a new SaaS | `pwsh opencode/init-saas-workflow.ps1 -Name <Name>` (clone — recommended) or `fsh new <Name>` (template, workflow included) |
| Build everything | `dotnet build src/FSH.Starter.slnx` |
| Run the stack | `dotnet run --project src/Host/FSH.Starter.AppHost` |

---

## 7. FAQ / troubleshooting

- **"Cannot provide a value for property 'XService'"** — service not registered in the WASM app's
  `Program.cs` `AddScoped` block. Add it (pattern: the 9 services fixed in Phase 7).
- **Route param crash (`InvalidCastException` on `Id`)** — page binds `[Parameter] string Id` but
  route uses `{Id:guid}`. Drop the `:guid` constraint; add a Router-level regression test.
- **Full-page reload 404s CSS/JS** — `<base href="/" />` must be the FIRST element in `<head>`
  (`IndexHtmlGuardTests` enforces it).
- **Razor source-gen cache stale** — verify.ps1 deletes obj/bin before building (that's the point).
- **verify.lock exists** — someone is verifying; wait or check heartbeat (>12h stale → board.md + remove).
- **Release publish crashed Mono at boot** — RESOLVED (wave 19): stale-publish artifact; delete
  `obj/Release`, re-publish, boots fine (~4.3 s to login). If it regresses, re-open upstream
  dotnet/runtime #121849.
- **Swarm session drifts out of zone** — the coder's scope is declared per task; if it touched
  files outside, unstage by explicit path and re-route through the zone owner.
- **Model identity wrong in a doc** — re-verify fresh (never copy from an older doc); fix header only.

---

## 8. Docs & changelog

- User-facing changes (feature, endpoint, config, infra, breaking change) require the separate
  docs repo (`github.com/fullstackhero/docs`) update + changelog entry (Golden Rule 10).
- Requirements are authored **only** in `docs/spec/`; plans trace `FR-###` ids 1:1 from there.
- Keep this file, `STATUS.md`, `00-Index.md`, phase plans, and `.agents/rules/frontend/*` in sync
  with the code — docs travel with the change.
- Workflow-authoring edits (this file, `_tracks-template/`, `docs/spec/`,
  `workflows/current/`) are sess-main zone, additive, user-approved — see readme.md "Scope Restriction".
