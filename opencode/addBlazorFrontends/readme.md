# AddBlazorFrontends — Session Instructions
** Last updated: 2026-Aug-05, by: Project Owner a.k.a user: FIKRUL IRSYAD.**

## Commit Policy

**NEVER commit until the user explicitly approves.**

After completing a feature:
1. `git add -A`
2. Show: `git diff --cached --stat` + one-line summary of what changed
3. Wait for user approval
4. Then commit

---

## Session Start Ritual

On session start:
1. `git status` — confirm the tree is clean or matches the expected in-progress work
2. Determine your actual model identity (see "Model Identity Convention" below) and record it
   for today's docs
3. Read `./opencode/addBlazorFrontends/STATUS.md` — current state, next task
4. Read the latest `./opencode/addBlazorFrontends/00_summary/implementation-summary-*.md` — what was last done
5. Read `.agents/rules/frontend/blazor-shared.md` — Blazor conventions (plus `blazor-admin.md`,
   `blazor-dashboard.md`, or `maui-hybrid.md` for the target app)
6. Load any relevant skills from `.agents/skills/` (e.g. `add-blazor-page`, `add-feature`,
   `setup-blazor-auth`, `setup-blazor-realtime`, `implement-blazor-list`, `implement-blazor-form`,
   `add-permission`) before you start
7. Verify builds + tests pass before starting work

---

## Parallel Streams & Worktrees

This repo runs **one Main stream plus parallel streams** when independent work can proceed
simultaneously (MAUI Hybrid, Playwright E2E). The Main stream owns the repo root on `develop`;
each parallel stream gets its own branch + worktree, executed by a subagent.

| Stream | Branch | Location | Who |
|--------|--------|----------|-----|
| Main | `develop` | repo root | main session |
| Parallel N | `feature/<name>` | `.worktrees/<name>` | subagent |

Create a stream:

```bash
git worktree add -b feature/<name> .worktrees/<name> develop
```

- `.worktrees/` is gitignored — never commit it
- Register every stream as a row in `STATUS.md` → "Active Streams" (branch · worktree · scope ·
  owner · state); remove the row once merged
- **`BlazorShared` is owned by the Main stream only** — parallel streams treat it read-only and
  route requested changes through Main
- Subagents never commit; each stream's work is committed by the user after approval (Commit Policy)

## Subagent Dispatch Protocol

Dispatch a subagent into a worktree only for work confined to **one independent project**.
**Dispatch when:** `clients/FSH.Hybrid`, standalone test projects (E2E), read-only audits.
**Never dispatch:** `BlazorShared` edits, both apps' `Program.cs`, shared csproj/solution files,
`.gitignore`, root build files — Main-stream work only.

Dispatch prompt template (fill in `<…>`; **one sub-feature per dispatch** — 5.1, not "Phase 5"):

```
You are a parallel-stream subagent working in git worktree <path> on branch <branch>.
Goal: <one sub-feature>
Read first: <STATUS.md stream row · phase plan · .agents/rules/... file · skill>
Scope:
- MAY modify: <files inside this project only>
- READ-ONLY: clients/BlazorShared, clients/admin, clients/dashboard, src/**, AGENTS.md,
  .agents/**, .gitignore, root build files, anything outside your project
- Do NOT touch other clients' projects or shared solution files
Build & test: only your own project + its tests — NEVER the solution (slnx). Work inside your own
worktree: `dotnet build <your.csproj>` (add `-f <tfm>` when applicable), `dotnet test <your.Tests>`.
Report contract (final message only):
- Files changed: <git status --short output>
- Verification: <N errors / N warnings / N tests passed / N skipped>
- Blockers / notes: <anything the Main stream must know>
No git commands, no commits, no touching other streams' files.
```

## Merge & Integration Protocol

- Merge leaf streams → `develop` **after** Main's commits land; smallest diff first
- Each merge needs user approval (show `git diff --cached --stat` per stream)
- Post-merge gate: run full `verify.ps1`; if `BlazorShared` was touched, build **all three**
  consumers (admin-blazor + dashboard-blazor + FSH.Hybrid)
