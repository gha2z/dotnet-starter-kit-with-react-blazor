# Session Protocol — Current Environment (canonical)

> The single source of truth for how a coding agent behaves in this repo. Read this file before
> any work; the **ritual below is self-starting** — you run it because you read it, never because a
> human reminded you. Everything is on disk, so a session that was compacted, closed, or started in
> a different tool resumes here in one read.
>
> Package: `workflows/current/`. Applies to the live track `opencode/addBlazorFrontends/` and any
> track seeded from `workflows/_tracks-template/`.

---

## 1. Session Start Ritual (run automatically on every task)

**Whole-file reads only** (no offset-tunneling) — state moves fast in this repo.

1. Read fresh, in order (whole files):
   - `AGENTS.md` (golden rules + register) → this file
   - the track's **`START-HERE.md`** (entry point) → the track's **`STATUS.md`** (top dashboard =
     current mission / next task / obstacles; history below)
   - the **current mission folder** named in the dashboard: `spec.md` + `plan.md` (status
     markers) + `implementation.md` (latest waves)
   - all track `live/*.md` → lessons ledger tail (step 4)
2. **Identify** — session id is the name the user gives (`sess-main`, `sess-maui`, …). Record it,
   your **model identity** (verify fresh — never copy from an older doc; write `<modelID> (unresolved)`
   if unverifiable), your scope, and a heartbeat in `live/sess-<id>.md` (from `live/_template.md`).
   Session files stay **short** (identity, heartbeat, current focus, `Next:` pointer, recent
   lessons) — mission narrative belongs in the mission's `implementation.md`, never in session files.
3. **Heartbeat** — stamp `live/sess-<id>.md` heartbeat AND run
   `pwsh opencode/<track>/coordination.ps1 -Session <sid> -Start` (also validates the session file).
4. **Read the lessons ledger tail** — `coordination.ps1 -Session <sid> -Lessons` — check the
   failure-pattern registry for a match **before** acting (a matching event gets `[RECUR]` on append).
5. **Overlap check** — is the task claimed by another session? Does it touch files in another
   session's zone (`zones.md`) or "Touched files" register? If either → stop, post on `live/board.md`.
6. **Verify the baseline** — the track's verify script runs green before you change code.

**Memory rule:** your model's memory tools (`ctx_memory`, `ctx_search`, `knowledge_recall`,
`ctx_note`) are search/recall aids and personal scratch — **never the source of truth**. The disk
(`STATUS.md`, mission folders, `live/`, summaries) is. If the disk and your memory disagree, the disk wins.

## 1b. Mission folders (spec → plan → implementation)

