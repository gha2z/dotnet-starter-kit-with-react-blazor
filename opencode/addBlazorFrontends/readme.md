# AddBlazorFrontends — Session Instructions
**Last updated: 2026-08-11 12:34, by: opencode (model: opencode/deepseek-v4-flash-free) — Stage A: summary close-out gate (`coordination.ps1 -CloseOut`), `## Lessons` summary section, wave-DAG/critical-path convention.**

## Commit Policy

**NEVER commit until the user explicitly approves.**

After completing a feature:
1. Stage **only your session's files by explicit path** — `git add <paths>`. NEVER `git add -A`
   in the shared checkout: it sweeps other sessions' uncommitted work into your commit (see
   "Multi-Session Coordination Protocol" below)
2. Show: `git diff --cached --stat` + one-line summary of what changed
3. Wait for user approval
4. Then commit
5. **Never `git push`** — pushes are user-managed; independent pushes break coordination

Also run the staging gate first: `pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid>`
(validates your session file, heartbeat, file ownership and the touched-files overlap check —
see "Coordination Gate Script" below).

---

## Session Start Ritual

On session start:
1. `git status` — confirm the tree is clean or matches the expected in-progress work
2. Determine your session id: the user names this session (`sess-main`, `sess-maui`, …). If
   unnamed, ask. Every doc and coordination file uses it
3. Determine your actual model identity (see "Model Identity Convention" below) and record it
   for today's docs
4. Read `./opencode/addBlazorFrontends/STATUS.md` — current state, next task
5. Read ALL `./opencode/addBlazorFrontends/live/*.md` (session coordination — see
   "Multi-Session Coordination Protocol"); create `live/<your-session-id>.md` from
   `live/_template.md` if it does not exist
6. Read the latest `./opencode/addBlazorFrontends/00_summary/implementation-summary-*.md` — what was last done
7. Read `.agents/rules/frontend/blazor-shared.md` — Blazor conventions (plus `blazor-admin.md`,
   `blazor-dashboard.md`, or `maui-hybrid.md` for the target app)
8. Load any relevant skills from `.agents/skills/` (e.g. `add-blazor-page`, `add-feature`,
   `setup-blazor-auth`, `setup-blazor-realtime`, `implement-blazor-list`, `implement-blazor-form`,
   `add-permission`) before you start; for .NET/Blazor/test tasks supplement with the dotnet
   plugin suite — see the task→skill map in `WORKFLOW-GUIDE.md` (`author-component`,
   `fetch-and-send-data`, `use-js-interop`, `run-tests`, `optimizing-ef-core-queries`,
   `test-anti-patterns`, `dotnet-webapi`, dotnet-maui skills, build-perf-*)
9. Verify builds + tests pass before starting work

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

**Same-checkout is the default for parallel sessions** — several sessions run side-by-side in the
repo root with disjoint directory ownership and coordinate through MD files (see
"Multi-Session Coordination Protocol" below). Worktrees remain available for heavy isolation.

## Multi-Session Coordination Protocol

Parallel sessions in the same checkout coordinate **entirely through MD files** under
`opencode/addBlazorFrontends/live/`:

```
live/
├── README.md          # this protocol's quick reference (stable)
├── board.md           # cross-session requests & handoffs (append-only rows)
├── _template.md       # skeleton for a new session file
├── sess-<id>.md       # ONE file per session — only that session writes it
└── ...
```

### Rules (binding once a session has read this section)

1. **Session identity** — the user names each session at launch (`sess-main`, `sess-maui`, …).
   Record it + your model identity + scope + heartbeat in `live/<session-id>.md` before any work.