- Shared-file conflicts are resolved by the Main stream

---

## Model Identity Convention

**Never guess or copy a model identity from memory or older docs.** Identity drift happened before —
a session "fixed" the docs to a wrong model name without verification, and it propagated for two
sessions. Determine it fresh every session.

**`auto/*` is a combo name, NOT a model.** A combo routes through a gateway (e.g. `omniroute`) to an
actual model (e.g. `big-pickle`). Writing the combo as the model is wrong.

### How to determine your actual model identity

1. **Inspect the opencode raw-message JSON** (system prompt / context window / raw messages) for the
   `modelID` + `providerID` of your current session.
2. **If `modelID` is a concrete model** (not `auto`/`auto/*`, e.g. `mimo-v2.5-free` via provider
   `opencode`) → use it directly. **No gateway log query needed.**
3. **If `modelID` is an `auto` combo** (e.g. `auto/coding`, provider `omniroute`) → resolve the real
   routed model through the omniroute log:
   - Open `http://localhost:20128/login`, enter the password (see dev machine secrets)
   - Open `http://localhost:20128/api/usage/call-logs?status=ok&limit=1`
   - Read the `model` field of the most recent `ok` entry (e.g. `big-pickle`)

### Identity format in docs

Use the resolved model everywhere identity appears:

- Headers: `Last Update: <yyyy-MM-dd HH:mm:ss>, by: opencode (auto/coding, model: <routed-model>).`
- Summaries: `**Creator:** opencode (auto/coding, model: <routed-model>)`

Use the actual session timestamp (24-hour clock), not a value copied from a prior file.

---

## Pre-Flight Convention

Before writing any `.razor` file, read an existing working page in the same project to confirm API conventions (parameter names, component availability, attribute syntax). This prevents iterative build-error-fix cycles.

---

## Definition of Done (every feature)

- [ ] Service method + DI registration exist — no invented DTOs/endpoints
- [ ] bUnit test asserts the **specific behavior** (gated content, redirect, data shown), not just
      "renders without error"
- [ ] Target project builds 0 warnings; that app's suite is green
- [ ] If behavior is not bUnit-testable (menu opens on click, real navigation), list the exact
      manual-verification steps and flag them in the summary
- [ ] Docs written this session use the **freshly-verified** model identity (see Model Identity
      Convention) + actual session timestamp — never a value copied from prior files
- [ ] Fix any warning you introduce

---

## Build & Test Cadence

- After each unit of work: `dotnet build <target.csproj>` then `dotnet test <app>.Tests`
  (fast feedback, small output)
- **Handoff verification: run `pwsh opencode/addBlazorFrontends/verify.ps1`.** It deletes `obj/` +
  `bin/` for both apps and their test projects (the Razor source-gen cache in `obj/` is NOT flushed
  by `--no-incremental` — real incident: `Icons.Material.Filled.Bell` shipped "green"), builds both
  apps counting warnings/errors, runs both test suites, performs the MudBlazor icon audit, and
  prints a paste-ready verification block for the summary. Variants: `-SkipClean`, `-SkipTests`.
- Run verify.ps1 **inside the stream's own worktree** — the main checkout cannot verify worktree state.
- Full solution build + both app suites at phase end (catches cross-project breakage)
- **Icon smoke check (MudBlazor):** verify.ps1 audits `Icons.Material.*` references by reflection;
  still spot-check new icons manually — names drift between MudBlazor versions (`Bell` → use
  `Notifications`).

---

## Scope Restriction

Modify ONLY:

- `./clients/BlazorShared` — **Main stream only**; parallel streams treat it read-only
- `./clients/admin-blazor` · `./clients/dashboard-blazor` — Main stream (parallel-safe only for
  standalone test projects, e.g. `clients/admin-blazor/FSH.Admin.Wasm.E2E.Tests`)