Every non-trivial unit of work lives in a mission folder under the track:
`Phase-XX-<Mission>/` (numbering continues the track's phase sequence).

- **`spec.md`** — the human authors requirements (verbatim reports are fine). The agent may
  draft it from conversation for the human to correct, but the human owns it.
- **`plan.md`** — the agent authors it FROM the spec: phases, tasks with ☐/🔄/✅ status markers,
  per-phase and overall success criteria, execution order. **The human confirms plan.md before
  implementation begins** (bench rule: do not proceed to build on an unconfirmed plan). Task
  claims append `— claimed by <sid> @ <ts>`; completion marks `✅ by <sid>`; never edit another
  session's claim.
- **`implementation.md`** — the agent keeps it current per wave: what changed, why, evidence
  (probe results, screenshots, test counts — point-inimate numbers live HERE and in
  `00_summary/`, not in living docs), deviations from plan, lessons.

Reading contract: **resuming work = `START-HERE.md` → `STATUS.md` dashboard → mission `plan.md`
markers.** Three reads, always current. "Why was X done?" → mission `implementation.md` →
`00_summary/` for raw evidence.

## 2. During Work (every user turn)

1. `coordination.ps1 -Session <sid> -Heartbeat` — always, not only at task boundaries.
2. `coordination.ps1 -Session <sid> -Lessons` — check patterns before acting.
3. Zone check — see Start Ritual step 5; a task drifting out of zone stops and is re-routed.
4. At turn end append **exactly one line** to the lesson ledger:
   `coordination.ps1 -Session <sid> -Lesson "<failure -> root cause -> remedy -> proof>"`.
   If nothing failed, write one improvement or one verified pattern. No empty turns.
   Tag lessons with a category when relevant: `[MUD]` `[SIGNALR]` `[WASM]` `[PLAYWRIGHT]`
   `[BUILD]` `[ARCH]` `[AUTH]` `[EF]` `[REDIS]` `[MINIO]`. After every5 turns with
   `[RECUR]` matches, re-evaluate section 6 — the process may need to evolve.

## 3. Wave Close-Out (after each verified unit of work)

1. Write `00_summary/implementation-summary-<yyyy-MM-dd-HH-mm-ss>-<sid>.md` per
   `00_summary/_template.md` — header + verification block + **`## Lessons`** section. This summary
   is the **single tracking artifact** for the wave.
2. Append one line to `STATUS.md` (timestamp + wave + commit + test counts). **Append-only ledger —
   never edit existing rows.**
3. If a board row exists for this work, update its Status cell only. (Board rows are for
   **cross-session** coordination; single-session work adds none.)
4. `coordination.ps1 -Session <sid> -CloseOut` — fail-closed: blocks staging if the summary,
   its `## Lessons`, STATUS refresh, heartbeat, or the lesson ledger are missing.
5. End with a `Next: <task id>` line in your session file — so the next session (even after
   compaction) resumes in one read.
6. Stage **by explicit path** (`git add <paths>`, never `-A`), show `git diff --cached --stat`,
   wait for user approval. Never `git push` (user-managed).

## 4. Verification (non-negotiable)

| When | Gate |
|---|---|
| Every unit of work | `dotnet build <target.csproj>` + `dotnet test <app>.Tests` (or track verify script) |
| Handoff / phase end | track `verify.ps1` (clean obj/bin → 0-warning builds → suites → app audits) |
| MAUI | track `verify-hybrid.ps1` |
| Backend | `dotnet build src/FSH.Starter.slnx` (warnings are errors) |
| Blazor/Hybrid page | real-browser walkthrough (Playwright driver) — bUnit is necessary, not sufficient |
| Numbers in docs | always from a real run THIS session — never from memory |

**Engineering rules (bench-inspired, mandatory where feasible):**
- **Prove the root cause before fixing.** Reproduce it, measure it, show the evidence (a probe,
  a failing request, a screenshot). Never ship a workaround for an unexplained symptom.
- **A bug fix gets a regression test that fails without it** (bUnit, a probe assertion, or a
  backend test) — that is what stops it coming back.
- **Fix flakiness at the root** (shared state, unmet wait, viewport) — never paper over with
  retries/timeouts without proving the cause.
- When you deliberately leave something unverified or uncovered, say so explicitly in the
  mission `implementation.md` — a green suite must not imply coverage it does not have.

`verify.ps1`/`verify-hybrid.ps1` delete `obj/`/`bin/` — take the **verify lock** first
(`coordination.ps1 -LockVerify`, release with `-UnlockVerify`); the script refuses while the lock
exists. A stale lock (owner heartbeat >12h) is removable after a `board.md` announcement.

## 5. Coordination (multi-session, same checkout)

Sessions coordinate entirely through MD files under the track's `live/`:
`board.md` (append-only rows) · `sess-<id>.md` (**single-writer** — you write only yours; others
may create a stub from `_template.md`, never populate it) · `lessons.md` (shared ledger, rule 13).

Ownership authority is **`zones.md`** (one `owner: prefix` line per zone) — parsed by
`coordination.ps1`, and the **only** place the zone→owner map lives. The script fails closed on
any path outside your zones.

Task claims live in the phase `plan.md` (append `— claimed by <sid> @ <ts>`; finish `✅ by <sid>`);
never edit a line another session claimed. Read claims + `live/*` fresh before every task, before
staging, and before merging.

Incidents → short playbook, full detail in the track `readme.md`:
- Swept foreign files into your staged set → `git restore --staged <paths>`, re-check.
- Lost board row (concurrent append) → re-read, re-append same ID + `(re-appended)`.
- Stale verify.lock → check owner heartbeat, announce on `board.md`, remove.
- Wrong model identity in docs → re-verify fresh, fix the header only.

## 6. Self-Improvement Loop (makes the workflow better every session)

- **Lesson ledger** (`live/lessons.md`) — one line per turn; top section is the **failure-pattern
  registry** (recurring failure classes + standing remedies). Recurrence is appended with `[RECUR]`
  and the registered remedy is followed — patterns are never re-litigated.
- **Wave summaries** carry a `## Lessons` section — the phase curator/reviewer reads them for
  cross-session drift.
- **Process changes** are authored here (this protocol), ranked by evidence from the ledger, and
  versioned with the code — never improvised mid-session.
- **Recursive self-improvement:** when a process change lands, re-read this section and ask whether
  the *way improvements are made* should also change (bench PROCESS.md §7 pattern). Workflow
  structure is reviewed whenever it starts costing more than it saves — duplication is removed,
  stale artifacts archived (not deleted), and the reading contract kept to three reads.

## 7. How this protocol is wired (pointer map)

This file is canonical. Everything below points here and no longer duplicates it:

- `adapt/AGENTS-session-protocol.md` — the exact section pasted into `AGENTS.md` (self-start guarantee).
- `opencode/AGENTIC-GUIDE.md` — the repo guide (tracks, driving Blazor/MAUI work, new-SaaS bootstrap, FAQ).
- `opencode/addBlazorFrontends/readme.md` — the live track's session instructions (superset, per-track).
- `task-skills.md` — canonical task→skill map (FSH recipes + dotnet plugin suite).
- `verify.md` — verbose verification contract + baselines.
- `coordination.ps1` — the mechanical gate (parses `zones.md`).