2. **Single-writer files** — you write ONLY `live/<your-session-id>.md`. You may APPEND rows to
   `live/board.md` (re-read it first; never rewrite existing rows; if the file changed under you,
   re-read and re-append — a concurrent append must never be lost). **Only the owner writes their
   session file — other sessions may create a stub from `_template.md` at most, never populate or
   edit it** (seeding another session's identity/heartbeat caused drift before). Every other file
   is read-only unless the ownership map gives it to you.
3. **Ownership map** — **`Scope Restriction` below is the single authority; this map is derived
   from it. If the two disagree, Scope Restriction wins.** (directory → owner):

   | Zone | Owner |
   |------|-------|
   | `clients/dashboard-blazor/**` · `clients/BlazorShared/**` | `sess-main` (others read-only) |
   | `clients/admin-blazor/**` | Main-owned by default (app code + tests); app-code edits by other sessions require a board sign-off per task (Phase C palette granted — row #1); standalone test projects (`FSH.Admin.Wasm.E2E.Tests`) always parallel-safe |
   | `clients/FSH.Hybrid/**` | `sess-maui` |
   | `opencode/addBlazorFrontends/live/*` | per-file (rule 2); `locks/*` are the shared verify lock (rule 9) |
   | `STATUS.md` · `00-Index.md` · phase-plan status lines | `sess-main` (streams may APPEND their own row to "Active Streams"; streams own the status lines of tasks they claimed) |
   | `.agents/rules/frontend/*.md` · root `README.md` | any session, but announce the edit on `board.md` first |

4. **Task claims** — the phase plans (`Phase-*.md`) are the task source of truth. Claim a task by
   appending `— claimed by <sid> @ <yyyy-MM-dd HH:mm>` to its task line; finish it with
   `✅ by <sid>`. Never edit a line another session claimed. **Your incoming queue = unclaimed
   tasks in your owned zones** — reading the plans + claims tells every session what every other
   session is doing now and next.
5. **Read-fresh ceremony** — re-read ALL `live/*.md` fresh (never from memory): at session start,
   before starting any task, before staging, and before merging.
6. **Overlap pre-check** — before a task: is it claimed by another session? Does it touch any file
   in another session's "Touched files" register? If either → do not start; post a request on
   `board.md` or take another task.
7. **Heartbeat** — stamp `heartbeat:` on **every user turn** (not only task boundaries) — an
   idle-but-mid-task session must still be visible as alive. A claim whose owner's heartbeat is
   older than 12h is void; the user re-assigns it.
8. **Staging** — explicit-path `git add` only; never `git add -A` (sweeps peers' work). Run the
   gate (`coordination.ps1`) and re-read `live/*.md` before staging. The commit diff review is the
   final guard — if your staged stat shows files outside your scope, unstage them.
9. **Build/verify contention — lock, don't announce** — `verify.ps1` / `verify-hybrid.ps1` clean
   `obj/`/`bin/` and build from the shared checkout. Before running either, create the lock file
   `live/locks/verify.lock` (atomic `New-Item`); delete it when done. While the lock exists, no
   session runs `dotnet build`/`dotnet test`/verify on any shared-checkout project (`coordination.ps1`
   refuses when the lock is present). A stale lock (creator's heartbeat > 12h) may be removed by
   anyone after announcing on `board.md`. Windows note: VS open on the solution, `.vs` locks, or
   running dev servers can abort the clean step — stop them or use `-SkipClean` first.
10. **Worktree variant** — if a stream ever moves to a worktree, MD sync requires small
    user-approved "sync commits" of `live/` + claim lines only, pulled via
    `git merge --ff-only develop` at task boundaries. Same-checkout stays the default.
11. **Shared MD files are UTF-8** — always read + write them as UTF-8 (`Set-Content -Encoding UTF8`,
    never a default-ANSI rewrite). Terminal displays may render emoji as `?` even when the file is
    fine — verify with the read tool / `git show HEAD:<file>` before assuming corruption.
12. **Incident recovery** (keep short — see full playbook in the readme appendix):
    - Swept foreign files into your staged set → `git restore --staged <paths>` and re-check.
    - Lost board row (concurrent append) → re-read, re-append with the same ID + `(re-appended)`.
    - Stale verify.lock → remove after checking creator heartbeat, announce on `board.md`.
    - Wrong identity in docs → fix the header only after re-verifying (never copy from memory).

## Coordination Gate Script

`opencode/addBlazorFrontends/coordination.ps1` turns rules 1/7/8/9 into a mechanical gate — run it
at session start, before staging, and before any shared-checkout build:

```powershell
# session start: validates session file + stamps a fresh heartbeat
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Start

# before staging: ownership + touched-files + heartbeat checks (exit code 1 on violations)
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Gate

# build/verify lock (rule 9) — create before verify.ps1 / verify-hybrid.ps1, delete after
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -LockVerify
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -UnlockVerify

# stamp heartbeat on every user turn
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -Heartbeat

# wave close-out (Stage A): summary + ## Lessons section + refreshed STATUS.md (exit code 1 = block commit)
pwsh opencode/addBlazorFrontends/coordination.ps1 -Session <sid> -CloseOut
```

The zone→session map lives at the top of the script (keep it in sync with `Scope Restriction`
below — the script fails closed: any path outside your zones blocks staging).

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
2. **If `modelID` is a concrete model** (not `auto`/`auto/*`, e.g. `opencode/deepseek-v4-flash-free`,
   `mimo-v2.5-free` via provider `opencode`) → use it directly. **No gateway log query needed.**
3. **If `modelID` is an `auto` combo** (e.g. `auto/coding`, provider `omniroute`) → resolve the real
   routed model through the omniroute usage log (`/api/usage/call-logs?status=ok&limit=1` — ask the
   user for the gateway dashboard URL and credentials; they are NOT stored in this repo). Read the
   `model` field of the most recent `ok` entry.
4. **If you cannot verify** (no raw-message access, gateway unreachable) → write
   `model: <modelID> (unresolved)` and flag it in the session summary. **Never guess, never copy
   from an older doc** — identity drift happened before and propagated for two sessions.

### Identity format in docs

- Concrete model: `Last Update: <yyyy-MM-dd HH:mm:ss>, by: opencode (model: <modelID>).`
- Resolved auto combo: `Last Update: <yyyy-MM-dd HH:mm:ss>, by: opencode (auto/coding, model: <routed-model>).`
- Unresolved: `Last Update: <yyyy-MM-dd HH:mm:ss>, by: opencode (model: <modelID> — unresolved).`
- Summaries: `**Creator:** opencode (model: <modelID>)` — same three variants.

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
- [ ] Session file `live/sess-<id>.md` updated (`## Current task`, `## Touched files`, heartbeat bumped)
- [ ] Wave close-out: summary written to `00_summary/` per template **with** `## Lessons` section
- [ ] Wave close-out: `STATUS.md` "Last Update" + phase/next-task line refreshed this wave
- [ ] Wave close-out: `coordination.ps1 -Session <sid> -CloseOut` passes (fail-closed, blocks staging)

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
  **In the shared checkout, take the verify lock first** (rule 9): `coordination.ps1 -LockVerify`
  before, `-UnlockVerify` after.
- Full solution build + both app suites at phase end (catches cross-project breakage)
- **Icon smoke check (MudBlazor):** verify.ps1 audits `Icons.Material.*` references by reflection;
  still spot-check new icons manually — names drift between MudBlazor versions (`Bell` → use
  `Notifications`).

---

## Scope Restriction

Modify ONLY:

- `./clients/BlazorShared` — **Main stream only**; parallel streams treat it read-only
- `./clients/admin-blazor` — Main stream by default (app code + tests). App-code edits by other
  sessions require a board sign-off per task (granted for the Phase C admin palette — board row #1);
  standalone test projects (`FSH.Admin.Wasm.E2E.Tests`) always parallel-safe
- `./clients/dashboard-blazor` — Main stream (parallel-safe only for standalone test projects)
- `./clients/FSH.Hybrid` — parallel-safe (MAUI stream)
- `./opencode/addBlazorFrontends` (incl. `live/` — per-file ownership, see coordination protocol)
- `./.gitignore` — gate-relevant (untracked local tooling dirs like `.opencode/` must stay ignored)
- `./README.md` — root project docs (announce the edit on `live/board.md` first)
- `./.agents/rules/frontend/{blazor-shared,blazor-admin,blazor-dashboard,maui-hybrid}.md` — OUR docs
- `./.agents/skills/{add-blazor-page,add-maui-hybrid-feature,add-permission-csharp,implement-blazor-form,implement-blazor-list,setup-blazor-auth,setup-blazor-realtime,setup-blazor-sse}/SKILL.md` — OUR skills
- `./opencode/AGENTIC-GUIDE.md` · `./opencode/_tracks-template/` · `./docs/spec/` · `./HUMAN-GUIDE.md` — workflow-authoring paths (track model + requirements home, user-approved); additive edits only, board-announced. Workflow is **repo-local** — the standalone `workflow/` publish/sync machinery was retired (user-approved)

NEVER modify (upstream baseline / React reference):

- `clients/admin` · `clients/dashboard` (React apps — READ-ONLY parity source)
- `AGENTS.md` · `CLAUDE.md` · `GEMINI.md` · `.github/**` · `deploy/**` · `src/**`
  - `AGENTS.md` + `.agents/workflows/**`: workflow-enhancement paths (waves 20–21b, user-approved) —
    additive edits only, require user approval + a board row. All other `.agents/**` remains NEVER.
  - `.github/**` is frozen; the Phase 5.10 MAUI CI workflow is deferred until the user approves
    a change here — the local `verify-hybrid.ps1` covers the gate meanwhile
- `.agents/rules/**` (all other rule files) · `.agents/skills/*` (all other skills) · `.agents/workflows/**`

---

## Audit / Review Convention

For parity/review tasks spanning many files, delegate to an explore subagent and require it to write
full findings to a temp file while returning only a compact severity-ranked list
(Critical/High/Medium/Low) — keeps the working context small.

---

## Implementation Summary

Write an MD file named `implementation-summary-<yyyy-MM-dd-HH-mm-ss>-<session-id>.md` (24-hour clock)
in `./opencode/addBlazorFrontends/00_summary/`. **Use `00_summary/_template.md` as the canonical
schema** (header, description/creator/duration front-matter, verification block) — it supersedes
inline templates; fill the verification block from `verify.ps1` output. Same-checkout sessions
write theirs in the same folder — timestamp + session-id keep filenames unique. Parallel streams
in worktrees write their own summary inside their worktree's `opencode/addBlazorFrontends/00_summary/`.

**Every summary MUST end with the `## Lessons / Process Improvements` section** (per
`00_summary/_template.md`). A summary + a refreshed `STATUS.md` are required **before** a wave is
committed — enforced by `coordination.ps1 -CloseOut` (see "Coordination Gate Script").

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