- `./clients/FSH.Hybrid` — parallel-safe (MAUI stream)
- `./opencode/addBlazorFrontends`
- `./.agents/rules/frontend/{blazor-shared,blazor-admin,blazor-dashboard,maui-hybrid}.md` — OUR docs
- `./.agents/skills/{add-blazor-page,add-maui-hybrid-feature,add-permission-csharp,implement-blazor-form,implement-blazor-list,setup-blazor-auth,setup-blazor-realtime,setup-blazor-sse}/SKILL.md` — OUR skills

NEVER modify (upstream baseline / React reference):

- `clients/admin` · `clients/dashboard` (React apps — READ-ONLY parity source)
- `AGENTS.md` · `CLAUDE.md` · `GEMINI.md` · `.github/**` · `deploy/**` · `src/**`
- `.agents/rules/**` (all other rule files) · `.agents/skills/*` (all other skills) · `.agents/workflows/**`

---

## Audit / Review Convention

For parity/review tasks spanning many files, delegate to an explore subagent and require it to write
full findings to a temp file while returning only a compact severity-ranked list
(Critical/High/Medium/Low) — keeps the working context small.

---

## Implementation Summary

Write an MD file named `implementation-summary-<yyyy-MM-dd-HH-mm-ss>.md` (24-hour clock) in
`./opencode/addBlazorFrontends/00_summary/`. **Use `00_summary/_template.md` as the canonical
schema** (header, description/creator/duration front-matter, verification block) — it supersedes
inline templates; fill the verification block from `verify.ps1` output. Parallel streams write
their own summary inside their worktree's `opencode/addBlazorFrontends/00_summary/`.

---

## Hands-On Files

Skip the `hands-on-phase-*.md` files until all phases have completed perfectly. After each phase
completes, write a brief `phase-N-complete.md` snapshot (what was built, known limitations, files
created/modified) in the phase folder. Write the full hands-on files from those snapshots + implementation
summaries after all phases are done.

---

## Dev Servers

```
dotnet run --project src/Host/FSH.Starter.AppHost   # starts everything
```

| App | URL | Tenant | Email | Password |
|-----|-----|--------|-------|----------|
| admin (React) | http://localhost:5173 | root | admin@root.com | Password123! |
| dashboard (React) | http://localhost:5174 | acme | admin@acme.com | Password123! |
| admin-blazor | http://localhost:5175 | root | admin@root.com | Password123! |
| dashboard-blazor | http://localhost:5176 | acme | admin@acme.com | Password123! |
| MAUI Hybrid | — (native, Windows) | — | — | — |

MAUI Windows build: `dotnet build clients/FSH.Hybrid/FSH.Hybrid/FSH.Hybrid.csproj -f net10.0-windows10.0.19041.0`

## Running MAUI from Visual Studio (two-instance setup)

The MAUI app is a Blazor WebView client of the API — the backend must be up before login.

1. **Instance 1 — backend:** open `src\FSH.Starter.slnx`, startup project `FSH.Starter.AppHost`, F5
   (Docker infra + migrator + API; also starts the React apps — ignore them). Alternative: run
   `dotnet run --project src/Host/FSH.Starter.AppHost` from a terminal (no API breakpoints).
2. **Instance 2 — MAUI:** File → Open → Project/Solution → `clients\FSH.Hybrid\FSH.Hybrid\FSH.Hybrid.csproj`
   (loads as a real MAUI project) → run target **`Windows Machine`** → F5. Unpackaged exe
   (`WindowsPackageType=None`), no MSIX signing. Android works via emulator; iOS/MacCatalyst need a
   paired Mac.
3. **Prereqs:** API dev cert trusted (`dotnet dev-certs https --trust` — app defaults to
   `https://localhost:7030` via `HybridRuntimeConfigService`); Windows App Runtime if F5 complains
   (VS MAUI workload usually provisions it).

The two instances share the checkout safely: the backend slnx covers `src/**` only, and the MAUI
project's only shared dependency is `clients\BlazorShared` (read-only reference).
