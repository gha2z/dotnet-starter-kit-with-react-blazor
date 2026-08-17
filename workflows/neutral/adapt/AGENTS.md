# AGENTS.md addition — Session protocol (neutral, paste into the original repo's AGENTS.md)

Resolve `<workflow-dir>` (`workflow/neutral`, `workflows/neutral`, …) before pasting. This section
is the **self-start guarantee**: any coding agent that reads AGENTS.md starts the ritual itself.

---

## Agent session protocol (MANDATORY, self-starting)

Every coding agent, any session, including a session resumed after context compaction, MUST run
this on every task. Progress lives **on disk** — never reconstruct state from memory.

1. **Read fresh** (whole files, in order): this file → `<workflow-dir>/session-protocol.md` →
   the work-stream's `STATUS.md` → all `<workflow-dir>/<track>/live/*.md` → latest
   `<workflow-dir>/<track>/00_summary/implementation-summary-*.md`.
2. **Identify** — session id (user-named, e.g. `sess-main`), tool/model identity **verified
   fresh** (never copied from older docs; `(unresolved)` if unverifiable), scope, heartbeat — in
   `live/sess-<id>.md` from `live/_template.md`; run
   `pwsh <workflow-dir>/coordination.ps1 -Session <sid> -Start` from the repo root.
3. **Lessons before acting** — `pwsh <workflow-dir>/coordination.ps1 -Session <sid> -Lessons`
   (failure-pattern registry; a matching event is appended with `[RECUR]` + its registered remedy).
4. **Overlap check** — task claimed elsewhere, or touching another session's zone (`zones.md`) or
   touched files? Stop, post on `<workflow-dir>/<track>/live/board.md`, take another task.
5. **Every user turn** — heartbeat (`-Heartbeat`); at turn end append exactly one line:
   `pwsh <workflow-dir>/coordination.ps1 -Session <sid> -Lesson "<failure -> root cause -> remedy -> proof>"`.
   If nothing failed, write one improvement or verified pattern. No empty turns.
6. **Wave close-out** — summary to `00_summary/` per `_template.md` (with `## Lessons` section) →
   one append-only line to `STATUS.md` → `coordination.ps1 -Session <sid> -CloseOut` (fail-closed)
   → stage by **explicit paths** (`git add <paths>`, never `-A`), show `git diff --cached --stat`,
   wait for approval. Never `git push`.
7. **Verify** — `verify.ps1` green (clean build 0 warnings + suites) before staging; numbers from
   real runs this session only.
8. **Continuity** — end each session with a `Next: <task id>` line in your session file and the
   STATUS ledger current, so any future session resumes in one read.
9. **Read the repo's `.agents/`** — the relevant `.agents/rules/**` file and `.agents/skills/*/SKILL.md`
   before the task, not during; FSH recipes win structurally.

Work-stream for this repo: `<workflow-dir>/<track>/` (zones.md owns the zone→session map).