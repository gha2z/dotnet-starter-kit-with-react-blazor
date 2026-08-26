# Session Protocol — Neutral (canonical)

> The single source of truth for how a coding agent behaves in any tool on the original
> fullstackhero repo (React pair, .NET monolith). **Self-starting**: you run the ritual because
> you read `AGENTS.md` and this file — never because a human reminded you. Everything lives on
> disk, so a compacted, closed, or tool-restarted session resumes here in one read.
>
> Package: `<workflow-dir>/neutral/` (used from this repo as `workflow/neutral/`). Track root:
> `<workflow-dir>/<track>/` (default in `coordination.ps1`). No opencode, no plugins, no swarms —
> only PowerShell, git, markdown, dotnet/npm CLI, and the repo's own `.agents/` knowledge.

---

## 1. Session Start Ritual (run automatically on every task)

Whole-file reads, fresh, in order:

1. `AGENTS.md` (golden rules) → this file → the track's `readme.md`
2. The track's `STATUS.md` → all track `live/*.md` → latest track `00_summary/implementation-summary-*.md`
3. **Identify** — session id is the user-given name (`sess-main`, …). Record it + your tool/model
   identity (**verified fresh** each session — never copied from an older doc; write
   `<modelID> (unresolved)` when unverifiable) + scope + heartbeat in `live/sess-<id>.md`
   (from `live/_template.md`), then run `coordination.ps1 -Session <sid> -Start` from the repo root.
4. **Lessons before acting** — `coordination.ps1 -Session <sid> -Lessons`; check the failure-pattern
   registry for a match; a matching event is appended with `[RECUR]` + the registered remedy.
5. **Overlap check** — is the task claimed by another session? Does it touch files in another
   session's zone (`zones.md`) or touched-files register? If either → stop, post on
   `live/board.md`, take another task.
6. **Baseline** — the track's verify script (`verify.ps1`) runs green before you change code.
7. **Skills** — load the relevant `.agents/skills/*/SKILL.md` (read the markdown; FSH recipes win
   structurally) and the matching `.agents/rules/` file BEFORE the task, not during.

**Memory rule:** this workflow's memory IS the disk — `STATUS.md`, `live/`, `00_summary/`, the
lessons ledger. If your tool has native memory/notes, treat them as scratch: they may remind you,
but never contradict the disk.

## 2. During Work (every user turn)

1. `coordination.ps1 -Session <sid> -Heartbeat` — every turn, not only at task boundaries.
2. `coordination.ps1 -Session <sid> -Lessons` — check patterns before acting.
3. Zone check (ritual step 5); drifting out of zone stops the task and re-routes.
4. At turn end append **exactly one line**: `coordination.ps1 -Session <sid> -Lesson "<failure ->
   root cause -> remedy -> proof>"`. If nothing failed, write one improvement or one verified
   pattern. No empty turns. Tag lessons with a category when relevant: `[MUD]` `[SIGNALR]`
   `[WASM]` `[PLAYWRIGHT]` `[BUILD]` `[ARCH]` `[AUTH]` `[EF]` `[REDIS]` `[MINIO]`. After
   every5 turns with `[RECUR]` matches, re-evaluate section 6 — the process may need to evolve.

## 3. Wave Close-Out (after each verified unit of work)

1. Write `00_summary/implementation-summary-<yyyy-MM-dd-HH-mm-ss>-<sid>.md` per
   `00_summary/_template.md` — header + verification block + **`## Lessons`** section (the wave's
   single tracking artifact).
2. Append one line to `STATUS.md` (timestamp + wave + commit + test counts). **Append-only —
   never edit existing rows.**
3. Board rows exist only for cross-session coordination; update their Status cell, nothing else.
4. `coordination.ps1 -Session <sid> -CloseOut` — fail-closed (verifies summary + `## Lessons` +
   STATUS refresh + heartbeat + ledger).
5. End with a `Next: <task id>` line in your session file — the next session resumes in one read.
6. Stage **by explicit path** (`git add <paths>`, never `-A`), show `git diff --cached --stat`,
   wait for user approval. Never `git push` (user-managed).

## 4. Verification (non-negotiable)

| When | Gate |
|---|---|
| Every unit of work | `dotnet build <project>` + `dotnet test <project>.Tests` (original repo: 0-warning builds, `TreatWarningsAsErrors`) |
| Handoff / phase end | track `verify.ps1` — clean build, `dotnet test src/FSH.Starter.slnx` (integration tests need Docker), `npm run build` + `npx tsc --noEmit` per React app (apps are FROZEN reference — read-only), route-mocked Playwright suites |
| Numbers in docs | from a real run THIS session — never from memory |

`verify.ps1` deletes `obj/`/`bin/` — take the **verify lock** first
(`coordination.ps1 -LockVerify`, release with `-UnlockVerify`); coordination refuses builds while
the lock exists. Stale lock (owner heartbeat >12h): announce on `board.md`, then remove.

## 5. Coordination (multi-session, same checkout)

Sessions coordinate through MD files under the track's `live/`: `board.md` (append-only rows) ·
`sess-<id>.md` (**single-writer**; others create a stub from `_template.md` at most) ·
`lessons.md` (shared, rule below).

- **Ownership authority is `zones.md`** — one `owner: prefix` line per zone, parsed by
  `coordination.ps1`. It is the ONLY place the zone→owner map lives; the gate fails closed on any
  path outside your zones.
- **Task claims** live in `plan.md` — append `— claimed by <sid> @ <yyyy-MM-dd HH:mm>`; finish
  `✅ by <sid>`. Never edit a line another session claimed.
- **Read-fresh ceremony** — re-read all `live/*.md` and claims before every task, before staging,
  and before merging. Files are UTF-8 (write with `-Encoding UTF8`).
- **Incidents:** swept foreign files → `git restore --staged <paths>`; lost board row (concurrent
  append) → re-read, re-append same ID + `(re-appended)`; stale lock → heartbeat check +
  announcement; wrong identity in docs → re-verify, fix header only.

## 6. Self-Improvement Loop

- **Lesson ledger** (`live/lessons.md`): one line per turn; top section is the failure-pattern
  registry (hall-of-fame recurrence classes + standing remedies). Recurrence is `[RECUR]` + the
  registered remedy — patterns are never re-litigated.
- Wave summaries carry `## Lessons`; reviewers read them for cross-session drift.
- Process changes are authored here, ranked by ledger evidence, versioned with the code.

## 7. Tool adaptation notes

- **GitHub Copilot VS 2026 / VS Code** — reads root `AGENTS.md`; executes `pwsh` via integrated
  terminal. No changes needed beyond install step 2.
- **Claude Code** — reads `CLAUDE.md` (the `adapt/CLAUDE.md` bridge points here) + `AGENTS.md`.
- **Cursor / Gemini CLI / Codex** — AGENTS.md standard; check the tool's "rules" setting accepts
  `AGENTS.md` globals.
- **Linux/macOS** — everything above works; install `pwsh` (PowerShell) for the gate script, or
  run the same checks manually (the gate is a convenience, the protocol is the contract).